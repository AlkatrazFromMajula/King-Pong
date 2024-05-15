using System.Collections;
using UnityEngine;

public class Shield : MonoBehaviour
{
    #region Fields

    // arrays with sprites
    // sprites should be added in following order: new -> used -> harmed -> broken
    [SerializeField] private Sprite[] equiped_Normal;
    [SerializeField] private Sprite[] equiped_Highlighted;
    [SerializeField] private Sprite[] equiped_Damaged;
    [SerializeField] private Sprite[] unequiped_Normal;
    [SerializeField] private Sprite[] unequiped_Highlighted;
    [SerializeField] private Utils.ShieldType shieldType;

    // references
    Utils utils;
    Animator animator;
    SpriteRenderer spriteRenderer;

    // fields
    private ShieldStatus status;
    private bool isEquiped;
    private int endurance;
    private bool isParrying;

    #endregion

    #region Private Methods

    private void Awake()
    {
        // set references
        utils = FindObjectOfType<Utils>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // Set shield's default values
    private void Start() { endurance = utils.GetShieldEndurance(shieldType); status = ShieldStatus.New; }

    private void OnEnable()
    {
        // reset values
        isParrying = false;
        isEquiped = transform.parent.CompareTag("Character");
        GetComponent<BoxCollider2D>().enabled = !isEquiped;
        SetSprite(false);
    }

    // Sets sprite
    private void SetSprite(bool isHighlighted)
    {
        // change rendering order
        spriteRenderer.sortingOrder = isEquiped ? 1 : 0;

        // choose proper array
        Sprite[] sprites;
        if (isEquiped) { sprites = isHighlighted ? (isParrying ? equiped_Highlighted : equiped_Damaged) : equiped_Normal; }
        else { sprites = isHighlighted ? unequiped_Highlighted : unequiped_Normal; }

        // set proper sprite
        switch (status)
        {
            case ShieldStatus.New: spriteRenderer.sprite = sprites[0]; break;
            case ShieldStatus.Used: spriteRenderer.sprite = sprites[1]; break;
            case ShieldStatus.Harmed: spriteRenderer.sprite = sprites[2]; break;
            default: if (sprites.Length > 3) { spriteRenderer.sprite = sprites[3]; StartCoroutine(Destroy()); } break;
        }
    }

    // Blinck for some time and destroy self
    private IEnumerator Destroy() 
    {
        for (int i = 0; i < 6; i++) 
        {
            GetComponentInChildren<SpriteRenderer>().enabled = false;
            yield return new WaitForSeconds(0.3f);
            GetComponentInChildren<SpriteRenderer>().enabled = true;
            yield return new WaitForSeconds(0.3f);
        }
        Destroy(gameObject); 
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Highlights shield
    /// </summary>
    public void Highlight() { SetSprite(true); }

    /// <summary>
    /// Removes highlight from shield
    /// </summary>
    public void Unhighlight() { SetSprite(false); }

    /// <summary>
    /// Possible shield's states
    /// </summary>
    public enum ShieldStatus { New, Used, Harmed, Broken }

    /// <summary>
    /// Sets currently equiped shield's status
    /// </summary>
    /// <param name="shieldStatus"> Shield's enum status </param>
    /// <param name="equiped"> Whether shield is equiped or not </param>
    public void SetStatus(ShieldStatus shieldStatus, bool equiped) 
    { 
        status = shieldStatus; 
        isEquiped = equiped;
        if (!equiped) { isParrying = false; }
        SetSprite(false);
    }

    /// <summary>
    /// Gets shield status
    /// </summary>
    /// <returns> Returns enumerator shield current status </returns>
    public ShieldStatus GetStatus() { return status; }

    /// <summary>
    /// Sets if character is parrying at the moment to true
    /// </summary>
    public void Parry() { isParrying = true; }

    /// <summary>
    /// Sets if character is parrying at the moment to false
    /// </summary>
    public void CancelParry() { isParrying = false; }

    /// <summary>
    /// Makes currently equiped shield play a hit ball animation
    /// </summary>
    public void HitBall(bool parry) 
    { if (animator.GetCurrentAnimatorStateInfo(0).IsName("A_Shield_Idle")) { animator.Play(parry ? "A_Shield_ParryBall" : "A_Shield_HitBall", 0); } }

    /// <summary>
    /// Reduces currently equiped shield's endurance by a certain value
    /// </summary>
    /// <param name="value"> Integer value to reduce endurance by </param>
    public void ReduceEndurance(int value) 
    {
        int initialEndurance = utils.GetShieldEndurance(shieldType);
        endurance = endurance - value > 0 ? endurance - value : 0;

        if (endurance > initialEndurance / 3 * 2) { SetStatus(ShieldStatus.New, true); }
        else if (endurance > initialEndurance / 3 && endurance <= initialEndurance / 3 * 2) { SetStatus(ShieldStatus.Used, true); }
        else if (endurance > 0 && endurance <= initialEndurance / 3) { SetStatus(ShieldStatus.Harmed, true); }
        else { SetStatus(ShieldStatus.Broken, false); }
    }

    /// <summary>
    /// Gets shield's remaining endurance
    /// </summary>
    /// <returns> An integer representation of how much endurance currently equiped shield has left </returns>
    public int GetEndurance() {  return endurance; }

    /// <summary>
    /// Gets shield type
    /// </summary>
    /// <returns> Return type of the shield </returns>
    public Utils.ShieldType GetShieldType() { return shieldType;} 

    #endregion
}
