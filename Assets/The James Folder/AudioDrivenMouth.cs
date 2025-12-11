using UnityEngine;
using UnityEngine.Playables; // For PlayableDirector / Timeline

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Drives a 0–1 "mouth open" (or any 0–1 control) from an AudioSource's loudness and
/// outputs it to ONE of the following (selected via enum):
/// - Animator float parameter
/// - SkinnedMeshRenderer blendshape
/// - Timeline (PlayableDirector) time, e.g. 0–1 seconds
///
/// Runs in LateUpdate with a high DefaultExecutionOrder so it can override
/// earlier animation / Timeline changes.
/// </summary>
[DefaultExecutionOrder(1000)]
public class AudioDrivenMouth : MonoBehaviour
{
    public enum OutputMode
    {
        Animator,
        Blendshape,
        Timeline
    }

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

    [Header("Output Mode")]
    [Tooltip("Where to send the 0–1 output value.")]
    public OutputMode outputMode = OutputMode.Animator;

    [Header("Animator Output")]
    [Tooltip("Animator that controls your character.")]
    public Animator animator;

    [Tooltip("Float parameter on the Animator that represents mouth open (0–1).")]
    public string animatorParameter = "MouthOpen";

    [Header("Blendshape Output")]
    [Tooltip("SkinnedMeshRenderer with your mouth/jaw blendshape.")]
    public SkinnedMeshRenderer skinnedMeshRenderer;

    [Tooltip("Blendshape index that represents mouth open (0–100%).")]
    public int blendshapeIndex = 0;

    [Header("Timeline Output")]
    [Tooltip("PlayableDirector whose time we control.")]
    public PlayableDirector playableDirector;

    [Tooltip("Local time (seconds) mapped from 0 when mouth is closed.")]
    public float timelineStartSeconds = 0f;

    [Tooltip("Local time (seconds) mapped from 1 when mouth is fully open.")]
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

        if (outputMode == OutputMode.Timeline &&
            playableDirector != null &&
            pauseDirectorOnAwake)
        {
            // Start the director paused so we fully control time via this script.
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

    // Run late in the frame so we can override Timeline / Animator changes.
    private void LateUpdate()
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

        // Get audio data directly from the AudioSource (channel 0).
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

        // Push to whichever output we picked.
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
    /// Sends the mouthOpen value to the selected output.
    /// </summary>
    private void ApplyOutputs()
    {
        switch (outputMode)
        {
            case OutputMode.Animator:
                if (animator != null && _hasAnimParam)
                {
                    animator.SetFloat(_animParamHash, mouthOpen);
                }
                break;

            case OutputMode.Blendshape:
                if (skinnedMeshRenderer != null &&
                    skinnedMeshRenderer.sharedMesh != null &&
                    blendshapeIndex >= 0 &&
                    blendshapeIndex < skinnedMeshRenderer.sharedMesh.blendShapeCount)
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(blendshapeIndex, mouthOpen * 100f);
                }
                break;

            case OutputMode.Timeline:
                if (playableDirector != null)
                {
                    float tNorm = Mathf.Clamp01(mouthOpen);
                    float targetTime = Mathf.Lerp(timelineStartSeconds, timelineEndSeconds, tNorm);

                    // If the director is currently playing normally, pause it so we fully control time.
                    if (playableDirector.state == PlayState.Playing)
                        playableDirector.Pause();

                    playableDirector.time = targetTime;
                    playableDirector.Evaluate(); // Apply this frame's Timeline state
                }
                break;
        }
    }

    /// <summary>
    /// Public getter so other scripts / timelines can read the current mouth value.
    /// </summary>
    public float MouthOpenValue => mouthOpen;
}

#if UNITY_EDITOR
[CustomEditor(typeof(AudioDrivenMouth))]
public class AudioDrivenMouthEditor : Editor
{
    // Serialized properties
    private SerializedProperty audioSourceProp;
    private SerializedProperty sampleWindowProp;
    private SerializedProperty loudnessFloorProp;
    private SerializedProperty loudnessCeilingProp;
    private SerializedProperty responseExponentProp;
    private SerializedProperty smoothingTimeProp;
    private SerializedProperty attackMultiplierProp;
    private SerializedProperty releaseMultiplierProp;

