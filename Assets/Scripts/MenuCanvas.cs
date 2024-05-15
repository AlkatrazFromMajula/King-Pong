using UnityEngine;


public class MenuCanvas : MonoBehaviour
{
    [SerializeField] private MenuType menuType;

    public enum MenuType { Main, Pause, Result, Options, Library }

    /// <summary>
    /// Gets menu type of canvas
    /// </summary>
    /// <returns> Menue type this canvas represents </returns>
    public MenuType GetMenuType() { return menuType; }
}


