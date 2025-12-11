using UnityEngine;
using UnityEngine.Serialization;

public class Rooms : MonoBehaviour
{
    [Header("Rooms")]
    [SerializeField] private GameObject[] rooms;

    private GameObject currentActiveRoom;

    // Call this to activate a random room
    public void ActivateRandomRoom()
    {
        // Deactivate previous one if exists
        if (currentActiveRoom != null)
            currentActiveRoom.SetActive(false);

        // Pick random
        int index = Random.Range(0, rooms.Length);
        currentActiveRoom = rooms[index];

        // Activate it
        currentActiveRoom.SetActive(true);

        Debug.Log("Activated: " + currentActiveRoom.name);
    }

    // Call this to reset/deactivate the current one
    public void ResetRoom()
    {
        if (currentActiveRoom != null)
        {
            currentActiveRoom.SetActive(false);
            currentActiveRoom = null;
        }
    }
}