    private SerializedProperty outputModeProp;

    private SerializedProperty animatorProp;
    private SerializedProperty animatorParameterProp;

    private SerializedProperty skinnedMeshRendererProp;
    private SerializedProperty blendshapeIndexProp;

    private SerializedProperty playableDirectorProp;
    private SerializedProperty timelineStartSecondsProp;
    private SerializedProperty timelineEndSecondsProp;
    private SerializedProperty pauseDirectorOnAwakeProp;

    private SerializedProperty currentRmsProp;
    private SerializedProperty mouthOpenProp;

    private void OnEnable()
    {
        audioSourceProp           = serializedObject.FindProperty("audioSource");
        sampleWindowProp          = serializedObject.FindProperty("sampleWindow");
        loudnessFloorProp         = serializedObject.FindProperty("loudnessFloor");
        loudnessCeilingProp       = serializedObject.FindProperty("loudnessCeiling");
        responseExponentProp      = serializedObject.FindProperty("responseExponent");
        smoothingTimeProp         = serializedObject.FindProperty("smoothingTime");
        attackMultiplierProp      = serializedObject.FindProperty("attackMultiplier");
        releaseMultiplierProp     = serializedObject.FindProperty("releaseMultiplier");

        outputModeProp            = serializedObject.FindProperty("outputMode");

        animatorProp              = serializedObject.FindProperty("animator");
        animatorParameterProp     = serializedObject.FindProperty("animatorParameter");

        skinnedMeshRendererProp   = serializedObject.FindProperty("skinnedMeshRenderer");
        blendshapeIndexProp       = serializedObject.FindProperty("blendshapeIndex");

        playableDirectorProp      = serializedObject.FindProperty("playableDirector");
        timelineStartSecondsProp  = serializedObject.FindProperty("timelineStartSeconds");
        timelineEndSecondsProp    = serializedObject.FindProperty("timelineEndSeconds");
        pauseDirectorOnAwakeProp  = serializedObject.FindProperty("pauseDirectorOnAwake");

        currentRmsProp            = serializedObject.FindProperty("currentRms");
        mouthOpenProp             = serializedObject.FindProperty("mouthOpen");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Audio Input", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(audioSourceProp);
        EditorGUILayout.PropertyField(sampleWindowProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loudness → 0–1 Mapping", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(loudnessFloorProp);
        EditorGUILayout.PropertyField(loudnessCeilingProp);
        EditorGUILayout.PropertyField(responseExponentProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Smoothing", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(smoothingTimeProp);
        EditorGUILayout.PropertyField(attackMultiplierProp);
        EditorGUILayout.PropertyField(releaseMultiplierProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output Mode", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(outputModeProp);

        var mode = (AudioDrivenMouth.OutputMode)outputModeProp.enumValueIndex;

        EditorGUILayout.Space();
        switch (mode)
        {
            case AudioDrivenMouth.OutputMode.Animator:
                EditorGUILayout.LabelField("Animator Output", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(animatorProp);
                EditorGUILayout.PropertyField(animatorParameterProp);
                break;

            case AudioDrivenMouth.OutputMode.Blendshape:
                EditorGUILayout.LabelField("Blendshape Output", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(skinnedMeshRendererProp);
                EditorGUILayout.PropertyField(blendshapeIndexProp);
                break;

            case AudioDrivenMouth.OutputMode.Timeline:
                EditorGUILayout.LabelField("Timeline Output", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(playableDirectorProp);
                EditorGUILayout.PropertyField(timelineStartSecondsProp);
                EditorGUILayout.PropertyField(timelineEndSecondsProp);
                EditorGUILayout.PropertyField(pauseDirectorOnAwakeProp);
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug (read-only)", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(currentRmsProp);
        EditorGUILayout.PropertyField(mouthOpenProp);
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
