using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] GameObject peasant;
    [SerializeField] GameObject king;

    // Instantiate chosen characters 
    private void Start() 
    {
        // make character player or AI driven
        if (transform.position.y >= 0) { SelectCharacter(DataManager.Instance.northCharacter).AddComponent<Character_AI>(); }
        else { SelectCharacter(DataManager.Instance.southCharacter).AddComponent<Character>(); }

        // destroy self
        Destroy(gameObject);
    }

    // Select character according to name
    private GameObject SelectCharacter(Utils.Character characterName)
    {
        switch (characterName)
        {
            case Utils.Character.Peasant: return Instantiate(peasant, transform.position, transform.rotation);
            case Utils.Character.King: return Instantiate(king, transform.position, transform.rotation);
            default: return null;
        }

    }
}
