using UnityEngine;

/// <summary>
/// Forces a neck (or any bone) to orient toward a conceptual "forward"
/// direction or rotation, with a chosen up axis, overriding Animator/Timeline.
/// Put this on any GameObject and assign the neck bone.
/// </summary>
[DefaultExecutionOrder(10000)] // Try to run AFTER Animator / Timeline
public class NeckForwardOrienter : MonoBehaviour
{
    public enum ForwardSource
    {
        WorldDirection,     // Use a world-space direction vector
        TransformForward,   // Use another transform's forward
        WorldRotation       // Use a world-space rotation (Euler) as reference
    }

    public enum UpAxisMode
    {
        WorldUp,        // Use global Vector3.up
        CharacterUp,    // Use a character/root transform's up
        CustomVector,   // Use any custom vector
        TransformUp     // Use another transform's up
    }

    [Header("Targets")]
    [Tooltip("The neck (or other) bone whose rotation will be overridden.")]
    [SerializeField] private Transform neckBone;

    [Tooltip("Optional root/character transform, used when UpAxisMode = CharacterUp.")]
    [SerializeField] private Transform characterRoot;

    [Header("Forward Source")]
    [Tooltip("Where we get our conceptual 'forward' direction from.")]
    [SerializeField] private ForwardSource forwardSource = ForwardSource.WorldDirection;

    [Tooltip("World-space direction treated as 'forward' when ForwardSource = WorldDirection.")]
    [SerializeField] private Vector3 worldForwardDirection = Vector3.forward;

    [Tooltip("Transform whose forward will be treated as 'forward' when ForwardSource = TransformForward.")]
    [SerializeField] private Transform forwardFromTransform;

    [Tooltip("Euler angles representing desired WORLD rotation when ForwardSource = WorldRotation.")]
    [SerializeField] private Vector3 worldRotationEuler = Vector3.zero;

    [Header("Up Axis")]
    [Tooltip("How to choose the 'up' vector for the LookRotation.")]
    [SerializeField] private UpAxisMode upAxisMode = UpAxisMode.WorldUp;

    [Tooltip("Custom up vector when UpAxisMode = CustomVector.")]
    [SerializeField] private Vector3 customUpVector = Vector3.up;

    [Tooltip("Transform whose up will be used when UpAxisMode = TransformUp.")]
    [SerializeField] private Transform upFromTransform;

    [Header("Control")]
    [Tooltip("If false, this script does nothing and leaves animation alone.")]
    [SerializeField] private bool overrideEnabled = true;

    [Tooltip("Degrees-per-second style smoothing. 0 = snap instantly, no smoothing.")]
    [SerializeField] private float rotationSmoothingSpeed = 0f;

    [Header("Calibration (bind offset)")]
    [Tooltip("Internal offset between analytic LookRotation and the actual neck bind orientation.")]
    [SerializeField] private Quaternion calibrationOffset = Quaternion.identity;

    [SerializeField, Tooltip("Has a calibration been captured yet?")]
    private bool hasCalibration = false;

    private void Reset()
    {
        neckBone = transform;
        characterRoot = transform.root;
        calibrationOffset = Quaternion.identity;
        hasCalibration = false;
    }

    private void LateUpdate()
    {
        if (!overrideEnabled || neckBone == null)
            return;

        // 1. Get desired forward direction
        Vector3 forwardDir = GetForwardDirection();
        if (forwardDir.sqrMagnitude < 0.0001f)
            return;
        forwardDir.Normalize();

        // 2. Get up axis
        Vector3 up = GetUpAxis();
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        up.Normalize();

        // 3. Base look rotation (what Unity thinks "forward" + "up" should be)
        Quaternion baseLookRotation = Quaternion.LookRotation(forwardDir, up);

        // 4. Apply calibration offset so the bone's actual mesh axis lines up
        Quaternion targetRotation = hasCalibration
            ? baseLookRotation * calibrationOffset
            : baseLookRotation;

        // 5. Apply rotation, optionally smoothed, in WORLD space to stomp animation
        if (rotationSmoothingSpeed > 0f)
        {
            // Exponential smoothing to keep it framerate-independent-ish
            float t = 1f - Mathf.Exp(-rotationSmoothingSpeed * Time.deltaTime);
            neckBone.rotation = Quaternion.Slerp(neckBone.rotation, targetRotation, t);
        }
        else
        {
            neckBone.rotation = targetRotation;
        }
    }

    private Vector3 GetForwardDirection()
    {
        switch (forwardSource)
        {
            case ForwardSource.WorldDirection:
                return worldForwardDirection;

            case ForwardSource.TransformForward:
                if (forwardFromTransform != null)
                    return forwardFromTransform.forward;
                break;

            case ForwardSource.WorldRotation:
                Quaternion q = Quaternion.Euler(worldRotationEuler);
                return q * Vector3.forward;
        }

        // Fallback
        return Vector3.forward;
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

        // Fallback
        return Vector3.up;
    }

    /// <summary>
    /// Capture the current pose as the "correct" alignment for the current
    /// forward + up configuration. Use from the context menu or via code.
    /// </summary>
    [ContextMenu("Calibrate From Current Pose")]
    public void CalibrateFromCurrentPose()
    {
        if (neckBone == null)
            return;

        Vector3 fwd = GetForwardDirection();
        if (fwd.sqrMagnitude < 0.0001f)
            fwd = neckBone.forward;
        fwd.Normalize();

        Vector3 up = GetUpAxis();
        if (up.sqrMagnitude < 0.0001f)
            up = neckBone.up;
        up.Normalize();

        Quaternion baseLookRotation = Quaternion.LookRotation(fwd, up);

        // Offset so: baseLookRotation * calibrationOffset == current bone rotation
        calibrationOffset = Quaternion.Inverse(baseLookRotation) * neckBone.rotation;
        hasCalibration = true;
    }

    /// <summary>
    /// Enable or disable overriding animation at runtime.
    /// </summary>
    public void SetOverrideEnabled(bool enabled)
    {
        overrideEnabled = enabled;
    }
}
