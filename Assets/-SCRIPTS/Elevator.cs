using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using TMPro;
using FMODUnity;
using FMOD.Studio;
using static UnityEngine.GraphicsBuffer;

public class Elevator : MonoBehaviour
{
    public CharacterRandomizer characterRandomizer;
    public Rooms rooms;
    public Animator elevatorAnim;
    public TextMeshProUGUI elevatorDisplayNumber;

    [Header("Coroutine")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] public bool canPress = true;
    [SerializeField] public bool buttonPressed = false;
    [SerializeField] private bool stopFloorSet = false; //bool setup to prevent stopFloor from changing every coroutine run

    [Header("Priority List")]
    [SerializeField] public bool priorityRequestActive;
    [SerializeField] public bool priorityClosestActive;
    [SerializeField] public bool priorityWinConActive;

    #region Priority List
    // ***Requested Floor (makes sure as long as you press the requested floor, the freak won't exit to another random floor, ruining your intentions)***
    // ***Closest Floor (if requested floor is not selected, dropping the freak off as soon as possible is the immediate priority)***
    // ***Win Con Floor (if neither condition is met, then the win con floor is aimed for; the ONLY way to get to the win con floor should be if the... 
    // ...requested and closest floors are ignored when a freak is in the elevator, in other words, only the win con is selected)***
    #endregion

    [Header("Floors")]
    [SerializeField] public List<bool> floorCount = new List<bool>(new bool[30]);
    [SerializeField] public int currentFloor;
    [SerializeField] public int stopFloor;
    [SerializeField] public int winConFloor;

    void Start()
    {
        currentFloor = Random.Range(0, 16);
        winConFloor = Random.Range(20, 31);
    }

    void Update()
    {
        elevatorDisplayNumber.text = currentFloor.ToString();

        if (!isRunning && buttonPressed && floorCount.Contains(true))
        {
            StartCoroutine(ElevatorRoutine());
        }
    }

    private IEnumerator ElevatorRoutine()
    {
        isRunning = true;

        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorMoving, this.transform.position);

        #region Stop Floor Setup
        if (!stopFloorSet && !characterRandomizer.freakInElevator)
        {
            stopFloor = Random.Range(currentFloor + 1, winConFloor);
            stopFloorSet = true;
        }
        #endregion

        #region Elevator Floor Timer
        //goes through the floors slowly and gradually, like a real elevator
        float waitTime = Random.Range(4f, 8f);
        Debug.Log("Waiting " + waitTime + " seconds before moving...");
        yield return new WaitForSeconds(waitTime);
        #endregion

        #region Freak is in elevator
        if (characterRandomizer.freakInElevator) //IS freak of nature fucker fuckington in the elevator? Run this shit.
        {
            //STOP FLOOR IS (NOT) USED

            if (priorityRequestActive)
            {
                yield return StartCoroutine(GoToRequestFloor());
            }

            if (priorityClosestActive)
            {
                yield return StartCoroutine(GoToClosestFloor());
            }

            if (priorityWinConActive)
            {
                yield return StartCoroutine(GoToWinConFloor());
            }

            else
            {
                Debug.LogError("There's no more floors to go to! Stopping routine w/ freak inside!");
                isRunning = false;
            }

            priorityRequestActive = false;
            priorityClosestActive = false;
            priorityWinConActive = false;

            Debug.Log("Freak is in the elevator, running specific code for this!");
        }
        #endregion

