using UnityEngine;
using UnityEngine.Playables; // For PlayableDirector / Timeline

/// <summary>
/// Drives a 0–1 "mouth open" value from an AudioSource's loudness and
/// optionally pushes it into:
/// - An Animator float parameter
/// - A SkinnedMeshRenderer blendshape
/// - A Timeline (PlayableDirector) time, e.g. 0–1 seconds
/// 
/// Typical use:
/// - Make a 0–1 second Timeline clip that animates your mouth (0 = closed, 1 = open).
/// - This script scrubs that Timeline based on live audio loudness.
/// </summary>
[DefaultExecutionOrder(-10)]
public class AudioDrivenMouth : MonoBehaviour
{
    [Header("Audio Input")]
    [Tooltip("AudioSource that is playing the dialogue / voice.")]
    public AudioSource audioSource;

    [Tooltip("Number of audio samples to analyze each frame. 512–2048 is typical.")]
    [Range(64, 4096)]
    public int sampleWindow = 1024;

    [Header("Loudness → 0–1 Mapping")]
    [Tooltip("Loudness below this is treated as 0 (mouth closed).")]
    public float loudnessFloor = 0.002f;

    [Tooltip("Loudness at or above this maps to 1 (mouth fully open).")]
    public float loudnessCeiling = 0.05f;

    [Tooltip("Apply a curve to perception. >1 makes response softer at low volumes, <1 makes it more punchy.")]
    public float responseExponent = 1.5f;

    [Header("Smoothing")]
    [Tooltip("Time (seconds) to smooth toward new values. Smaller = snappier, larger = floatier.")]
    [Min(0f)]
    public float smoothingTime = 0.06f;

    [Tooltip("Extra multiplier when the mouth is opening. >1 = faster openings.")]
    public float attackMultiplier = 2f;

    [Tooltip("Extra multiplier when the mouth is closing. < attackMultiplier for softer closing.")]
    public float releaseMultiplier = 1f;

    [Header("Animator Output (Optional)")]
    [Tooltip("If true, push the mouth value into an Animator float parameter.")]
    public bool driveAnimator = true;

    [Tooltip("Animator that controls your character.")]
    public Animator animator;

    [Tooltip("Float parameter on the Animator that represents mouth open (0–1).")]
    public string animatorParameter = "MouthOpen";

    [Header("Blendshape Output (Optional)")]
    [Tooltip("If true, drive a blendshape directly on a SkinnedMeshRenderer.")]
    public bool driveBlendshape = false;

    [Tooltip("SkinnedMeshRenderer with your mouth/jaw blendshape.")]
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [Tooltip("Blendshape index that represents mouth open (0–100%).")]
    public int blendshapeIndex = 0;

    [Header("Timeline Output (Optional)")]
    [Tooltip("If true, scrub a Timeline using the mouth open value (0–1).")]
    public bool driveTimeline = false;

    [Tooltip("PlayableDirector whose time we control.")]
    public PlayableDirector playableDirector;

    [Tooltip("Local time range (seconds) within the Timeline to map 0–1 into. For a 0–1 second clip, use 0 and 1.")]
    public float timelineStartSeconds = 0f;

    [Tooltip("Local time range (seconds) within the Timeline to map 0–1 into.")]
    public float timelineEndSeconds = 1f;

    [Tooltip("Pause the director on Awake so we can manually scrub it.")]
    public bool pauseDirectorOnAwake = true;

    [Header("Debug")]
    [Tooltip("Current raw RMS loudness (before mapping).")]
    public float currentRms;

    [Tooltip("Current normalized mouth open value (0–1) after smoothing.")]
    [Range(0f, 1f)]
    public float mouthOpen;

    private float[] _samples;
    private int _animParamHash;
    private bool _hasAnimParam;

    private void Awake()
    {
        if (sampleWindow <= 0)
            sampleWindow = 1024;

        _samples = new float[sampleWindow];

        if (animator != null && !string.IsNullOrEmpty(animatorParameter))
        {
            _animParamHash = Animator.StringToHash(animatorParameter);
            _hasAnimParam = true;
        }

        if (driveTimeline && playableDirector != null && pauseDirectorOnAwake)
        {
            // Start the director paused so we fully control time via this script
            playableDirector.time = timelineStartSeconds;
            playableDirector.Evaluate();
            playableDirector.Pause();
        }
    }

