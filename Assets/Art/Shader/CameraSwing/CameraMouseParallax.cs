using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraMouseParallax : MonoBehaviour
{
    [Header("Rotation (degrees)")]
    public float maxYaw = 3f;      // left-right
    public float maxPitch = 2f;    // up-down
    public bool invertX = false;
    public bool invertY = false;

    [Header("Optional position parallax (world units)")]
    public float positionAmplitude = 0f;

    [Header("Smoothing")]
    public float smooth = 8f;      // higher = snappier

    Quaternion baseRot;
    Vector3 basePos;

    void Awake()
    {
        baseRot = transform.rotation;
        basePos = transform.position;
    }

    void LateUpdate()
    {
        // 1) get mouse position in viewport [0..1]
        Vector2 mouse;
#if ENABLE_INPUT_SYSTEM
        mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        mouse = Input.mousePosition;
#endif

        Vector2 viewport = new Vector2(mouse.x / Screen.width, mouse.y / Screen.height);
        // center to [-1..1]
        Vector2 p = (viewport - new Vector2(0.5f, 0.5f)) * 2f;
        p = Vector2.ClampMagnitude(p, 1f);

        if (invertX) p.x = -p.x;
        if (invertY) p.y = -p.y;

        // 2) compute target rotation
        float yaw = -p.x * maxYaw;   // mouse right -> slight look right (flip sign if you prefer)
        float pitch = p.y * maxPitch;

        Quaternion targetRot = baseRot * Quaternion.Euler(pitch, yaw, 0f);

        // nice smooth factor (exp smoothing, framerate independent)
        float t = 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);

        // 3) optional tiny position drift
        if (positionAmplitude > 0f)
        {
            Vector3 targetPos = basePos + new Vector3(p.x, p.y, 0f) * positionAmplitude;
            transform.position = Vector3.Lerp(transform.position, targetPos, t);
        }
    }

    // Call this if you change camera parent or want to re-center at runtime
    public void RecenterNow()
    {
        baseRot = transform.rotation;
        basePos = transform.position;
    }
}