        #region Freak is NOT in elevator
        if (!characterRandomizer.freakInElevator)
        {
            int spawnFreak = Random.Range(0, 4); //rolls for a chance to spawn a freak every time this routine is ran

            if (spawnFreak == 0 || currentFloor == stopFloor) //if a roll is successful (20%)
            {
                Debug.Log("Freak is being spawned...");
                buttonPressed = false;
                isRunning = false;
                stopFloorSet = false;
                StartCoroutine(characterRandomizer.FreakSpawner());
                yield break;
            }

            //STOP FLOOR IS (INDEED) USED
            if (currentFloor < winConFloor) //always moving towards winConFloor if bigger
            {
                currentFloor++;
                floorCount[currentFloor] = false; //***CHANGE LINE LATER; weird to have floors set to false if they aren't being opened?***
            }

            else if (currentFloor > winConFloor) //always moving towards winConFloor if smaller
            {
                currentFloor--;
                floorCount[currentFloor] = false; //***CHANGE LINE LATER; weird to have floors set to false if they aren't being opened?***
            }

            else if (floorCount[currentFloor])
            {
                floorCount[currentFloor] = false;
            }

            /*if (currentFloor == stopFloor || floorCount[currentFloor]) //stops execution when the default stopFloor is met
            {
                Debug.Log("Stopped at random floor before win!"); //***INSERT EVENT HERE (GENERATE FREAK)***
                stopFloorSet = false; //Stop Floor wont EVER change unless the execution of the coroutine here STOPS
            }*/

            Debug.Log("Current floor: " + currentFloor);
            Debug.Log("No freak in the elevator, acting accordingly!");
        }
        #endregion

