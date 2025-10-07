using UnityEngine;

public class LandingDetector : MonoBehaviour
{
    public Ground3D ground;
    public Rigidbody rb;
    public float minFallDistance = 0.2f;
    public float heightWeight = 3.0f;
    public float cooldown = 0.1f;

    bool wasGrounded = true;
    float fallStartHeight;
    float lastTriggerTime = -999f;

    void Reset()
    {
        ground = GetComponentInChildren<Ground3D>();
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (ground == null) return;

        Vector3 g = Physics.gravity;
        if (g.sqrMagnitude < 1e-6f) return;

        Vector3 up = -g.normalized;
        float currentHeight = Vector3.Dot(transform.position, up);
        bool grounded = ground.GetOnGround();

        if (wasGrounded && !grounded)
        {
            fallStartHeight = currentHeight;
        }

        if (!wasGrounded && grounded)
        {
            float fallDistance = Mathf.Max(0f, fallStartHeight - currentHeight);
            float speedAlongGravity = 0f;
            if (rb != null) speedAlongGravity = Mathf.Max(0f, -Vector3.Dot(rb.linearVelocity, up));
            float impact = (fallDistance * heightWeight) + speedAlongGravity;

            if (fallDistance >= minFallDistance && (Time.time - lastTriggerTime) >= cooldown)
            {
                AudioManager.Instance.Play("Landing_SFX",5f);
                lastTriggerTime = Time.time;
            }
        }

        wasGrounded = grounded;
    }
}
