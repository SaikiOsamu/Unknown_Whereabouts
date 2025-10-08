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
    [Tooltip("Animator里落地动画的状态名（短名，非完整路径）。")]
    public string landStateName = "Land";
    [Tooltip("Land 所在层索引，通常是 Base Layer=0")]
    public int landLayerIndex = 0;

    [Header("Lock During Landing")]
    [Tooltip("上锁期间要禁用的组件（移动/输入/攻击等）")]
    public MonoBehaviour[] systemsToDisable;
    [Tooltip("落地动画期间是否临时冻结刚体")]
    public bool freezeRigidbodyDuringLock = false;

    bool wasGrounded = true;
    float fallStartHeight;
    float lastTriggerTime = -999f;

    int landStateHash;
    Coroutine landLockCo;
    bool isLocked;

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

        if (wasGrounded && !grounded)
            fallStartHeight = currentHeight;

        if (!wasGrounded && grounded)
        {
            float fallDistance = Mathf.Max(0f, fallStartHeight - currentHeight);
            float speedAlongGravity = 0f;
            if (rb != null) speedAlongGravity = Mathf.Max(0f, -Vector3.Dot(rb.linearVelocity, up));
            float impact = (fallDistance * heightWeight) + speedAlongGravity;

            if (fallDistance >= minFallDistance && (Time.time - lastTriggerTime) >= cooldown)
            {
                TriggerLand(impact);
            }
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
        if (animator != null && impact >= minImpactForAnim)
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
