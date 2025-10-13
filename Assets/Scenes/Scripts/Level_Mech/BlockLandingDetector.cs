using System;
using UnityEngine;
using UnityEngine.Events;

public class BlockLandingDetector : MonoBehaviour
{
    [Header("Refs")]
    public Ground3D ground;
    public Rigidbody rb;

    [Header("Tune")]
    [Tooltip("Minimum vertical fall distance to consider this a 'landing'.")]
    public float minFallDistance = 2f;
    [Tooltip("Weight applied to fall height when computing impact.")]
    public float heightWeight = 3.0f;
    [Tooltip("Cooldown between landing triggers.")]
    public float cooldown = 0.1f;

    public float LastImpact { get; private set; }

    [Header("Callbacks")]
    public UnityEvent<float> onLanded;          // Inspector-friendly
    public event Action<float> OnLanded;        // Code subscription

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

        // falling started
        if (wasGrounded && !grounded)
            fallStartHeight = currentHeight;

        // just landed
        if (!wasGrounded && grounded)
        {
            float fallDistance = Mathf.Max(0f, fallStartHeight - currentHeight);

            float speedAlongGravity = 0f;
#if UNITY_6000_0_OR_NEWER
            if (rb != null) speedAlongGravity = Mathf.Max(0f, -Vector3.Dot(rb.linearVelocity, up));
#else
            if (rb != null) speedAlongGravity = Mathf.Max(0f, -Vector3.Dot(rb.velocity, up));
#endif
            float impact = (fallDistance * heightWeight) + speedAlongGravity;

            if (fallDistance >= minFallDistance && (Time.time - lastTriggerTime) >= cooldown)
            {
                TriggerLand(impact);
            }
        }

        wasGrounded = grounded;
    }

    void TriggerLand(float impact)
    {
        LastImpact = impact;

        AudioManager.Instance?.Play("CubeFall_SFX");
        onLanded?.Invoke(impact);
        OnLanded?.Invoke(impact);

        lastTriggerTime = Time.time;
    }
}
