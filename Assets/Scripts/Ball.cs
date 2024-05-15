using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class Ball : MonoBehaviour
{

    #region Fields

    // sprite used in phase two
    [SerializeField] Sprite ballPhaseTwo;

    // references
    Utils utils;
    Animator animator;
    GoalEvent goalEvent;
    HitBorderEvent hitBorderEvent;

    // diverse
    private bool isMoving;
    private bool isTraumatic;
    private float ballVelocity;
    private Utils.Opponent whosTurn; 
    private Vector2 ballDirection;

    // input 
    IA input;
    InputAction goalNorth;
    InputAction goalSoulth;

    #endregion

    #region Private Methods

    private void Awake()
    {
        // get references
        utils = FindObjectOfType<Utils>();
        animator = GetComponent<Animator>();
        goalEvent = new GoalEvent();
        hitBorderEvent = new HitBorderEvent();
        input = new IA();

        // activate input
        goalNorth = input.Developer.GoalNorth;
        goalNorth.started += ShortcutGoalNorth;
        goalNorth.Enable();
        goalSoulth = input.Developer.GoalSouth;
        goalSoulth.started += ShortcotGoalSouth;
        goalSoulth.Enable();

        // first of all handle events
        utils.SetGoalEventInvoker(this);
        utils.SetHitBorderEventInvoker(this);
    }

    // Useful method to add score
    private void ShortcutGoalNorth(InputAction.CallbackContext callbackContext) { BeginGame(); goalEvent.Invoke(Utils.Opponent.North); }

    // Useful method to add score
    private void ShortcotGoalSouth(InputAction.CallbackContext callbackContext) { BeginGame(); goalEvent.Invoke(Utils.Opponent.South); }

    private void Start()
    {
        // set default values
        ballVelocity = utils.ballProperties.BallStartVelocity;
        whosTurn = Utils.Opponent.South;
        ballDirection = RandomiseBallDirection();

        // prevent ball from moving
        transform.GetChild(0).gameObject.SetActive(false);
        isMoving = false;

        // add self as listener of multiple events
        utils.AddBeginGameEventListner(BeginGame);
        utils.AddPhaseTwoEventListner(PhaseTwo);
    }

    // Reset values
    private void OnEnable() { isMoving = true; isTraumatic = false; goalSoulth.Enable(); goalNorth.Enable(); }

    private void OnDisable() { goalSoulth.Disable(); goalNorth.Disable(); }

    private void Update()
    {
        // move ball
        if (isMoving) { transform.position += new Vector3(ballDirection.x, ballDirection.y, 0) *
                (isTraumatic ? utils.ballProperties.BallMaxVelocity : ballVelocity) * Time.deltaTime; }

        // clamp position
        ClampPositionInArena();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // reflect and set new direction
        if (collision != null)
        {
            //  reflect if walls hit
            if (!collision.gameObject.CompareTag("Character"))
                SetBallDirection(Vector2.Reflect(ballDirection, collision.GetContact(0).normal));
            //  accelerate if character hit
            else if (ballVelocity < utils.ballProperties.BallMaxVelocity)
                ballVelocity += utils.ballProperties.BallAcceleration;
        }
    }

    // Reflect ball off walls
    private void ClampPositionInArena()
    {
        float ballRadius = utils.ballProperties.BallRadius;
        float arenaWidth = utils.arenaProperties.ArenaWidth;

        // reflect ball off right wall
        if (transform.position.x + ballRadius >= arenaWidth)
        {
            transform.position = new Vector3(arenaWidth - ballRadius, transform.position.y, transform.position.z);
            SetBallDirection(Vector2.Reflect(ballDirection, Vector2.left));
            SqueezeBall(false);

            // invoke hit eastern border event
            hitBorderEvent.Invoke(Arena.Border.East);
        }
        // reflect ball off left wall
        else if (transform.position.x - ballRadius <= -arenaWidth)
        {
            transform.position = new Vector3(-arenaWidth + ballRadius, transform.position.y, transform.position.z);
            SetBallDirection(Vector2.Reflect(ballDirection, Vector2.right));
            SqueezeBall(false);

            // invoke hit western border event
            hitBorderEvent.Invoke(Arena.Border.West);
        }

        // register upper wall hit
        if (transform.position.y + ballRadius >= utils.arenaProperties.ArenaHeight)
        {
            // it's winner's turn
            whosTurn = Utils.Opponent.South;

            // reset values
            SetBallDirection(RandomiseBallDirection());
            ballVelocity = utils.ballProperties.BallStartVelocity;
            transform.position = Vector3.zero;
            animator.Play("A_Ball_Respawn", 0);

            // invoke goal event for northern opponent
            goalEvent.Invoke(Utils.Opponent.South);
        }
        // register lower wall hit
        else if (transform.position.y - ballRadius <= -utils.arenaProperties.ArenaHeight)
        {
            // it's winner's turn
            whosTurn = Utils.Opponent.North;

            // reset values
            SetBallDirection(RandomiseBallDirection());
            ballVelocity = utils.ballProperties.BallStartVelocity;
            transform.position = Vector3.zero;
            animator.Play("A_Ball_Respawn", 0);

            // invoke goal event for southern opponent
            goalEvent.Invoke(Utils.Opponent.North);
        }
    }

    // React to phase two event
    private void PhaseTwo() 
    { 
        // reset values 
        transform.position = Vector3.zero; 
        ballDirection = utils.ballProperties.StartBallDirection;
        ballVelocity = utils.ballProperties.BallStartVelocity;

        // prepare for phase two
        animator.Play("A_Ball_PhaseTwo", 0);
        GetComponentsInChildren<SpriteRenderer>()[1].sprite = ballPhaseTwo;
    }

    // React to begin game event
    private void BeginGame() 
    {
        // reset values
        ballVelocity = utils.ballProperties.BallStartVelocity;
        transform.position = Vector3.zero;

        // reveal self
        transform.GetChild(0).gameObject.SetActive(true);
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("A_Ball_PhaseTwo")) 
            animator.Play("A_Ball_Respawn", 0);
    }

    private Vector3 RandomiseBallDirection() { return new Vector3(Random.Range(-1.0f, 1.0f), whosTurn == Utils.Opponent.North ? 1 : -1, 0).normalized; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets new direction of the ball's movement
    /// </summary>
    /// <param name="newDirection"> New Vector2 direction of the ball </param>
    public void SetBallDirection(Vector2 newDirection) { ballDirection = newDirection; }

    /// <summary>
    /// Gets current direction of ball's movement
    /// </summary>
    /// <returns> Vector2 direction</returns>
    public Vector2 GetBallDirection() { return ballDirection;}

    /// <summary>
    /// Freezes ball in place
    /// </summary>
    public void BallStopMoving() { isMoving = false; }

    /// <summary>
    /// Resumes ball's movement
    /// </summary>
    public void BallResumeMoving() { isMoving = true; }

    /// <summary>
    /// Makes ball play animation of squeezing in "squeeze direction" (true - vertically, false - horizontally) 
    /// </summary>
    /// <param name="squeezeDirection"> "Squeeze direction" (true - vertically, false - horizontally) </param>
    public void SqueezeBall(bool squeezeDirection) 
    { 
        animator.Play(squeezeDirection ? "Ball_Squeeze_Vertically" : "Ball_Squeeze_Horizontally", 0);
    }

    /// <summary>
    /// Makes ball react to mighty punch
    /// </summary>
    public void EnterMightyPunch()
    {
        SetBallDirection(new Vector3(0, -(ballDirection.y / Mathf.Abs(ballDirection.y)), 0));
        isTraumatic = true;
        animator.SetBool("isTraumatic", isTraumatic);
        if (ballDirection.y < 0) { GetComponent<SpriteRenderer>().flipY = true; }
    }

    /// <summary>
    /// Exit's mighty punch state
    /// </summary>
    public void ExitMightyPunch() 
    { 
        isTraumatic = false;
        animator.SetBool("isTraumatic", isTraumatic);
        GetComponent<SpriteRenderer>().flipY = false;
    }

    /// <summary>
    /// Gets if ball is traumatic at the moment
    /// </summary>
    /// <returns> Bool if ball is currently traumatic </returns>
    public bool GetIsTraumatic() { return isTraumatic; }

    /// <summary>
    /// Adds the given listener for the GoalEvent
    /// </summary>
    public void AddGoalEventListener(UnityAction<Utils.Opponent> listener)
    {
        goalEvent.AddListener(listener);
    }

    /// <summary>
    /// Removes the given listener from the GoalEvent
    /// </summary>
    public void RemoveGoalEventListener(UnityAction<Utils.Opponent> listener)
    {
        goalEvent.RemoveListener(listener);
    }

    public void AddHitBorderEventListener(UnityAction<Arena.Border> listener)
    {
        hitBorderEvent.AddListener(listener);
    }

    #endregion

}