        isRunning = false; //can re-trigger again next frame if another switch is active
        //StartCoroutine(ElevatorOpening());
    }

    #region RequestFloor
    private IEnumerator GoToRequestFloor()
    {
        Debug.Log("Going To Request Floor!");

        //***GET REQUEST FLOOR VALUE FROM FREAK HERE ONCE WRITTEN OUT***

        while (currentFloor != characterRandomizer.requestFloor)
        {
            //goes through the floors slowly and gradually, like a real elevator
            float waitTime = Random.Range(4f, 8f);
            Debug.Log("Waiting " + waitTime + " seconds before moving...");
            yield return new WaitForSeconds(waitTime);

            if (currentFloor < characterRandomizer.requestFloor)
            {
                currentFloor++;
            }
            else if (currentFloor > characterRandomizer.requestFloor)
            {
                currentFloor--;
            }

            if (currentFloor == characterRandomizer.requestFloor)
            {
                Debug.Log("We at the freak's floor! Now they're happy :0)!!!"); //***add win state later, probably just call a seperate win function written later***

                yield return new WaitForSeconds(1f);
            }
        }

        floorCount[currentFloor] = false;
        characterRandomizer.freakInElevator = false;
        StartCoroutine(ElevatorOpening());
    }
    #endregion

    #region ClosestFloor
    private IEnumerator GoToClosestFloor()
    {
        Debug.Log("Going To Closest Floor!");

        int closest = GetClosestFloorToCurrentFloor(); //check for what the closest floor to current floor even is

        if (closest == -1)
        {
            Debug.LogError("No closest floor found! Going to WinCon"); //shouldn't really happen; but just in case
            yield return StartCoroutine(GoToWinConFloor());
            yield break;
        }

        while (currentFloor != closest) //loop to go to closest floor relative to current floor
        {
            if (priorityRequestActive)
            {
                isRunning = false;
                yield break;
            }

            //goes through the floors slowly and gradually, like a real elevator
            float waitTime = Random.Range(4f, 8f);
            Debug.Log("Waiting " + waitTime + " seconds before moving...");
            yield return new WaitForSeconds(waitTime);

            if (currentFloor < closest)
            {
                currentFloor++;
            }

            else if (currentFloor > closest)
            {
                currentFloor--;
            }

            yield return new WaitForSeconds(1f);
        }

        if (characterRandomizer.freakInElevator)
        {
            characterRandomizer.freakInElevator = false;
            FreakGivesTheirRemarks();
        }

        floorCount[currentFloor] = false;
        if (floorCount.Contains(true))
        {
            yield return StartCoroutine(GoToClosestFloor());
        }

        Debug.Log("Arrived at closest floor!");
        StartCoroutine(ElevatorOpening());
    }
    #endregion

    #region WinConFloor
    private IEnumerator GoToWinConFloor()
    {
        Debug.Log("Going To Win Con Floor!");

        while (currentFloor != winConFloor)
        {
            //goes through the floors slowly and gradually, like a real elevator
            float waitTime = Random.Range(4f, 8f);
            Debug.Log("Waiting " + waitTime + " seconds before moving...");
            yield return new WaitForSeconds(waitTime);

            if (currentFloor < winConFloor)
            {
                currentFloor++;
            }
            else if (currentFloor > winConFloor)
            {
                currentFloor--;
            }

            if (currentFloor == winConFloor)
            {
                Debug.Log("WINNER!"); //***add win state later, probably just call a seperate win function written later***

                yield return new WaitForSeconds(1f);
            }
        }
        Debug.Log("Returning To Regularly Scheduled Coroutine");
    }
    #endregion

    #region Closest Floor Check
    private int GetClosestFloorToCurrentFloor()
    {
        int maxFloors = floorCount.Count;

        // check increasing distance outward
        for (int distance = 0; distance < maxFloors; distance++)
        {
            int lower = currentFloor - distance;
            int upper = currentFloor + distance;

            // Check lower bound
            if (lower >= 0 && floorCount[lower])
                return lower;

            // Check upper bound
            if (upper < maxFloors && floorCount[upper])
                return upper;
        }

        return -1; // no requested floors found
    }
    #endregion

    public IEnumerator ElevatorOpening()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorStopping, this.transform.position);
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorConfirm, this.transform.position);
        Debug.Log("Elevator Opening!");
        elevatorAnim.Play("ElevatorOpen");
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorOpening, this.transform.position);
        rooms.ActivateRandomRoom();

        float waitTime = Random.Range(3f, 6f);
        Debug.Log("Waiting " + waitTime + " seconds before moving...");
        yield return new WaitForSeconds(waitTime);

        StartCoroutine(ElevatorRoutine());
    }

    public IEnumerator ElevatorClosing()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorConfirm, this.transform.position);
        Debug.Log("Elevator Closing!");
        elevatorAnim.Play("ElevatorClose");
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorClosing, this.transform.position);

        float waitTime = Random.Range(3f, 6f);
        Debug.Log("Waiting " + waitTime + " seconds before moving...");
        yield return new WaitForSeconds(waitTime);

        rooms.ResetRoom();
        StartCoroutine(ElevatorRoutine());
    }

    public IEnumerator FreakGivesTheirRemarks()
    {
        //**ADD STUFF FOR THE FREAK's REMARKS WHEN THE TIME COMES HERE**//

        float waitTime = Random.Range(3f, 6f);
        Debug.Log("Waiting " + waitTime + " seconds before moving...");
        yield return new WaitForSeconds(waitTime);

        StartCoroutine(ElevatorRoutine());
    }

    #region Unused
    /*private void ElevatorStart()
    {
        if (characterRandomizer.freakInElevator) //IS freak in the elevator? Run this.
        {
            Debug.Log("Freak is in the elevator, running specific code for this!");
        }

        if (!characterRandomizer.freakInElevator) //NO freak in the elevator? Run this.
        {
            stopFloor = Random.Range(currentFloor + 1, winConFloor); //sets default floor to stop at before your floor.

            if (currentFloor < winConFloor)
                currentFloor++;

            else if (currentFloor > winConFloor)
                currentFloor--;

            if (currentFloor == stopFloor)
            {
                Debug.Log("Stopped at random floor before win!");
            }

            int targetFloor = currentFloor != stopFloor
            ? stopFloor
            : winConFloor;

            MoveElevatorToward(targetFloor);

            Debug.Log("Current floor: " + currentFloor);
            Debug.Log("No freak in the elevator, acting accordingly!");
        }
    }*/

    /*private void MoveElevatorToward(int target)
    {
        if (currentFloor < target)
            currentFloor++;

        else if (currentFloor > target)
            currentFloor--;

        Debug.Log("Current floor: " + currentFloor);

        // Check if we hit the random stop
        if (currentFloor == stopFloor)
        {
            Debug.Log("Stopped at random floor before win!");
        }
    }*/
    #endregion
}
