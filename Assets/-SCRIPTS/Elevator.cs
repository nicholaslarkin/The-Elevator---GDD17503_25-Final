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
    public enum ElevatorState
    {
        Idle,
        WaitingForInput,
        Moving_NoFreak,
        Moving_RequestFloor,
        Moving_ClosestFloor,
        Moving_WinCon,
        DoorsOpening,
        DoorsClosing,
        SpawningFreak,
        FreakRemarks
    }

    [Header("Debug State (Read Only)")]
    [SerializeField] private ElevatorState currentState = ElevatorState.Idle;
    [SerializeField, TextArea] private string stateDescription = "";

    public CharacterRandomizer characterRandomizer;
    public Rooms rooms;
    public Animator elevatorAnim;
    public TextMeshProUGUI elevatorDisplayNumber;

    [Header("Coroutine")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] public bool canPress = true;
    [SerializeField] public bool buttonPressed = false;
    [SerializeField] private bool stopFloorSet = false; // currently not used for logic, kept for future behavior

    [Header("Priority List")]
    [SerializeField] public bool priorityRequestActive;
    [SerializeField] public bool priorityClosestActive;
    [SerializeField] public bool priorityWinConActive;

    #region Priority List
    // Requested Floor (highest priority)
    // Closest Floor (second priority)
    // Win Con Floor (third priority)
    #endregion

    [Header("Floors")]
    [SerializeField] public List<bool> floorCount = new List<bool>(new bool[30]);
    [SerializeField] public int currentFloor;
    [SerializeField] public int stopFloor;
    [SerializeField] public int winConFloor;

    void Start()
    {
        currentFloor = Random.Range(0, 16);
        // floorCount has indices 0–29, so winConFloor should be 20–29
        winConFloor = Random.Range(20, 30);

        SetState(ElevatorState.WaitingForInput, "Elevator initialized. Waiting for button press.");
    }

    void Update()
    {
        elevatorDisplayNumber.text = currentFloor.ToString();

        if (!isRunning && buttonPressed && floorCount.Contains(true))
        {
            StartCoroutine(ElevatorRoutine());
        }
        else if (!isRunning && !buttonPressed)
        {
            SetState(ElevatorState.WaitingForInput, "Idle. Waiting for button press.");
        }
    }

    private void SetState(ElevatorState newState, string description = "")
    {
        currentState = newState;
        stateDescription = description;
    }

    private IEnumerator ElevatorRoutine()
    {
        isRunning = true;

        // Safety: if there's nothing to do and no freak inside, just bail
        if (!characterRandomizer.freakInElevator && !floorCount.Contains(true))
        {
            SetState(ElevatorState.Idle, "No pending floors and no freak. Going idle.");
            isRunning = false;
            buttonPressed = false;
            yield break;
        }

        // goes through the floors slowly and gradually, like a real elevator
        float waitTime = Random.Range(4f, 8f);
        Debug.Log("Waiting " + waitTime + " seconds before moving...");
        yield return new WaitForSeconds(waitTime);

        #region Freak is in elevator
        if (characterRandomizer.freakInElevator) // freak is in the elevator: use priorities ONLY
        {
            // PRIORITY ORDER: Request > Closest > WinCon
            if (priorityRequestActive)
            {
                SetState(ElevatorState.Moving_RequestFloor, $"Moving to request floor {characterRandomizer.requestFloor}.");
                yield return StartCoroutine(GoToRequestFloor());
            }
            else if (priorityClosestActive)
            {
                SetState(ElevatorState.Moving_ClosestFloor, "Executing priority: Closest floor.");
                yield return StartCoroutine(GoToClosestFloor());
            }
            else if (priorityWinConActive)
            {
                SetState(ElevatorState.Moving_WinCon, $"Moving to WinCon floor {winConFloor}.");
                yield return StartCoroutine(GoToWinConFloor());
            }
            else
            {
                Debug.LogWarning("Freak is in the elevator, but no priority flag is set. Doing nothing this cycle.");
                SetState(ElevatorState.Idle, "Freak inside but no priority set. Standing by.");
            }

            // Reset priorities after we've handled the freak case
            priorityRequestActive = false;
            priorityClosestActive = false;
            priorityWinConActive = false;

            Debug.Log("Freak was in the elevator, priority logic completed.");
            isRunning = false;
            // IMPORTANT: don't fall through into the "no freak" logic in the same routine
            yield break;
        }
        #endregion

        #region Freak is NOT in elevator
        else // no freak in elevator
        {
            SetState(ElevatorState.Moving_NoFreak, "No freak. Moving based on selected floors.");

            // First: roll for freak spawn chance at the current floor
            int spawnFreak = Random.Range(0, 4); // 25% chance

            if (spawnFreak == 0)
            {
                Debug.Log("Freak is being spawned...");
                SetState(ElevatorState.SpawningFreak, $"Spawning freak at floor {currentFloor}.");
                buttonPressed = false;
                isRunning = false;
                stopFloorSet = false;
                StartCoroutine(characterRandomizer.FreakSpawner());
                yield break;
            }

            // If there are floors selected, go to the closest one
            if (floorCount.Contains(true))
            {
                SetState(ElevatorState.Moving_ClosestFloor, "No freak inside. Going to closest selected floor.");
                yield return StartCoroutine(GoToClosestFloor());
                isRunning = false;
                yield break;
            }

            // If somehow we got here with no floors, just idle
            SetState(ElevatorState.Idle, "No floors selected and no freak. Going idle.");
        }
        #endregion

        isRunning = false; //can re-trigger again next frame if another switch is active
        SetState(ElevatorState.WaitingForInput, "Finished automatic move. Waiting for next input.");
    }

    #region RequestFloor
    private IEnumerator GoToRequestFloor()
    {
        Debug.Log("Going To Request Floor!");
        SetState(ElevatorState.Moving_RequestFloor, $"Heading toward request floor {characterRandomizer.requestFloor}.");

        while (currentFloor != characterRandomizer.requestFloor)
        {
            //goes through the floors slowly and gradually, like a real elevator
            float waitTime = Random.Range(4f, 8f);
            Debug.Log("Waiting " + waitTime + " seconds before moving...");
            SetState(ElevatorState.Moving_RequestFloor, $"Moving toward request floor {characterRandomizer.requestFloor} (currently on {currentFloor}).");
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
                Debug.Log("We at the freak's floor! Now they're happy :0)!!!");
                SetState(ElevatorState.Moving_RequestFloor, $"Arrived at request floor {currentFloor}.");
                yield return new WaitForSeconds(1f);
            }
        }

        floorCount[currentFloor] = false;
        characterRandomizer.freakInElevator = false;
        yield return StartCoroutine(ElevatorOpening());
    }
    #endregion

    #region ClosestFloor
    private IEnumerator GoToClosestFloor()
    {
        Debug.Log("Going To Closest Floor!");

        int closest = GetClosestFloorToCurrentFloor(); //check for what the closest floor to current floor even is

        if (closest == -1)
        {
            Debug.LogError("No closest floor found! Nothing left to visit.");
            SetState(ElevatorState.Idle, "No closest floor found. Nothing left to visit.");
            yield break;
        }

        SetState(ElevatorState.Moving_ClosestFloor, $"Beginning closest-floor logic. Target: floor {closest}.");

        while (currentFloor != closest) //loop to go to closest floor relative to current floor
        {
            // If during travel, a request floor becomes active, let the main routine handle that on next cycle
            if (priorityRequestActive)
            {
                Debug.Log("Priority request became active mid-travel; aborting closest-floor run.");
                SetState(ElevatorState.Moving_RequestFloor, "Request priority activated mid-travel. Aborting closest floor movement.");
                isRunning = false;
                yield break;
            }

            //goes through the floors slowly and gradually, like a real elevator
            float waitTime = Random.Range(4f, 8f);
            Debug.Log("Waiting " + waitTime + " seconds before moving...");
            SetState(ElevatorState.Moving_ClosestFloor, $"Moving toward closest floor {closest}. Currently at {currentFloor}.");
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

        // We've arrived at the closest floor
        if (characterRandomizer.freakInElevator)
        {
            characterRandomizer.freakInElevator = false;
            SetState(ElevatorState.FreakRemarks, $"Arrived at closest floor {currentFloor}. Freak giving remarks.");
            yield return StartCoroutine(FreakGivesTheirRemarks());
        }

        floorCount[currentFloor] = false;

        Debug.Log("Arrived at closest floor!");
        SetState(ElevatorState.DoorsOpening, $"Arrived at closest floor {currentFloor}. Opening doors.");
        // Open the elevator; ElevatorRoutine will only restart if appropriate conditions are met
        yield return StartCoroutine(ElevatorOpening());
    }
    #endregion

    #region WinConFloor
    private IEnumerator GoToWinConFloor()
    {
        Debug.Log("Going To Win Con Floor!");
        SetState(ElevatorState.Moving_WinCon, $"Heading toward WinCon floor {winConFloor}.");

        while (currentFloor != winConFloor)
        {
            //goes through the floors slowly and gradually, like a real elevator
            float waitTime = Random.Range(4f, 8f);
            Debug.Log("Waiting " + waitTime + " seconds before moving...");
            SetState(ElevatorState.Moving_WinCon, $"Moving toward WinCon floor {winConFloor}. Currently at {currentFloor}.");
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
                Debug.Log("WINNER!");
                SetState(ElevatorState.Moving_WinCon, $"Arrived at WinCon floor {winConFloor}. WINNER!");
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
        SetState(ElevatorState.DoorsOpening, $"Opening doors at floor {currentFloor}.");
        elevatorAnim.Play("ElevatorOpen");
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorOpening, this.transform.position);
        rooms.ActivateRandomRoom();

        float waitTime = Random.Range(3f, 6f);
        Debug.Log("Waiting " + waitTime + " seconds while doors are open...");
        yield return new WaitForSeconds(waitTime);

        isRunning = false;
        SetState(ElevatorState.WaitingForInput, "Doors open cycle finished. Waiting for new input.");
    }

    public IEnumerator ElevatorClosing()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorConfirm, this.transform.position);
        Debug.Log("Elevator Closing!");
        SetState(ElevatorState.DoorsClosing, "Closing elevator doors.");
        elevatorAnim.Play("ElevatorClose");
        AudioManager.instance.PlayOneShot(FMODEvents.instance.elevatorClosing, this.transform.position);

        float waitTime = Random.Range(3f, 6f);
        Debug.Log("Waiting " + waitTime + " seconds while doors are closed...");
        yield return new WaitForSeconds(waitTime);

        rooms.ResetRoom();
        isRunning = false;

        // IMPORTANT: after a freak enters and doors close, we want to resume ElevatorRoutine
        // if there are floors to go to.
        if (floorCount.Contains(true) && !isRunning)
        {
            buttonPressed = true; // ensure Update logic also agrees we're "in motion mode"
            StartCoroutine(ElevatorRoutine());
        }
        else
        {
            SetState(ElevatorState.WaitingForInput, "Doors closed. Waiting for next button press.");
        }
    }

    public IEnumerator FreakGivesTheirRemarks()
    {
        SetState(ElevatorState.FreakRemarks, "Freak is giving their remarks.");

        float waitTime = Random.Range(3f, 6f);
        Debug.Log("Freak is giving remarks. Waiting " + waitTime + " seconds...");
        yield return new WaitForSeconds(waitTime);

        isRunning = false;
        SetState(ElevatorState.WaitingForInput, "Freak finished remarks. Waiting for next input.");
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
