using UnityEngine;

public class ShieldPull : MonoBehaviour
{
    [SerializeField] GameObject shield;

    // Start is called before the first frame update
    void Start() { SpawnShield(); }

    // bring new shield
    private void OnTransformChildrenChanged() { GetComponent<Animator>().SetBool("shieldIsNeeded", true); }

    // spawn shield
    private void SpawnShield()
    {
        Instantiate(shield, transform.position, new Quaternion(0, 0, 45, 45), transform);
        GetComponent<Animator>().SetBool("shieldIsNeeded", false);
    }
}
