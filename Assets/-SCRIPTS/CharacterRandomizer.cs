using UnityEngine;
using UnityEngine.UIElements;

public class CharacterRandomizer : MonoBehaviour
{
    public Elevator elevator;

    [Header("Freak State")]
    [SerializeField] public bool freakInElevator = false;
    [SerializeField] public int requestFloor;

    [Header("Values")]
    [SerializeField] private Color skinColor;
    [SerializeField] private Color mouthColor;
    [SerializeField] private Color earColor;
    [SerializeField] private int voiceType;
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
        skinColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.7f, 1f); //***change all the values to work with RGB***//
        mouthColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.7f, 1f);
        earColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.7f, 1f);

        voiceType = Random.Range(0, 3); //***change range later based on amount of voices***//
        personality = Random.Range(0, 3);

        requestFloor = Random.Range(0, elevator.currentFloor); //calls a random number to be selected as the requested floor
    }
}
