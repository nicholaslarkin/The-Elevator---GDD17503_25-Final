using UnityEngine;

/// <summary>
/// Constrained neck look-at:
/// - Looks at a target (e.g., camera)
/// - Limits yaw (left/right) and pitch (up/down) around a calibrated "forward"
/// - Forces zero roll (no head cocking)
/// - Maintains a chosen up axis
/// - Runs late to override Animator / Timeline without jitter
/// </summary>
[DefaultExecutionOrder(10000)] // Run late to stomp animation / Timeline
public class NeckConstrainedLookAt : MonoBehaviour
{
    public enum UpAxisMode
    {
        WorldUp,
        CharacterUp,
        CustomVector,
        TransformUp
    }

    [Header("Targets")]
    [Tooltip("The neck (or head) bone to rotate.")]
    [SerializeField] private Transform neckBone;

    [Tooltip("Target to look at (e.g., the main camera).")]
    [SerializeField] private Transform lookTarget;

    [Tooltip("Root / character transform, used for UpAxisMode.CharacterUp.")]
    [SerializeField] private Transform characterRoot;

    [Header("Up Axis")]
    [SerializeField] private UpAxisMode upAxisMode = UpAxisMode.WorldUp;

    [Tooltip("Custom up vector when UpAxisMode = CustomVector.")]
    [SerializeField] private Vector3 customUpVector = Vector3.up;

    [Tooltip("Transform whose up is used when UpAxisMode = TransformUp.")]
    [SerializeField] private Transform upFromTransform;

    [Header("Angle Limits (relative to calibrated forward)")]
    [Tooltip("Max left/right rotation in degrees.")]
    [SerializeField] private float maxYawDegrees = 60f;

    [Tooltip("Max up/down rotation in degrees.")]
    [SerializeField] private float maxPitchDegrees = 40f;

    [Header("Control")]
    [Tooltip("If disabled, the script does nothing and animation is untouched.")]
    [SerializeField] private bool overrideEnabled = true;

    [Tooltip("Degrees-per-second style smoothing. 0 = snap instantly.")]
    [SerializeField] private float rotationSmoothingSpeed = 0f;

    [Header("Calibration")]
    [Tooltip("Automatically capture neutral neck pose on Start.")]
    [SerializeField] private bool autoCalibrateOnStart = true;

    [Tooltip("Neutral local rotation (forward pose).")]
    [SerializeField] private Quaternion neutralLocalRotation;

    [SerializeField, Tooltip("Has neutral pose been captured?")]
    private bool hasNeutral = false;

    private void Reset()
    {
        neckBone = transform;
        characterRoot = transform.root;
        lookTarget = Camera.main != null ? Camera.main.transform : null;

        if (neckBone != null)
        {
            neutralLocalRotation = neckBone.localRotation;
            hasNeutral = true;
        }
    }

    private void Start()
    {
        if (autoCalibrateOnStart && neckBone != null)
        {
            CalibrateNeutralFromCurrentPose();
        }
    }

    private void LateUpdate()
    {
        if (!overrideEnabled || neckBone == null || lookTarget == null || !hasNeutral)
            return;

        Transform parent = neckBone.parent;
        if (parent == null)
            return;

        // 1. World-space desired look rotation (no roll, up axis enforced)
        Vector3 toTarget = lookTarget.position - neckBone.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;
        toTarget.Normalize();

        Vector3 up = GetUpAxis();
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        up.Normalize();

        Quaternion desiredWorldRotation = Quaternion.LookRotation(toTarget, up);

        // 2. Convert to local space relative to parent
        Quaternion parentWorldRot = parent.rotation;
        Quaternion desiredLocalRotation = Quaternion.Inverse(parentWorldRot) * desiredWorldRotation;

        // 3. Compute delta from neutral and extract local yaw/pitch
        // delta = rotation that moves from neutral to desired
        Quaternion deltaFromNeutral = Quaternion.Inverse(neutralLocalRotation) * desiredLocalRotation;
        Vector3 deltaEuler = deltaFromNeutral.eulerAngles;

        // Convert euler from [0,360) to [-180,180)
        deltaEuler.x = NormalizeAngle(deltaEuler.x); // pitch
        deltaEuler.y = NormalizeAngle(deltaEuler.y); // yaw
        deltaEuler.z = 0f;                           // kill roll explicitly

        // 4. Clamp yaw & pitch
        float clampedPitch = Mathf.Clamp(deltaEuler.x, -maxPitchDegrees, maxPitchDegrees);
        float clampedYaw = Mathf.Clamp(deltaEuler.y, -maxYawDegrees, maxYawDegrees);

        // 5. Reconstruct local rotation: neutral + clamped delta, no roll
        Quaternion clampedLocalDelta =
            Quaternion.Euler(clampedPitch, clampedYaw, 0f);

        Quaternion targetLocalRotation = neutralLocalRotation * clampedLocalDelta;

        // 6. Convert back to world space
        Quaternion targetWorldRotation = parentWorldRot * targetLocalRotation;

        // 7. Apply, with optional smoothing, in WORLD space (overrides anim/Timeline)
        if (rotationSmoothingSpeed > 0f)
        {
            float t = 1f - Mathf.Exp(-rotationSmoothingSpeed * Time.deltaTime);
            neckBone.rotation = Quaternion.Slerp(neckBone.rotation, targetWorldRotation, t);
        }
        else
        {
            neckBone.rotation = targetWorldRotation;
        }
    }

    private Vector3 GetUpAxis()
    {
        switch (upAxisMode)
        {
            case UpAxisMode.WorldUp:
                return Vector3.up;

            case UpAxisMode.CharacterUp:
                if (characterRoot != null)
                    return characterRoot.up;
                break;

            case UpAxisMode.CustomVector:
                return customUpVector;

            case UpAxisMode.TransformUp:
                if (upFromTransform != null)
                    return upFromTransform.up;
                break;
        }

        return Vector3.up;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        else if (angle < -180f)
            angle += 360f;
        return angle;
    }

    /// <summary>
    /// Captures the current local rotation as the neutral "looking forward" pose.
    /// Call this when the character is in a natural forward-facing stance.
    /// </summary>
    [ContextMenu("Calibrate Neutral From Current Pose")]
    public void CalibrateNeutralFromCurrentPose()
    {
        if (neckBone == null)
            return;

        neutralLocalRotation = neckBone.localRotation;
        hasNeutral = true;
    }

    /// <summary>
    /// Enables or disables the override at runtime.
    /// </summary>
    public void SetOverrideEnabled(bool enabled)
    {
        overrideEnabled = enabled;
    }
}
