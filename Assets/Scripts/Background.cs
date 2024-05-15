using UnityEngine;

public class Background : MonoBehaviour
{

    [Header("Crowns Background")]
    [SerializeField] private RectTransform layer;

    // Start is called before the first frame update
    private void OnEnable()
    {
        // set default position
        layer.localPosition = new Vector3(-Screen.currentResolution.width / 2, 0, 0);
    }

    // Update is called once per frame
    private void Update()
    {
        // reset position when end reached
        if (layer.localPosition.x + Time.deltaTime * 15 < Screen.currentResolution.width / 2) { layer.localPosition += new Vector3(Time.deltaTime * 15, 0, 0); }
        else if (layer.localPosition.x == Screen.currentResolution.width / 2) { layer.localPosition = new Vector3(-Screen.currentResolution.width / 2, 0, 0); }
        else if (layer.localPosition.x + Time.deltaTime * 15 >= Screen.currentResolution.width / 2) { layer.localPosition = new Vector3(Screen.currentResolution.width / 2, 0, 0); }
    }

    /// <summary>
    /// Resets background to default position
    /// </summary>
    public void ResetBackground() { layer.localPosition = new Vector3(-Screen.currentResolution.width / 2, 0, 0); }
}
