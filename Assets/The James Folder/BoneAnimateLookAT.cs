using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Smoothly rotates a chain of bones toward a target.
/// Includes a button to capture the actual forward axis of the rig.
/// </summary>
public class MultiBoneLookAtController : MonoBehaviour
{
    [Header("Bones")]
    [Tooltip("Ordered from root (neck base) to tip (head).")]
    public Transform[] bones;

    [Header("Look Target")]
    public Transform target;
    public Animator animator;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float globalWeight = 1.0f;

    [Range(0f, 180f)]
    public float maxAngle = 60f;

    [Tooltip("How quickly bones move toward the target.")]
    public float rotationLerpSpeed = 10f;

    [Header("Bone Weights")]
    [Tooltip("Optional per-bone weight override. If empty, weights are evenly distributed.")]
    public float[] perBoneWeights;

    [Header("Forward Axis Correction (auto-generated)")]
    [SerializeField]
    [Tooltip("Rotation applied to ensure the rig's captured forward direction is true forward.")]
    private Quaternion forwardCorrection = Quaternion.identity;

    public bool applyInLateUpdate = true;

    // Expose forwardCorrection safely (read-only)
    public Quaternion ForwardCorrection => forwardCorrection;

    private Quaternion[] animatedLocalRotations;

    private void Reset()
    {
        if (bones == null || bones.Length == 0)
            bones = new Transform[] { transform };

        if (target == null && Camera.main != null)
            target = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying && applyInLateUpdate)
            ApplyLookAt();
    }

    /// <summary>
    /// Captures the head bone's CURRENT forward direction
    /// and stores it as the "true forward" axis.
    /// </summary>
    public void CaptureForwardAxis()
    {
        if (bones == null || bones.Length == 0)
        {
            Debug.LogWarning("No bones assigned.");
            return;
        }

        Transform head = bones[bones.Length - 1];

        // The head's current world-space forward
        Vector3 currentForward = head.forward;

        // Compute correction: maps Vector3.forward → currentForward
        forwardCorrection = Quaternion.FromToRotation(Vector3.forward, currentForward);

#if UNITY_EDITOR
        // Ensures the value is serialized immediately
        EditorUtility.SetDirty(this);
#endif

        Debug.Log("Forward axis captured successfully.");
    }

    public void ApplyLookAt()
    {
        if (bones == null || bones.Length == 0 || target == null || globalWeight <= 0f)
            return;

        // Cache animated local rotations
        animatedLocalRotations = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            animatedLocalRotations[i] = bones[i].localRotation;

        // Per-bone weights
        float[] w = new float[bones.Length];
        if (perBoneWeights != null && perBoneWeights.Length == bones.Length)
            perBoneWeights.CopyTo(w, 0);
        else
        {
            for (int i = 0; i < bones.Length; i++)
                w[i] = (i + 1f) / bones.Length; // head gets more weight
        }

        float maxW = Mathf.Max(w);
        if (maxW > 0)
            for (int i = 0; i < w.Length; i++)
                w[i] /= maxW;

        // For each bone…
        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = bones[i];
            Quaternion animatedLocal = animatedLocalRotations[i];

            Vector3 toTarget = target.position - bone.position;
            if (toTarget.sqrMagnitude < 0.0001f)
                continue;

            toTarget.Normalize();

            // LookRotation + forwardCorrection
            Quaternion lookRotWorld =
                Quaternion.LookRotation(toTarget, bone.up) * ForwardCorrection;

            // Convert to local space
            Quaternion desiredLocal =
                Quaternion.Inverse(bone.parent.rotation) * lookRotWorld;

            // Clamp rotation angle
            float angle = Quaternion.Angle(animatedLocal, desiredLocal);
            if (angle > maxAngle)
            {
                float t = maxAngle / Mathf.Max(angle, 0.0001f);
                desiredLocal = Quaternion.Slerp(animatedLocal, desiredLocal, t);
            }

            // Weight
            Quaternion targetLocal =
                Quaternion.Slerp(animatedLocal, desiredLocal, w[i] * globalWeight);

            // Smooth
            bone.localRotation = Quaternion.Slerp(
                bone.localRotation,
                targetLocal,
                1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime)
            );
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom inspector with a button to capture the forward axis.
/// </summary>
[CustomEditor(typeof(MultiBoneLookAtController))]
public class MultiBoneLookAtControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MultiBoneLookAtController script = (MultiBoneLookAtController)target;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Capture Forward Axis (Use Current Pose)"))
        {
            script.CaptureForwardAxis();
        }
    }
}
#endif
