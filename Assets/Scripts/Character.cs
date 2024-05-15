using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Character : MonoBehaviour
{

    #region Fields

    // diverse
    protected bool isArmed = true;
    protected bool isMoving;
    private bool parryCooldown;
    private bool isMightyPunching;
    protected bool isParrying;

    // currently equiped shield 
    protected Shield currentShield;

    // list of shields character is physically contacting
    protected List<Shield> shieldsInContact;

    // references
    protected Animator animator;
    protected Ability abilityComponent;
    protected Utils utils;

    // input
    private IA input;
    private InputAction parry;
    private InputAction ability;
    private InputAction interact;
    private InputAction move;

    #endregion

    #region MonoBehaviour Methods

    virtual protected void Awake()
    {
        // set referemces
        animator = GetComponentInChildren<Animator>();
        utils = FindObjectOfType<Utils>();
        shieldsInContact = new List<Shield>();
        currentShield = GetComponentInChildren<Shield>();
        abilityComponent = GetComponent<Ability>();
        input = new IA();
    }

    virtual protected void Start()
    {
        // add self as listener of multiple events
        utils.AddPhaseTwoEventListner(PhaseTwo);
        utils.AddBeginGameEventListner(Begingame);
    }

    virtual protected void OnEnable()
    {
        // set and activate input
        interact = input.Player.Interact;
        interact.performed += Interact;
        interact.Enable();

        ability = input.Player.Ability;
        ability.started += UseAbility;
        ability.Enable();

        parry = input.Player.Parry;
        parry.started += StartParryCountdown;
        parry.Enable();

        move = input.Player.Move;
        move.Enable();
    }

    virtual protected void OnDisable()
    {
        // deactivate input
        if (input != null) 
        {
            interact.Disable();
            parry.Disable();
            move.Disable();
            ability.Disable();
            StopAllCoroutines();
        }
    }

    // Process movement
    protected void Update() { ClampedMove(); }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("Ball"))
        {
            // reflect ball, hit shield and play parry animation
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("A_Character_Armed_Idle") || isMightyPunching)
            {
                Ball ball = collision.gameObject.GetComponent<Ball>();

                // hit shield, consider that ball might be traumatic
                HitShield(ball.GetIsTraumatic());
                if (!isMightyPunching) { animator.Play(isParrying && !ball.GetIsTraumatic() ? "A_Character_ParryBall" : "A_Character_HitBall", 0); }
                if (ball.GetIsTraumatic()) { ball.ExitMightyPunch(); }

                // reflect ball
                ReflectBall(collision.gameObject.GetComponent<Ball>());
                isParrying = false;
            }
        }
    }

    virtual protected void OnTriggerEnter2D(Collider2D collision)
    {
        // insert found shield in list of shields found nearby
        if (collision.CompareTag("Shield") && !isArmed)
        {
            Shield shield = collision.GetComponent<Shield>();
            if (shield != null)
            {
                if (!shieldsInContact.Contains(shield)) { shieldsInContact.Add(shield); }
                shield.Highlight();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // remove left shield from list of shields nearby
        if (collision.CompareTag("Shield") && !isArmed)
        {
            Shield shield = collision.GetComponent<Shield>();
            if (shield != null)
            {
                if (shieldsInContact.Contains(shield)) { shieldsInContact.Remove(shield); }
                shield.Unhighlight();
            }
        }
    }

    #endregion

    #region Movement

    // Moves character inside arena
    virtual protected void ClampedMove()
    {
        // read input 
        float input = move.ReadValue<float>();
        isMoving = input != 0;

        if (isMoving)
        {
            // calculate absolut value
            input = input > 0 ? 1 : -1;

            // calculate delta position and supposed new position
            float deltaPos = input * utils.characterProperties.CharacterVelocity * Time.deltaTime;
            Vector3 newPos = new Vector3(transform.position.x + deltaPos, transform.position.y, 0);

            // perform clamped movement
            if (newPos.x + utils.characterProperties.HalthCharacteridth < utils.arenaProperties.ArenaWidth &&
                newPos.x - utils.characterProperties.HalthCharacteridth > -utils.arenaProperties.ArenaWidth) { transform.position = newPos; }

            // calculate rotation if unarmed
            if (!isArmed) { transform.up = new Vector3(input, 0, 0); }
        }

        // pull character back to default position
        Vector3 defaultPosition = new Vector3(0, -utils.arenaProperties.DefaultCharacterOffsetY, 0);
        float pullBackForce = Mathf.Clamp((defaultPosition - transform.position).magnitude * 4, 1, 10) * Time.deltaTime;

        // pull back vertically only if not currently parrying
        if (!isParrying)
        {
            if (transform.position.y > defaultPosition.y) { transform.position -= new Vector3(0, 1.5f * Time.deltaTime, 0); }
            if (transform.position.y < defaultPosition.y + 0.025f) { transform.position = new Vector3(transform.position.x, defaultPosition.y, 0); }
        }
        // pull back horizontally only if not currently moving
        if (!isMoving)
        {
            if (transform.position.x > 0) { transform.position -= new Vector3(pullBackForce, 0, 0); }
            else if (transform.position.x < 0) { transform.position += new Vector3(pullBackForce, 0, 0); }

            if (transform.position.x < 0.025f && transform.position.x > -0.025f) { transform.position = new Vector3(0, transform.position.y, 0); }
        }

        // update animator variables
        animator.SetBool("isArmed", isArmed);
        animator.SetBool("isMoving", isMoving);
    }

    #endregion

    #region Shield

    // Drop shield
    virtual protected void DropShield(Shield shield)
    {
        // prepare shield for drop
        shield.SetStatus(shield.GetStatus(), false);
        if (shield.GetStatus() != Shield.ShieldStatus.Broken) { shield.transform.GetComponent<BoxCollider2D>().enabled = true; }
        shield.GetComponent<Animator>().enabled = false;
        shield.transform.position = transform.position;
        shield.transform.up = Vector3.up;
        shield.transform.parent = null;

        // change colliders from paddle to circle trigger
        GetComponent<CapsuleCollider2D>().enabled = true;
        GetComponent<BoxCollider2D>().enabled = false;

        // set new values
        currentShield = null;
        isArmed = false;

        // avoid using ability without shield
        abilityComponent.SetAbilityPossibility(false);
    }

    // Handle interaction, since only interaction with shields has been implemented consider this as "pick shield"
    private void Interact(InputAction.CallbackContext callbackContext) { if (shieldsInContact.Count > 0 && shieldsInContact[0] != null) { PickShield(shieldsInContact[0]); } }

    virtual protected void PickShield(Shield shield)
    {
        if (shield != null)
        {
            // change colliders from trigger to paddle
            GetComponent<CapsuleCollider2D>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = true;

            // avoid interrupting phase two animation 
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("A_Character_PhaseTwo")) { animator.Play("A_Character_Armed_Idle", 0); }

            // set values
            currentShield = shield;
            isArmed = true;

            // set if ability is possible
            abilityComponent.SetAbilityPossibility(currentShield.GetShieldType());

            // turn to opponent
            transform.up = transform.position.y <= 0 ? Vector2.up : Vector2.down;

            // prepare shield for picking up
            shield.SetStatus(shield.GetStatus(), true);
            shield.GetComponent<Animator>().enabled = true;
            shield.transform.GetComponent<BoxCollider2D>().enabled = false;
            shield.transform.rotation = new Quaternion(0, 0, 0, 0);
            shield.transform.parent = transform;

        }
    }

    // Hide shield
    protected void HideShield() { if (currentShield != null) { currentShield.gameObject.SetActive(false); } }

    // Reveal shield 
    protected void RevealShield() { if (currentShield != null) { currentShield.gameObject.SetActive(true); } }

    // Damage shield
    private void HitShield(bool ballIsTraumatic)
    {
        // decide whither to throw away broken shield
        if (currentShield != null)
        {
            // don't reduce shield's endurance if currently parrying
            int damage = isParrying && !ballIsTraumatic ? 0 : 1;
            if (currentShield.GetEndurance() - damage > 0)
            {
                currentShield.ReduceEndurance(damage);
                currentShield.HitBall(isParrying && !ballIsTraumatic);
            }
            else
            {
                currentShield.ReduceEndurance(damage);
                DropShield(currentShield);
            }
        }
    }

    #endregion

    #region Ability

    // Set variables responsible for mighty punch ability to true
    public void EnterMightyPunch() { isParrying = true; isMightyPunching = true; }

    // Set variables responsible for mighty punch ability to false
    public void ExitMightyPunch() { isParrying = false; isMightyPunching = false; }

    // Use ability
    private void UseAbility(InputAction.CallbackContext callbackContext)
    {
        if (!parryCooldown && animator.GetCurrentAnimatorStateInfo(0).IsName("A_Character_Armed_Idle"))
            abilityComponent.UseAbility(currentShield.GetShieldType());
    }

    // React to phase two event
    virtual protected void PhaseTwo() { abilityComponent.SetAbilityPossibility(false); }

    // Recat to begin game event
    private void Begingame() { abilityComponent.SetAbilityPossibility(true); }

    #endregion

    #region Parry

    // Is needed to call an IEnumerator
    private void StartParryCountdown(InputAction.CallbackContext callbackContext)
    {
        if (isArmed && !isParrying && !parryCooldown)
        {
            StartCoroutine(Parry());
            StartCoroutine(ParryCooldown());
        }

    }
    private IEnumerator Parry()
    {
        // rush forward in attempt to parry
        isParrying = true;
        float time = utils.characterProperties.ParryTime;
        while (time > 0 && isParrying)
        {
            time -= Time.deltaTime;
            float deltaY = 2 * utils.characterProperties.CharacterVelocity * Time.deltaTime;
            transform.position += new Vector3(0, deltaY, 0);

            yield return new WaitForEndOfFrame();
        }

        // stop parrying
        isParrying = false;
    }

    // Prohibit parry for some time
    private IEnumerator ParryCooldown()
    {
        parryCooldown = true;
        yield return new WaitForSeconds(utils.characterProperties.ParryCountdownTime);
        parryCooldown = false;
    }

    // Reflect the ball off the shield 
    virtual protected void ReflectBall(Ball ball)
    {
        if (ball != null)
        {
            // mighty punch ability has to be handled separately
            if (!isMightyPunching)
            {
                // calculate new ball direction
                float ballOffsetFromPaddleCenter = transform.position.x -
                    ball.transform.position.x;
                float normalizedBallOffset = ballOffsetFromPaddleCenter /
                    utils.arenaProperties.CharacterCageWidth;
                float angleOffset = normalizedBallOffset * utils.ballProperties.BallBounceAngle * Mathf.Deg2Rad;
                float angle = Mathf.PI / 2 + angleOffset;
                if (transform.position.y >= 0) { angle *= -1; }
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // set new ball direction
                ball.SqueezeBall(true);
                ball.SetBallDirection(direction);
            }
            // punch ball as mighty as you can
            else if (ball.GetBallDirection().y * (transform.position - ball.transform.position).y > 0) { ball.EnterMightyPunch(); }
        }
    }

    #endregion

}
