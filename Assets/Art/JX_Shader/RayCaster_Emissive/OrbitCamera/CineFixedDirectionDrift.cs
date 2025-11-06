using UnityEngine;

/// <summary>
/// Slide (translate) a camera along a fixed direction, with user-controlled
/// rotation and speed. Attach to a Cinemachine Virtual Camera (or its parent).
/// Recommended: set the vcam's Body/Aim to "Do Nothing" so this script fully
/// drives the transform.
/// 
/// Controls (Legacy Input):
///   - W / S : move +dir / -dir
///   - Left Shift : speed multiplier
///   - RMB + Mouse : yaw/pitch look
///   - Arrows (optional): extra yaw/pitch nudge
/// </summary>
public class CineFixedDirectionDrift : MonoBehaviour
{
    [Header("Direction (the rail you move along)")]
    [Tooltip("Local-space direction which will be transformed by Direction Space (if assigned).")]
    public Vector3 localMoveDirection = Vector3.right;
    [Tooltip("If set, the localMoveDirection is evaluated in this transform's space; if null, world space.")]
    public Transform directionSpace;

    [Header("Speed")]
    [Tooltip("Constant drift speed along the direction (units/sec). Can be 0.")]
    public float baseDriftSpeed = 0f;
    [Tooltip("User input speed along the direction (units/sec).")]
    public float userMoveSpeed = 4f;
    [Tooltip("Multiplier when holding Shift.")]
    public float fastMultiplier = 2.5f;
    [Tooltip("Acceleration when speeding up (units/sec^2).")]
    public float acceleration = 12f;
    [Tooltip("Deceleration when slowing down / no input (units/sec^2).")]
    public float deceleration = 12f;

    [Header("Rotation")]
    [Tooltip("Hold RMB to rotate with mouse (legacy input).")]
    public bool requireHoldRMB = true;
    [Tooltip("Yaw/Pitch speed in degrees/sec in response to Mouse X/Y or arrows.")]
    public Vector2 lookSpeed = new Vector2(120f, 90f);
    [Tooltip("Pitch limits in degrees.")]
    public Vector2 pitchLimits = new Vector2(-20f, 75f);
    [Tooltip("Start rotation (Euler, degrees). Leave zeros to use current transform.")]
    public Vector2 initialYawPitch = Vector2.zero;

    [Header("Smoothing")]
    [Tooltip("How quickly to ease rotation toward the target (seconds). 0 = snap.")]
    public float rotationSmoothTime = 0.08f;
    [Tooltip("How quickly to ease position toward the target (seconds). 0 = snap.")]
    public float positionSmoothTime = 0.08f;

    // internals
    float _currentSpeed;
    float _yaw, _pitch;
    Vector3 _posVel;                      // for SmoothDamp
    Quaternion _rotSmoothed;

    void Awake()
    {
        if (initialYawPitch != Vector2.zero)
        {
            _yaw = initialYawPitch.x;
            _pitch = Mathf.Clamp(initialYawPitch.y, pitchLimits.x, pitchLimits.y);
        }
        else
        {
            // derive from current orientation
            Vector3 f = transform.forward;
            if (f.sqrMagnitude > 0.0001f)
            {
                _yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                _pitch = Mathf.Asin(f.y) * Mathf.Rad2Deg;
                _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);
            }
        }
        _rotSmoothed = transform.rotation;
    }

    void LateUpdate()
    {
        float dt = (Time.timeScale > 0f) ? Time.deltaTime : Time.unscaledDeltaTime;

        // 1) Direction vector (world space)
        Vector3 dirWS = (directionSpace != null)
            ? directionSpace.TransformDirection(localMoveDirection)
            : localMoveDirection;
        dirWS = dirWS.sqrMagnitude > 0.0001f ? dirWS.normalized : Vector3.right;

        // 2) Read user speed input (Legacy Input Manager)
        float desiredSpeed = baseDriftSpeed;
#if ENABLE_LEGACY_INPUT_MANAGER
        float forward = 0f;
        if (Input.GetKey(KeyCode.W)) forward += 1f;
        if (Input.GetKey(KeyCode.S)) forward -= 1f;

        float speed = userMoveSpeed * forward;
        if (Mathf.Abs(speed) > 0.0001f && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            speed *= fastMultiplier;

        desiredSpeed += speed;
#endif
        // 3) Accelerate / decelerate toward desired speed
        float accel = Mathf.Sign(desiredSpeed - _currentSpeed) > 0 ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, desiredSpeed, accel * dt);

        // 4) Compute desired position step
        Vector3 desiredPos = transform.position + dirWS * (_currentSpeed * dt);

        // 5) Rotation input (Legacy Input Manager)
#if ENABLE_LEGACY_INPUT_MANAGER
        bool rotate = !requireHoldRMB || Input.GetMouseButton(1);
        if (rotate)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            _yaw   += mx * lookSpeed.x * dt;
            _pitch -= my * lookSpeed.y * dt;
        }
        // Optional keyboard nudge
        if (Input.GetKey(KeyCode.LeftArrow))  _yaw -= lookSpeed.x * 0.5f * dt;
        if (Input.GetKey(KeyCode.RightArrow)) _yaw += lookSpeed.x * 0.5f * dt;
        if (Input.GetKey(KeyCode.UpArrow))    _pitch -= lookSpeed.y * 0.5f * dt;
        if (Input.GetKey(KeyCode.DownArrow))  _pitch += lookSpeed.y * 0.5f * dt;
#endif
        _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);
        Quaternion targetRot = Quaternion.Euler(_pitch, _yaw, 0f);

        // 6) Smooth apply
        Vector3 smoothPos = (positionSmoothTime <= 0f)
            ? desiredPos
            : Vector3.SmoothDamp(transform.position, desiredPos, ref _posVel, positionSmoothTime);

        _rotSmoothed = (rotationSmoothTime <= 0f)
            ? targetRot
            : Quaternion.Slerp(_rotSmoothed, targetRot, 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, rotationSmoothTime)));

        transform.SetPositionAndRotation(smoothPos, _rotSmoothed);
    }

    /// <summary>
    /// Set the rail direction at runtime (world space).
    /// </summary>
    public void SetWorldDirection(Vector3 worldDir)
    {
        localMoveDirection = directionSpace != null
            ? directionSpace.InverseTransformDirection(worldDir)
            : worldDir;
    }
}
