using UnityEngine;

public class CharacterRandomizer : MonoBehaviour
{
    public Elevator elevator;

    [Header("Freak State")]
    [SerializeField] public bool freakInElevator = false;
    [SerializeField] public int requestFloor;

    [Header("Values")]
    [SerializeField] private int skinColor;
    [SerializeField] private int hatType;
    [SerializeField] private int personality;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) //***TEST INPUT; CHANGE LATER***
        {
            Randomize();

            AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position); //test sound; runs from FMOD hardcoded into script
        }

        if (Input.GetKeyDown(KeyCode.T)) //***TEST INPUT; CHANGE LATER***
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

        ///SCALABLE PARAMETERS///
        skinColor = Random.Range(0, 6);
        hatType = Random.Range(0, 3);
        personality = Random.Range(0, 3);

        requestFloor = Random.Range(0, elevator.currentFloor); //calls a random number to be selected as the requested floor
    }
}
