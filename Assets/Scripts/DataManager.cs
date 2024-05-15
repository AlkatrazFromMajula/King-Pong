using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public Utils.Character southCharacter;
    public Utils.Character northCharacter;

    private void Awake()
    {
        // destroy if instance already exists
        if (Instance != null) { Destroy(gameObject); return; }

        // set this as instance
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