    private void OnValidate()
    {
        if (sampleWindow < 64) sampleWindow = 64;
        if (loudnessCeiling < loudnessFloor)
            loudnessCeiling = loudnessFloor + 0.0001f;

        if (responseExponent <= 0f)
            responseExponent = 1f;

        if (attackMultiplier <= 0f)
            attackMultiplier = 1f;

        if (releaseMultiplier <= 0f)
            releaseMultiplier = 1f;

        if (Mathf.Approximately(timelineStartSeconds, timelineEndSeconds))
            timelineEndSeconds = timelineStartSeconds + 0.001f;
    }

    private void Update()
    {
        if (audioSource == null || !audioSource.isPlaying)
        {
            // Gently close the mouth if nothing is playing.
            SmoothMouthTowards(0f);
            ApplyOutputs();
            return;
        }

        // Make sure our buffer size matches current window.
        if (_samples == null || _samples.Length != sampleWindow)
            _samples = new float[sampleWindow];

        // Get audio data directly from the AudioSource.
        // Channel index 0 is fine for mono or left channel of stereo.
        audioSource.GetOutputData(_samples, 0);

        // Compute RMS loudness.
        float sum = 0f;
        for (int i = 0; i < sampleWindow; i++)
        {
            float s = _samples[i];
            sum += s * s;
        }

        currentRms = Mathf.Sqrt(sum / sampleWindow);

        // Map RMS to 0–1 using floor/ceiling.
        float normalized = Mathf.InverseLerp(loudnessFloor, loudnessCeiling, currentRms);
        normalized = Mathf.Clamp01(normalized);

        // Apply perceptual curve.
        if (!Mathf.Approximately(responseExponent, 1f))
        {
            normalized = Mathf.Pow(normalized, responseExponent);
        }

        // Smooth toward the target so it doesn't jitter.
        SmoothMouthTowards(normalized);

        // Push to Animator / Blendshape / Timeline.
        ApplyOutputs();
    }

    /// <summary>
    /// Smooths mouthOpen toward target using an exponential interpolation
    /// with separate attack/release speeds.
    /// </summary>
    private void SmoothMouthTowards(float target)
    {
        float delta = target - mouthOpen;

        if (smoothingTime <= 0f)
        {
            mouthOpen = target;
            return;
        }

        // Base smoothing factor per frame.
        float speed = 1f / smoothingTime;

        // Attack vs release behavior.
        if (delta > 0f)
        {
            speed *= attackMultiplier;
        }
        else
        {
            speed *= releaseMultiplier;
        }

        // Exponential smoothing toward target.
        float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
        mouthOpen += delta * t;
        mouthOpen = Mathf.Clamp01(mouthOpen);
    }

    /// <summary>
    /// Sends the mouthOpen value to any configured outputs.
    /// </summary>
    private void ApplyOutputs()
    {
        // Animator float
        if (driveAnimator && animator != null && _hasAnimParam)
        {
            animator.SetFloat(_animParamHash, mouthOpen);
        }

        // Blendshape
        if (driveBlendshape && skinnedMeshRenderer != null &&
            blendshapeIndex >= 0 && skinnedMeshRenderer.sharedMesh != null &&
            blendshapeIndex < skinnedMeshRenderer.sharedMesh.blendShapeCount)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(blendshapeIndex, mouthOpen * 100f);
        }

        // Timeline (scrub between timelineStartSeconds and timelineEndSeconds)
        if (driveTimeline && playableDirector != null)
        {
            float t = Mathf.Clamp01(mouthOpen);
            float targetTime = Mathf.Lerp(timelineStartSeconds, timelineEndSeconds, t);

            // If the director is currently playing normally, pause it so we fully control time.
            if (playableDirector.state == PlayState.Playing)
                playableDirector.Pause();

            playableDirector.time = targetTime;
            playableDirector.Evaluate(); // Force the Timeline to update to this exact time
        }
    }

    /// <summary>
    /// Public getter so other scripts / timelines can read the current mouth value.
    /// </summary>
    public float MouthOpenValue => mouthOpen;
}
