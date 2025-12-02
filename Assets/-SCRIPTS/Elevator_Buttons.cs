using UnityEngine;

public class Elevator_Buttons : MonoBehaviour
{
    public Elevator elevator;

    [Header("Floor Button")]
    [SerializeField] private int floorIndex;   // <-- assign this per button in Inspector :0) smile!
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
        // Directly modify the bool list inside Elevator when hand(any rigidbody) touches button
        elevator.floorCount[floorIndex] = true;

        Debug.Log("Pressed button for floor: " + floorIndex);
    }
}
