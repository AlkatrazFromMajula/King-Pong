using System.Collections;
using UnityEngine;

public class Ability : MonoBehaviour
{
    #region Fields

    // Select ability type
    [SerializeField] private AbilityType abilityType;

    // references
    Utils utils;
    PlayerCard playerCard;

    // diverse
    private bool abilityCooldown = false;
    private bool abilityPossible = true;

    #endregion

    #region Private Methods

    private void Awake()
    {
        // set references
        PlayerCard[] playerCards = FindObjectsOfType<PlayerCard>();
        foreach (PlayerCard card in playerCards)
        {
            if (transform.position.y >= 0 && card.transform.position.y >= 0 ||
                transform.position.y < 0 && card.transform.position.y < 0) { playerCard = card; }
        }
        utils = FindObjectOfType<Utils>();
    }

    // Delay ability availability
    private void Start() { StartCoroutine(AbilityCooldown(utils.GetAbilityCooldownTime(abilityType))); }

    // Perform cooldown
    private IEnumerator AbilityCooldown(float cooldownTime)
    {
        // unhighlight character's icon
        playerCard.HighlightIcon(false);

        // wait for ability cooldown to pass
        abilityCooldown = true;
        float time = cooldownTime;
        while (time > 0)
        {
            time -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        // highlight character's icon
        playerCard.HighlightIcon(abilityPossible);
        abilityCooldown = false;
    }

    #endregion

    #region Abilities

    // Royal order functionality
    private IEnumerator RoyalOrder()
    {
        // call cooldown
        StartCoroutine(AbilityCooldown(utils.GetAbilityCooldownTime(abilityType)));

        // play animation
        GetComponentInChildren<Animator>().Play("A_Character_Ability", 0);

        // reverse ball direction
        Ball ball = FindObjectOfType<Ball>();
        Animator ballAnimator = ball.GetComponent<Animator>();
        ballAnimator.Play("A_Ball_RoyalOrder", 0);
        yield return new WaitForEndOfFrame();
        ball.BallStopMoving();

        // wait for ball to finish playing animation
        while (ballAnimator.GetCurrentAnimatorStateInfo(0).IsName("A_Ball_RoyalOrder")) { yield return new WaitForEndOfFrame(); }

        ball.SetBallDirection(-ball.GetBallDirection());
        ball.BallResumeMoving();
    }

    // Mighty Punch functionality
    private IEnumerator MightyPunch()
    {
        // call cooldown
        StartCoroutine(AbilityCooldown(utils.GetAbilityCooldownTime(abilityType)));

        // play animation
        GetComponentInChildren<Shield>().GetComponent<Animator>().Play("A_Shield_MightyPunch", 0);
        Animator animator = GetComponent<Animator>();
        animator.Play("A_Character_Ability", 0);
        yield return new WaitForEndOfFrame();

        // rush forward 
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("A_Character_Ability") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.3)
        {
            float deltaY = 2 * utils.characterProperties.CharacterVelocity * Time.deltaTime;
            transform.position += new Vector3(0, deltaY, 0);
            yield return new WaitForEndOfFrame();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Character's abilities
    /// </summary>
    public enum AbilityType { RoyalOrder, MightyPunch }

    /// <summary>
    /// Uses character's ability
    /// </summary>
    public void UseAbility(Utils.ShieldType shieldType)
    {
        // use given ability
        if (!abilityCooldown && abilityPossible)
        {
            if (abilityType == AbilityType.RoyalOrder && shieldType == Utils.ShieldType.Royal) { StartCoroutine(RoyalOrder()); }
            else if (abilityType == AbilityType.MightyPunch && shieldType == Utils.ShieldType.Wheel) { StartCoroutine(MightyPunch()); }
        }
    }

    /// <summary>
    /// Gets if ability is currently on cooldown
    /// </summary>
    /// <returns> True if ability is cooling down and can't be used </returns>
    public bool isOnCooldown() { return abilityCooldown; }

    /// <summary>
    /// Sets if it's possible to use an ability or not
    /// </summary>
    /// <param name="isPossibile"> New bool value of field isPossible</param>
    public void SetAbilityPossibility(bool isPossibile)
    {
        abilityPossible = isPossibile;
        playerCard.HighlightIcon(abilityPossible && !abilityCooldown);
    }

    /// <summary>
    /// Sets if it's possible to use an ability, based on shield's type
    /// </summary>
    /// <param name="isPossibile"> New bool value of field isPossible</param>
    public void SetAbilityPossibility(Utils.ShieldType shieldType)
    {
        abilityPossible = utils.CompareShieldWithAbility(shieldType, abilityType);
        playerCard.HighlightIcon(abilityPossible && !abilityCooldown);
    }

    #endregion

}
