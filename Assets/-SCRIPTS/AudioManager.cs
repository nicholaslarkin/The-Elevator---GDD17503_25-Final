using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private EventInstance musicEventInstance;

    public static AudioManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeMusic(FMODEvents.instance.music);
    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.R)) // TEST
        {
            ResetAudio();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }*/
    }

    // Create any FMOD instance safely
    public EventInstance CreateInstance(EventReference eventReference)
    {
        return RuntimeManager.CreateInstance(eventReference);
    }

    // Create + Start the music event
    public void InitializeMusic(EventReference musicEventReference)
    {
        musicEventInstance = CreateInstance(musicEventReference);
        musicEventInstance.start();
    }

    // One-shot SFX
    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    // Reset music properly
    public void ResetAudio()
    {
        musicEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicEventInstance.release();

        // Recreate the music using FMODEvents reference — NEVER raw strings
        InitializeMusic(FMODEvents.instance.music);
    }
}
