using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class CharacterRandomizer : MonoBehaviour
{
    public Elevator elevator;
    public GameObject freakPrefab;

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

    public IEnumerator FreakSpawner()
    {
        Debug.Log("FreakSpawner is running!");

        Randomize();

        Instantiate(freakPrefab);
        freakInElevator = true;
        elevator.canPress = false;

        #region Greetings Audio
        //GREETINGS
        switch (voiceType)
        {
            case 0:
                break;
            case 1:
                break;
            case 2:
                break;
        }
        #endregion

        float waitTime = 7.5f;
        Debug.Log("Waiting " + waitTime + " seconds before moving..."); //this is so the freak has time to enter without moving the elevator
        yield return new WaitForSeconds(waitTime);

        StartCoroutine(FreakEntersElevator());
    }

    private IEnumerator FreakEntersElevator()
    {
        Debug.Log("FreakEnterElevator is running...");
        //***put request audio here***//

        elevator.canPress = true;

        float waitTime = Random.Range(4f, 8f);
        Debug.Log("Waiting " + waitTime + " seconds before moving..."); //this is so the elevator gives time to press the request button before leaving
        yield return new WaitForSeconds(waitTime);

        Debug.Log("Closing Elevator!");

        StartCoroutine(elevator.ElevatorClosing());
    }

    public void DetermineNextLocation(int pressedFloorIndex)
    {
        if (requestFloor == pressedFloorIndex)
        {
            elevator.priorityRequestActive = true;
        }
        else
        {
            elevator.priorityClosestActive = true;
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

        #region Voice/Personality Key
        //Voices
        //0 = Deep
        //1 = Frog
        //2 = Spain
        //Personality
        //0 = Good
        //1 = Neutral Sorry
        //2 = Bad
        //3 = Bad Sorry
        #endregion
        voiceType = Random.Range(0, 2); //***change range later based on amount of voices***//
        personality = Random.Range(0, 3);

        requestFloor = Random.Range(0, elevator.currentFloor); //calls a random number to be selected as the requested floor
    }

    public void SayGreeting()
    {

    }

    public void SayNumber()
    {

    }

    public void SayDialogue()
    {

    }

    public void SayRemarks()
    {

    }
}
