using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using FMODUnity;
using FMOD.Studio;

public class FMODEvents : MonoBehaviour
{
    [field: Header("Music")]
    [field: SerializeField] public EventReference music { get; private set; }

    [field: Header("SFX")]
    [field: SerializeField] public EventReference buttonPressed { get; private set; }
    [field: SerializeField] public EventReference elevatorOpening { get; private set; }
    [field: SerializeField] public EventReference elevatorClosing { get; private set; }
    [field: SerializeField] public EventReference elevatorMoving { get; private set; }
    [field: SerializeField] public EventReference elevatorStopping { get; private set; }
    [field: SerializeField] public EventReference elevatorConfirm { get; private set; }

    public static FMODEvents instance { get; private set; }

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one FMOD Event instance in the scene!");
        }
        instance = this;
    }
}
