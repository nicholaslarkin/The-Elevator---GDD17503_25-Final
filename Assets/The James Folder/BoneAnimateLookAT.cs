using UnityEngine;

/// <summary>
/// Blends this Transform's rotation so it "looks at" a target (e.g., camera),
/// while still respecting the underlying animation.
/// Drop it on a bone (usually head). On add, it auto-wires:
/// - headBone = this.transform
/// - target   = Camera.main (if found)
/// Then it only runs in play mode.
/// </summary>
public class BoneLookAtController : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Bone to rotate (e.g., head bone). Defaults to this.transform on first add.")]
    public Transform headBone;

    [Tooltip("Target to look at. Defaults to Camera.main on first add, if available.")]
    public Transform target;

    [Tooltip("Optional Animator reference. Not required, but useful for clarity.")]
    public Animator animator;

    [Header("Look Settings")]
    [Range(0f, 1f)]
    [Tooltip("How strongly the bone is rotated toward the target. 0 = no effect, 1 = full look-at.")]
    public float weight = 1.0f;

    [Tooltip("Maximum angle in degrees the bone is allowed to rotate away from its animated orientation.")]
    [Range(0f, 180f)]
    public float maxAngle = 80f;

    [Tooltip("Smooth damping speed for rotation blending. Higher = snappier.")]
    public float rotationLerpSpeed = 10f;

    [Header("Runtime Options")]
    [Tooltip("If true, the script will run in LateUpdate and adjust the pose every frame (play mode only).")]
    public bool applyInLateUpdate = true;

    // Internal cache of the bone's rotation before we apply look-at logic
    private Quaternion _animatedLocalRotation;

    /// <summary>
    /// Called by Unity when the component is first added or reset from the inspector.
    /// This is where we do the one-time auto-wiring.
    /// </summary>
    private void Reset()
    {
        // Auto-assign headBone to this transform if empty.
        if (headBone == null)
        {
            headBone = transform;
        }

        // Auto-assign target to Camera.main if available.
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        // Auto-assign animator from parents if possible (optional quality-of-life).
        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }
    }

    private void LateUpdate()
    {
        // Do absolutely nothing in edit mode.
        if (!Application.isPlaying)
            return;

        if (!applyInLateUpdate)
            return;

        ApplyLookAt();
    }

    /// <summary>
    /// Public method in case you want to call this manually from elsewhere.
    /// Only does work in play mode.
    /// </summary>
    public void ApplyLookAt()
    {
        if (!Application.isPlaying)
            return;

        if (headBone == null || target == null || weight <= 0f)
            return;

        // 1) Cache the bone’s current animated local rotation (from Animator) every frame.
        _animatedLocalRotation = headBone.localRotation;

        // 2) Direction from bone to target in world space.
        Vector3 bonePosition = headBone.position;
        Vector3 toTargetWorld = target.position - bonePosition;

        if (toTargetWorld.sqrMagnitude < 0.0001f)
            return;

        toTargetWorld.Normalize();

        // 3) Current "animated forward" in world space (assuming +Z forward).
        Vector3 animatedForwardWorld = headBone.rotation * Vector3.forward;

        // 4) World-space rotation that looks at the target.
        Quaternion lookRotationWorld = Quaternion.LookRotation(toTargetWorld, headBone.up);

        // 5) Convert that desired world rotation back into local space.
        Quaternion desiredLocalRotation = Quaternion.Inverse(headBone.parent.rotation) * lookRotationWorld;

        // 6) Clamp the rotation angle relative to the original animated pose.
        float angle = Quaternion.Angle(_animatedLocalRotation, desiredLocalRotation);
        if (angle > maxAngle)
        {
            float t = maxAngle / Mathf.Max(angle, 0.0001f);
            desiredLocalRotation = Quaternion.Slerp(_animatedLocalRotation, desiredLocalRotation, t);
        }

        // 7) Blend from animation rotation to our desired rotation using weight.
        Quaternion targetLocalRotation = Quaternion.Slerp(_animatedLocalRotation, desiredLocalRotation, weight);

        // 8) Smooth over time for nice motion.
        headBone.localRotation = Quaternion.Slerp(
            headBone.localRotation,
            targetLocalRotation,
            1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime)
        );
    }
}