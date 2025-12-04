using UnityEngine;

public class Elevator_Buttons : MonoBehaviour
{
    public Elevator elevator;
    public CharacterRandomizer characterRandomizer;

    [Header("Floor Button")]
    [SerializeField] public int floorIndex;   // <-- assign this per button in Inspector :0) smile!
    [SerializeField] private GameObject button;

    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)) //***TEST INPUT***
        {
            PressButton();
        }
    }*/

    public void OnTriggerEnter(Collider other)
    {
        if (!elevator.canPress)
        {
            return;
        }

        // Directly modify the bool list inside Elevator when hand(any rigidbody) touches button
        elevator.floorCount[floorIndex] = true;
        elevator.buttonPressed = true;

        if (!characterRandomizer.freakInElevator)
        {
            elevator.priorityClosestActive = true;
        }

        if (characterRandomizer.freakInElevator)
        {
            characterRandomizer.DetermineNextLocation(floorIndex);

            Debug.Log("Determining next location...");
        }
    }
}
