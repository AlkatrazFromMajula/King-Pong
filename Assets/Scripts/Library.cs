using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Library : MonoBehaviour, ISelectHandler, IDeselectHandler
{

    #region Fields

    // fields nicely serialized in inspector
    [Header("Library Category")]
    [SerializeField] Category category;
    [Header("Characters")]
    [SerializeField] Utils.Character[] characters;
    [SerializeField] Image icon;
    [SerializeField] Sprite[] iconsNormal;
    [SerializeField] Sprite[] iconsHighlighted;
    [Header ("Arrows")]
    [SerializeField] Image rightArrow;
    [SerializeField] Image leftArrow;
    [SerializeField] Sprite[] rightArrows;
    [SerializeField] Sprite[] leftArrows;

    // input
    IA input;
    InputAction scroll;

    // diverse
    private int currentIconIndex = 0;

    #endregion

    #region Private Methods

    private void Awake() 
    { 
        // set input but don't activate it yet
        input = new IA();
        scroll = input.Menu.Scroll;
        scroll.started += Scroll;
    }

    // Add self as total mute event listener
    private void Start() { FindObjectOfType<Utils>().AddTotalMuteEventListner(TotalMute); }

    // Select character library first
    private void OnEnable() { if (category == Category.Character) { GetComponent<Button>().Select(); } }

    // Deactivate input
    private void OnDisable() { if (scroll.enabled) { scroll.Disable(); } }

    // Deactivate input
    private void TotalMute() { if (scroll.enabled) { scroll.Disable(); } }

    #endregion

    #region Public Methods

    // Highlight arrows to show selection
    public void OnSelect(BaseEventData eventData)
    {
        if (eventData.selectedObject == gameObject)
        {
            // highlight and enable input
            SetArrows(true);
            scroll.Enable();
        }
    }

    // Unhighlight arrows to show deselection
    public void OnDeselect(BaseEventData eventData)
    {
        // unhighlight and deactivate input
        SetArrows(false);
        scroll.Disable();
    }

    // Confirm option selection
    public void ConfirmChoice()
    {
        // confirm option choice if icon has a highlighted variant
        if (currentIconIndex < iconsHighlighted.Length && icon.sprite == iconsNormal[currentIconIndex])
        {
            // highlight icon
            icon.sprite = iconsHighlighted[currentIconIndex];

            // save choice in data manager
            if (category == Category.Character) { DataManager.Instance.southCharacter = characters[currentIconIndex]; }
            else { DataManager.Instance.northCharacter = characters[currentIconIndex]; }

            // share choice with menu script
            GetComponentInParent<Menu>().MakeChoice(category, true);
        }
        // otherwise deselect option
        else
        {
            // unhighlight icon
            icon.sprite = iconsNormal[currentIconIndex];

            // save choice in data manager
            if (category == Category.Character) { DataManager.Instance.southCharacter = Utils.Character.None; }
            else { DataManager.Instance.northCharacter = Utils.Character.None; }

            // share choice with menu script
            GetComponentInParent<Menu>().MakeChoice(category, false);
        }
    }

    public enum Category { Character, Level }

    #endregion

    #region Scroll

    // Scroll library in given direction (input overload)
    public void Scroll(InputAction.CallbackContext callbackContext)
    {
        // transform input into positive/negative form
        float input = callbackContext.ReadValue<float>();
        int direction = input >= 0 ? 1 : -1;
        if (direction != 0) { direction = direction > 0 ? 1 : -1; }

        // highlight arrows
        if (direction > 0 && currentIconIndex < iconsNormal.Length - 1 ||
            direction < 0 && currentIconIndex > 0) StartCoroutine(HighlightArrow(direction));

        // set new icon
        currentIconIndex = (int)Mathf.Clamp(currentIconIndex + direction, 0, iconsNormal.Length - 1);
        icon.sprite = iconsNormal[currentIconIndex];
        
        // remove previous selection 
        if (category == Category.Character) { DataManager.Instance.southCharacter = Utils.Character.None; }
        else { DataManager.Instance.northCharacter = Utils.Character.None; }
        GetComponentInParent<Menu>().MakeChoice(category, false);
    }

    // Scroll library in given direction (button overload)
    public void Scroll(int direction)
    {
        // set new icon
        currentIconIndex = Mathf.Clamp(currentIconIndex + direction, 0, iconsNormal.Length - 1);
        icon.sprite = iconsNormal[currentIconIndex];

        // highlight arrows
        SetArrows(true);

        // remove previous selection 
        if (category == Category.Character) { DataManager.Instance.southCharacter = Utils.Character.None; }
        else { DataManager.Instance.northCharacter = Utils.Character.None; }
        GetComponentInParent<Menu>().MakeChoice(category, false);

        // select this library
        GetComponent<Button>().Select();
    }

    // Set new arrows sprites
    private void SetArrows(bool isSelected)
    {
        // highlight if selected
        if (isSelected)
        {
            // deside whether to highlight right arrow
            if (currentIconIndex < iconsNormal.Length - 1) { rightArrow.sprite = rightArrows[1]; }
            else { rightArrow.sprite = rightArrows[0]; }

            // deside whether to highlight left arrow
            if (currentIconIndex > 0) { leftArrow.sprite = leftArrows[1]; }
            else { leftArrow.sprite = leftArrows[0]; }
        }
        // otherwise unhighlight
        else
        {
            rightArrow.sprite = rightArrows[0];
            leftArrow.sprite = leftArrows[0];
        }
    }

    // Blink arrow core once to visualise scroll
    private IEnumerator HighlightArrow(int direction)
    {
        if (direction > 0) { rightArrow.sprite = rightArrows[2]; }
        else { leftArrow.sprite = leftArrows[2]; }
        yield return new WaitForSeconds(0.1f);
        SetArrows(true);
    }

    #endregion

}
