using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_AI : Character
{

    #region Fields

    // target position AI tends to take
    private Vector3 targetPosition;

    // task AI is currently busy with
    private Task currentTask;

    // list of shields character is physically contacting
    private List<Shield> shieldsAvalible;

    // the ball
    private Ball ball;

    // diverse
    private float movementVelocity;
    private bool ballIncomming;

    #endregion

    #region Override Methods

    override protected void Awake()
    {
        // set references
        animator = GetComponent<Animator>();
        utils = FindObjectOfType<Utils>();
        ball = FindObjectOfType<Ball>();
        shieldsInContact = new List<Shield>();
        shieldsAvalible = new List<Shield>();
        currentShield = GetComponentInChildren<Shield>();
        abilityComponent = GetComponent<Ability>();
    }

    override protected void Start()
    {
        base.Start();

        // start trying to use ability
        StartCoroutine(UseAbility());
    }

    override protected void OnEnable()
    {
        // set default task
        SetTask(Task.DefaultWait);

        // set default values
        isArmed = true;
        isMoving = true;
    }

    // React to phase two event
    override protected void PhaseTwo() { base.PhaseTwo(); animator.Play("A_Character_PhaseTwo", 0); }

    // Start searching for a new shield as soon as prevoius one is broken
    override protected void DropShield(Shield shield)
    {
        base.DropShield(shield);
        SetTask(Task.DefaultWait);
        StartCoroutine(FindShieldToPick());
    }

    // Brand new clamped move for AI
    override protected void ClampedMove()
    {
        // if not buisy and the ball is close set task to hit it
        HandleIncommingBall();

        // lerp to target position if it doesn't mean leaving arena
        Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, movementVelocity * Time.deltaTime);
        if (newPos.x + utils.characterProperties.HalthCharacteridth < utils.arenaProperties.ArenaWidth &&
            newPos.x - utils.characterProperties.HalthCharacteridth > -utils.arenaProperties.ArenaWidth) { transform.position = new Vector3 (newPos.x, transform.position.y, 0); }

        // pull back and clamp
        float pullForce = Mathf.Clamp((targetPosition - transform.position).magnitude * 2, 1, 10) * Time.deltaTime;
        Vector3 defaultPosition = new Vector3(0, utils.arenaProperties.DefaultCharacterOffsetY, 0);

        // pull back vertically only if not reflecting ball currently
        if (currentTask != Task.HitBall)
        {
            if (transform.position.y < defaultPosition.y) { transform.position += new Vector3(0, 1.5f * Time.deltaTime, 0); }
            if (transform.position.y > defaultPosition.y - 0.025f) { transform.position = new Vector3(transform.position.x, defaultPosition.y, 0); }
        }

        // also pull back horizontally only if no specific task set
        if (currentTask == Task.DefaultWait)
        {
            if (transform.position.x > 0) { transform.position -= new Vector3(pullForce, 0, 0); }
            else if (transform.position.x < 0) { transform.position += new Vector3(pullForce, 0, 0); }

            if (transform.position.x < 0.025f && transform.position.x > -0.025f) { transform.position = new Vector3(0, transform.position.y, 0); }
        }

        // calculate and set rotation if unarmed
        Vector3 movementVector = (targetPosition - transform.position).normalized;
        if (!isArmed) 
        {
            if (transform.position != defaultPosition) { transform.up = new Vector3(movementVector.x, movementVector.y, 0); isMoving = true; }
            else { transform.up = Vector3.down; isMoving = false; }
        }

        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("A_Character_PhaseTwo"))
        {
            // update animator variables
            animator.SetBool("isArmed", isArmed);
            animator.SetBool("isMoving", isMoving);
        }
    }

    // If unarmed pick found shield immediately
    override protected void OnTriggerEnter2D(Collider2D collision)
    {
        // insert found shield in list of shields nearby
        if (collision.CompareTag("Shield") && !isArmed)
        {
            Shield shield = collision.GetComponent<Shield>();
            if (shield != null)
            {
                // add shield to list of shields in contact, highlight it and pick it
                if (!shieldsInContact.Contains(shield)) { shieldsInContact.Add(shield); }
                shield.Highlight();
                if (!isArmed) { PickShield(shield); }
            }
        }
    }

    override protected void PickShield(Shield shield)
    {
        base.PickShield(shield);

        // in addition to base method set task to default one and turn shield down
        shield.transform.rotation = new Quaternion(0, 0, 90, 0);
        SetTask(Task.DefaultWait);
    }

    #endregion

    #region Unique Methods

    // AI's tasks
    private enum Task { DefaultWait, HitBall, PickShield }

    // Set new task (overload fits all tasks except for pick shield task)
    private void SetTask(Task task)
    {
        currentTask = task;

        // set target position accordingly to current task
        switch (task)
        {
            case Task.DefaultWait: targetPosition = new Vector3(0, utils.arenaProperties.DefaultCharacterOffsetY, 0);
                movementVelocity = utils.aIProperties.DefaultVelocity_AI; break;
            case Task.HitBall: targetPosition = new Vector3(ball.transform.position.x, utils.arenaProperties.DefaultCharacterOffsetY, 0);
                movementVelocity = utils.aIProperties.HitBallVelocity_AI; break;
        }
    }

    // Set new task (overload for pick shield task)
    private void SetTask(Shield shield)
    {
        currentTask = Task.PickShield;
        targetPosition = shield.transform.position;
        movementVelocity = utils.aIProperties.DefaultVelocity_AI;
    }

    // Try use ability
    private IEnumerator UseAbility()
    {
        // loop method
        while (true)
        {
            // try only if ball is moving towards AI and currently locates close to middle of arena, also try only from idle state
            if (isArmed && !abilityComponent.isOnCooldown() && ball.transform.position.y > -utils.characterProperties.HalthCharacteridth &&
                ball.transform.position.y < utils.characterProperties.HalthCharacteridth && ball.GetBallDirection().y >= 0 &&
                animator.GetCurrentAnimatorStateInfo(0).IsName("A_Character_Armed_Idle"))
            {
                // use ability with 10% possibility
                if (UnityEngine.Random.Range(0, 10) == 0)
                    abilityComponent.UseAbility(currentShield.GetShieldType());
            }

            // try again after some time
            yield return new WaitForSeconds(0.4f);
        }
    }

    // Rush towards ball if it's close
    private void HandleIncommingBall()
    {
        // set task
        if (isArmed) { SetTask(ball.transform.position.y > 0 && ball.GetBallDirection().y > 0 ? Task.HitBall : Task.DefaultWait); }

        // parry if ball is incomming
        if (ball.transform.position.y > utils.arenaProperties.CharacterCageWidth && ball.GetBallDirection().y > 0 && !ballIncomming)
        {
            // parry with 33% possibility
            ballIncomming = true;
            isParrying = UnityEngine.Random.Range(0, 3) == 0 ? false : true;
        }
        else if (ball.transform.position.y < utils.arenaProperties.CharacterCageWidth || ball.GetBallDirection().y < 0) { ballIncomming = false;  isParrying = false; }

        // rush towards ball 
        if (ballIncomming) { transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, ball.transform.position.y, 0), utils.aIProperties.HitBallVelocity_AI * Time.deltaTime); }
    }

    // Find closest shield to pick
    private IEnumerator FindShieldToPick()
    {
        // find shields inside character's cage, keep trying until one is found
        shieldsAvalible.Clear();
        while (shieldsAvalible.Count == 0)
        {
            // avoid interrupting phase two animation
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("A_Character_PhaseTwo"))
            {
                Shield[] shields = FindObjectsOfType<Shield>();

                // sort shields so that only avalible ones are left
                foreach (Shield shield in shields)
                    if (shield.transform.position.y < utils.arenaProperties.ArenaHeight && shield.transform.position.y > utils.arenaProperties.CharacterCageWidth &&
                        shield.GetStatus() != Shield.ShieldStatus.Broken) { shieldsAvalible.Add(shield); }
            }
            yield return new WaitForEndOfFrame();
        }

        // choose closest shield to pick
        Shield closestShield = null;
        if (shieldsAvalible.Count > 0)
        {
            closestShield = shieldsAvalible[0];
            float distance = (transform.position - closestShield.transform.position).magnitude;
            for (int i = 0; i < shieldsAvalible.Count; i++)
                if (distance < (transform.position - shieldsAvalible[i].transform.position).magnitude)
                {
                    distance = (transform.position - shieldsAvalible[i].transform.position).magnitude;
                    closestShield = shieldsAvalible[i];
                }
        }

        // set task to pick the shield
        if (closestShield != null) { SetTask(closestShield); }
        else { SetTask(Task.DefaultWait); }
    }

    #endregion

}
