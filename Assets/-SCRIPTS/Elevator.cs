using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Elevator : MonoBehaviour
{
    public CharacterRandomizer characterRandomizer;

    [Header("Coroutine")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] private bool stopFloorSet = false; //bool setup to prevent stopFloor from changing every coroutine run

    [Header("Priority List")]
    [SerializeField] private bool priorityRequestActive;
    [SerializeField] private bool priorityClosestActive;
    [SerializeField] private bool priorityWinConActive;

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
        if (!isRunning && floorCount.Contains(true))
        {
            StartCoroutine(ElevatorRoutine());
        }
    }

    private IEnumerator ElevatorRoutine()
    {
        isRunning = true;

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
                Debug.LogError("None of the priorities were true; failsafing to WinConFloor");
                yield return StartCoroutine(GoToWinConFloor()); //failsafe in case none of the values become true
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

            if (currentFloor == stopFloor || floorCount[currentFloor]) //stops execution when the default stopFloor is met
            {
                Debug.Log("Stopped at random floor before win!"); //***INSERT EVENT HERE (GENERATE FREAK)***
                stopFloorSet = false; //Stop Floor wont EVER change unless the execution of the coroutine here STOPS
            }

            Debug.Log("Current floor: " + currentFloor);
            Debug.Log("No freak in the elevator, acting accordingly!");
        }
        #endregion

        isRunning = false; //can re-trigger again next frame if another switch is active
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
        Debug.Log("Returning To Regularly Scheduled Coroutine");
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

        floorCount[currentFloor] = false;
        Debug.Log("Arrived at closest floor!");
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
