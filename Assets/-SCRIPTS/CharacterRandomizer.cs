using UnityEngine;

public class CharacterRandomizer : MonoBehaviour
{
    [Header("Freak State")]
    [SerializeField] public bool freakInElevator = false;

    [Header("Values")]
    [SerializeField] private int skinColor;
    [SerializeField] private int hatType;
    [SerializeField] private int personality;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Randomize();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            freakInElevator = true;
        }
    }

    void Randomize()
    {
        if (freakInElevator)
        {
            return;
        }
        skinColor = Random.Range(0, 6);
        hatType = Random.Range(0, 3);
        personality = Random.Range(0, 3);
    }
}
