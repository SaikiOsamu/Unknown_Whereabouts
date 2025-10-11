using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PushableBlock : MonoBehaviour
{
    [Header("Rail")]
    public Transform pointA;
    public Transform pointB;

    [Header("Push Tuning")]
    public float pushSpeedScale = 2.5f;
    public float maxSpeed = 6f;
    public float damping = 5f;
    public float followGain = 14f;
    public float assistBoost = 0.6f;

    [Tooltip("Minimum axis speed (m/s) a player must generate to count as pushing (filters rotation-only jitter).")]
    public float minLinearSpeed = 0.05f;

    [Tooltip("Extra allowance (m) where we consider the player 'in contact' even if slightly ahead/behind the block on the rail.")]
    public float contactSlack = 0.3f;

    [Tooltip("If player's axis motion is smaller than this, we treat it as not moving toward/away.")]
    public float towardEpsilon = 0.02f;

    [Header("Detection")]
    [Tooltip("Only colliders on these layers can push (set your Player to this layer).")]
    public LayerMask playerLayer = 0;

    private Vector3 railOrigin;
    private Vector3 railDir;
    private float railLen;
    private float t;
    private float axisVel;
    private bool gotPush;

    private BoxCollider triggerCol;

    // We keep last positions to compute velocity if the player has no Rigidbody/CharacterController
    private readonly Dictionary<Transform, Vector3> lastPlayerPos = new Dictionary<Transform, Vector3>(4);

    void Awake()
    {
        triggerCol = GetComponent<BoxCollider>();
        if (triggerCol != null) triggerCol.isTrigger = true;
        RecomputeRail();
        t = WorldToT(transform.position);
        ClampAndPlace();
    }

    void OnValidate()
    {
        if (pointA && pointB && pointA != pointB) RecomputeRail();
        if (triggerCol == null) triggerCol = GetComponent<BoxCollider>();
        if (triggerCol != null && !triggerCol.isTrigger) triggerCol.isTrigger = true;
    }

    private void RecomputeRail()
    {
        if (!pointA || !pointB) return;
        railOrigin = pointA.position;
        Vector3 ab = pointB.position - pointA.position;
        railLen = Mathf.Max(0.0001f, ab.magnitude);
        railDir = ab / railLen;
    }

    private float WorldToT(Vector3 p)
    {
        float proj = Vector3.Dot(p - railOrigin, railDir);
        return Mathf.InverseLerp(0f, railLen, proj);
    }

    private Vector3 TToWorld(float param) => railOrigin + railDir * Mathf.Lerp(0f, railLen, param);

    private void ClampAndPlace()
    {
        float clamped = Mathf.Clamp01(t);
        if (!Mathf.Approximately(clamped, t))
        {
            if ((t <= 0f && axisVel < 0f) || (t >= 1f && axisVel > 0f)) axisVel = 0f;
            t = clamped;
        }
        transform.position = TToWorld(t);
    }

    void Update()
    {
        if (!pointA || !pointB) return;

        // If your rail endpoints can move, we keep it fresh.
        RecomputeRail();
        t = WorldToT(transform.position);

        float dt = Mathf.Max(Time.deltaTime, 1e-6f);
        PollTriggerAndApplyPushes(dt);

        if (!gotPush)
        {
            // Exponential decay to rest when no one is pushing
            axisVel = Mathf.Lerp(axisVel, 0f, 1f - Mathf.Exp(-damping * dt));
        }
        gotPush = false;

        axisVel = Mathf.Clamp(axisVel, -maxSpeed, maxSpeed);
        t += (axisVel * dt) / Mathf.Max(railLen, 1e-6f);
        ClampAndPlace();
    }

    private void PollTriggerAndApplyPushes(float dt)
    {
        if (triggerCol == null) return;

        var tr = triggerCol.transform;
        var m = tr.localToWorldMatrix;

        Vector3 center = m.MultiplyPoint3x4(triggerCol.center);
        Vector3 halfExtents = Vector3.Scale(triggerCol.size * 0.5f, tr.lossyScale);
        Quaternion orientation = tr.rotation;

        // Only players on the specified layer(s)
        int layerMask = playerLayer.value == 0 ? ~0 : playerLayer.value;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.Ignore);

        var seenThisFrame = HashSetPool<Transform>.Get();

        float combinedAxisSpeed = 0f;
        int contributors = 0;

        float sBlock = Vector3.Dot(transform.position, railDir);

        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.transform == transform) continue;

            // Layer check (already enforced by OverlapBox mask), but keep in case mask is wide:
            if ((playerLayer.value & (1 << col.gameObject.layer)) == 0 && playerLayer.value != 0) continue;

            Transform pTr = col.transform;
            seenThisFrame.Add(pTr);

            // Best-effort player velocity along the rail:
            Vector3 v = GetWorldVelocity(pTr, dt);
            float axisSpeed = Vector3.Dot(v, railDir);

            // Ignore rotation-only or micro jitter (root motion shuffles etc.)
            if (Mathf.Abs(axisSpeed) < minLinearSpeed) continue;

            float sPlayer = Vector3.Dot(pTr.position, railDir);
            float gap = sBlock - sPlayer;

            // If the player is close enough along the rail, allow small positive axisSpeed to count as "toward"
            bool isClose = Mathf.Abs(gap) <= contactSlack;

            bool movingToward =
                (gap > 0f && axisSpeed > towardEpsilon) ||
                (gap < 0f && axisSpeed < -towardEpsilon) ||
                (isClose && Mathf.Abs(axisSpeed) > towardEpsilon * 0.5f);

            if (!movingToward) continue;

            combinedAxisSpeed += axisSpeed;
            contributors++;
        }

        // Clean up last positions for transforms that left the trigger
        if (lastPlayerPos.Count > 0)
        {
            var toRemove = ListPool<Transform>.Get();
            foreach (var kv in lastPlayerPos)
            {
                if (!seenThisFrame.Contains(kv.Key)) toRemove.Add(kv.Key);
            }
            foreach (var rem in toRemove) lastPlayerPos.Remove(rem);
            ListPool<Transform>.Release(toRemove);
        }

        HashSetPool<Transform>.Release(seenThisFrame);

        if (contributors > 0)
        {
            ApplyPush(combinedAxisSpeed);
        }
    }

    private Vector3 GetWorldVelocity(Transform pTr, float dt)
    {
        // Priority 1: Rigidbody velocity if present
        if (pTr.TryGetComponent<Rigidbody>(out var rb))
        {
            return rb.linearVelocity;
        }

        // Priority 2: CharacterController velocity if present
        if (pTr.TryGetComponent<CharacterController>(out var cc))
        {
            return cc.velocity;
        }

        // Fallback: finite-difference based on last frame position
        Vector3 curr = pTr.position;
        if (!lastPlayerPos.TryGetValue(pTr, out var last))
        {
            lastPlayerPos[pTr] = curr;
            return Vector3.zero; // first frame inside trigger, no velocity yet
        }

        Vector3 v = (curr - last) / Mathf.Max(dt, 1e-6f);
        lastPlayerPos[pTr] = curr;
        return v;
    }

    public void ApplyPush(float playerAxisSpeed)
    {
        if ((t <= 0f && playerAxisSpeed < 0f) || (t >= 1f && playerAxisSpeed > 0f)) return;

        float dt = Mathf.Max(Time.deltaTime, 1e-6f);

        // Follow the player's axis speed, smoothly
        float target = playerAxisSpeed * pushSpeedScale;

        float k = 1f - Mathf.Exp(-followGain * dt);
        axisVel = Mathf.Lerp(axisVel, target, k);

        // Only add assist if player is clearly pushing (prevents phantom nudges)
        if (Mathf.Abs(target) > minLinearSpeed)
        {
            axisVel += Mathf.Sign(target) * assistBoost;
        }

        gotPush = true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!pointA || !pointB) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawSphere(pointA.position, 0.05f);
        Gizmos.DrawSphere(pointB.position, 0.05f);

        var bc = GetComponent<BoxCollider>();
        if (bc)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            var m = bc.transform.localToWorldMatrix;
            Vector3 c = m.MultiplyPoint3x4(bc.center);
            Vector3 s = Vector3.Scale(bc.size, bc.transform.lossyScale);
            Gizmos.matrix = Matrix4x4.TRS(c, bc.transform.rotation, s);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
#endif

    static class HashSetPool<T>
    {
        static readonly Stack<HashSet<T>> pool = new Stack<HashSet<T>>();
        public static HashSet<T> Get() => pool.Count > 0 ? pool.Pop() : new HashSet<T>();
        public static void Release(HashSet<T> set) { set.Clear(); pool.Push(set); }
    }

    static class ListPool<T>
    {
        static readonly Stack<List<T>> pool = new Stack<List<T>>();
        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>();
        public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
    }
}
