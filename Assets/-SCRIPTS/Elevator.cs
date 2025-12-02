using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Elevator : MonoBehaviour
{
    public CharacterRandomizer characterRandomizer;

    [Header("Coroutine")]
    [SerializeField] private bool isRunning = false;

    [Header("Floors")]
    [SerializeField] private List<bool> floorCount = new List<bool>(new bool[30]);
    [SerializeField] private int currentFloor;
    [SerializeField] private int stopFloor;
    [SerializeField] private int winConFloor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentFloor = Random.Range(0, 16);
        winConFloor = Random.Range(20,31);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isRunning && floorCount.Contains(true)) // or your trigger condition
        {
            StartCoroutine(ElevatorRoutine());
        }
    }

    private IEnumerator ElevatorRoutine()
    {
        isRunning = true;

        stopFloor = Random.Range(currentFloor + 1, winConFloor);

        float waitTime = Random.Range(4f, 8f);
        Debug.Log("Waiting " + waitTime + " seconds before moving...");
        yield return new WaitForSeconds(waitTime);

        if (characterRandomizer.freakInElevator) //IS freak in the elevator? Run this.
        {
            Debug.Log("Freak is in the elevator, running specific code for this!");
        }

        if (!characterRandomizer.freakInElevator)
        {
            if (currentFloor < winConFloor) //always moving towards winConFloor if bigger
            {
                currentFloor++;
            }
                
            else if (currentFloor > winConFloor) // always moving towards winConFloor if smaller
            {
                currentFloor--;
            }

            if (currentFloor == stopFloor || floorCount[currentFloor]) //stops execution when the default stopFloor is met
            {
                Debug.Log("Stopped at random floor before win!");
            }

            Debug.Log("Current floor: " + currentFloor);
            Debug.Log("No freak in the elevator, acting accordingly!");
        }

        // You can re-trigger again next frame if another switch is active
        isRunning = false;
    }

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
}
