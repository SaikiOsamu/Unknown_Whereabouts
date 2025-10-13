using System.Collections;
using UnityEngine;

public class LandingDetector : MonoBehaviour
{
    [Header("Refs")]
    public Ground3D ground;
    public Rigidbody rb;
    public Animator animator;

    [Header("Tune")]
    public float minFallDistance = 0.2f;
    public float heightWeight = 3.0f;
    public float cooldown = 0.1f;
    public float minImpactForAnim = 0.2f;

    [Header("Animator Params")]
    public string speedParam = "Speed";
    public string landTrigger = "Land";
    public string impactParam = "Impact";

    [Header("Land State Watching")]
    public string landStateName = "Land";
    public int landLayerIndex = 0;

    [Header("Lock During Landing")]
    public MonoBehaviour[] systemsToDisable;
    public bool freezeRigidbodyDuringLock = false;

    [Header("Airborne Gates")]
    public float minAirTime = 0.06f;
    public float minDownVelToStart = 2f;
    public float minDownVelToLand = 3.5f;

    [Header("Animation Toggle")]
    public bool enableAnimation = true;

    bool wasGrounded = true;
    float fallStartHeight;
    float lastTriggerTime = -999f;

    int landStateHash;
    Coroutine landLockCo;
    bool isLocked;

    float airborneTime = 0f;
    bool inFall = false;
    float maxDownVel = 0f;

    void Awake()
    {
        landStateHash = Animator.StringToHash(landStateName);
    }

    void Reset()
    {
        ground = GetComponentInChildren<Ground3D>();
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    void FixedUpdate()
    {
        if (ground == null) return;

        Vector3 g = Physics.gravity;
        if (g.sqrMagnitude < 1e-6f) return;

        Vector3 up = -g.normalized;
        float currentHeight = Vector3.Dot(transform.position, up);
        bool grounded = ground.GetOnGround();

        float vDown = 0f;
        if (rb != null) vDown = Mathf.Max(0f, -Vector3.Dot(rb.linearVelocity, up));

        if (!grounded)
        {
            airborneTime += Time.fixedDeltaTime;
            maxDownVel = Mathf.Max(maxDownVel, vDown);

            if (!inFall)
            {
                if (airborneTime >= Time.fixedDeltaTime && vDown >= minDownVelToStart)
                {
                    inFall = true;
                    fallStartHeight = currentHeight;
                }
            }
        }
        else
        {
            if (inFall)
            {
                float fallDistance = Mathf.Max(0f, fallStartHeight - currentHeight);
                float impact = (fallDistance * heightWeight) + maxDownVel;
                bool gateOK = (airborneTime >= minAirTime) || (maxDownVel >= minDownVelToLand);

                if (gateOK && fallDistance >= minFallDistance && (Time.time - lastTriggerTime) >= cooldown)
                {
                    TriggerLand(impact);
                }
            }

            airborneTime = 0f;
            inFall = false;
            maxDownVel = 0f;
        }

        wasGrounded = grounded;

        if (animator != null && rb != null)
        {
            float horizSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            animator.SetFloat(speedParam, horizSpeed, 0.1f, Time.fixedDeltaTime);
        }
    }

    void TriggerLand(float impact)
    {
        if (enableAnimation && animator != null && impact >= minImpactForAnim)
        {
            LockSystems(true);
            animator.SetFloat(impactParam, impact);
            animator.SetTrigger(landTrigger);

            if (landLockCo != null) StopCoroutine(landLockCo);
            landLockCo = StartCoroutine(WaitLandStateAndUnlock());
        }

        AudioManager.Instance?.Play("Landing_SFX", 5f);
        lastTriggerTime = Time.time;
    }

    IEnumerator WaitLandStateAndUnlock()
    {
        while (!IsInState(animator, landLayerIndex, landStateHash))
            yield return null;

        while (true)
        {
            if (!IsInState(animator, landLayerIndex, landStateHash))
                break;

            var info = animator.GetCurrentAnimatorStateInfo(landLayerIndex);

            if (info.normalizedTime >= 1f && !animator.IsInTransition(landLayerIndex))
                break;

            yield return null;
        }

        LockSystems(false);
        landLockCo = null;
    }

    bool IsInState(Animator a, int layer, int shortNameHash)
    {
        if (a == null) return false;
        return a.GetCurrentAnimatorStateInfo(layer).shortNameHash == shortNameHash;
    }

    void LockSystems(bool lockOn)
    {
        if (isLocked == lockOn) return;
        isLocked = lockOn;

        foreach (var s in systemsToDisable)
            if (s) s.enabled = !lockOn;

        if (freezeRigidbodyDuringLock && rb)
            rb.isKinematic = lockOn;
    }
}
