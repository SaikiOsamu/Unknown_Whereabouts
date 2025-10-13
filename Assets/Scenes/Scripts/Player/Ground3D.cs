using UnityEngine;

public class Ground3D : MonoBehaviour
{
    [Header("Settings")]
    [Range(0f, 89f)] public float maxGroundAngle = 50f;
    public float castRadius = 0.25f;
    public float castDistance = 0.2f;
    public float castOffset = 0.05f;
    public LayerMask groundMask = ~0;
    public Transform feet;

    [Header("Debug")]
    public bool debugDraw = false;

    float minGroundDot;
    bool onGround;
    float bestDotThisFrame = -1f;

    void Awake()
    {
        minGroundDot = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void FixedUpdate()
    {
        Vector3 g = Physics.gravity;
        Vector3 up = g.sqrMagnitude > 1e-6f ? (-g).normalized : Vector3.up;

        bool contactGround = bestDotThisFrame >= minGroundDot;
        bool castGround = false;

        if (!contactGround)
            castGround = GroundCast(up);

        onGround = contactGround || castGround;
        bestDotThisFrame = -1f;
    }

    void OnCollisionEnter(Collision c) { EvaluateCollision(c); }
    void OnCollisionStay(Collision c) { EvaluateCollision(c); }
    void OnCollisionExit(Collision c) { }

    void EvaluateCollision(Collision c)
    {
        Vector3 g = Physics.gravity;
        Vector3 up = g.sqrMagnitude > 1e-6f ? (-g).normalized : Vector3.up;

        for (int i = 0; i < c.contactCount; i++)
        {
            Vector3 n = c.GetContact(i).normal;
            float d = Vector3.Dot(n, up);
            if (d > bestDotThisFrame) bestDotThisFrame = d;
        }
    }

    bool GroundCast(Vector3 up)
    {
        Vector3 origin = feet ? feet.position : transform.position;
        origin += up * castOffset;

        if (Physics.SphereCast(origin, castRadius, -up, out RaycastHit hit, castDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw)
            {
                Debug.DrawLine(origin, origin + (-up) * hit.distance, Color.green, 0.05f);
                Debug.DrawRay(hit.point, hit.normal * 0.2f, Color.yellow, 0.05f);
            }
            float d = Vector3.Dot(hit.normal, up);
            return d >= minGroundDot;
        }

        if (debugDraw)
            Debug.DrawLine(origin, origin + (-up) * castDistance, Color.red, 0.05f);

        return false;
    }

    public bool GetOnGround() => onGround;
}
