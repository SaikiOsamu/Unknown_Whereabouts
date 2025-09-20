using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RayChainSystem : MonoBehaviour
{
    public enum DirectionMode { AxisForward, EmitterForward, WorldVector, TowardsTarget }
    public enum LaserMode { OnWhenHit, OnWhenActive, Manual }
    public enum EventFireMode { AnyLink, LastLink, SpecificIndex }

    [System.Serializable]
    public class RayLink
    {
        public Transform emitter;
        public Transform receiver;
        public Transform directionAxis;
        public DirectionMode directionMode = DirectionMode.AxisForward;
        public Vector3 worldDirection = Vector3.forward;
        public float maxDistance = 100f;
        public bool startActive = false;
        public bool continuousScan = true;
        public bool hitTriggers = false;
        public bool enableNextOnSatisfied = true;
        public bool returnOnLose = false;
        public GameObject laser;
        public LaserMode laserMode = LaserMode.OnWhenHit;
        public bool drawDebugRay = true;
        [HideInInspector] public bool isActive;
        [HideInInspector] public bool satisfied;
        [HideInInspector] public bool everSatisfied;
    }

    public string triggerTag = "LightTrigger";
    public List<RayLink> links = new List<RayLink>();
    public UnityEngine.Events.UnityEvent onAnyLinkSatisfiedOnce;
    public UnityEngine.Events.UnityEvent onAnyLinkLoseAfterSatisfied;

    public EventFireMode eventFireMode = EventFireMode.LastLink;
    public int eventIndex = 0;

    private void Start()
    {
        for (int i = 0; i < links.Count; i++)
        {
            var L = links[i];
            L.isActive = (i == 0) ? true : false;
            if (L.startActive && i == 0) L.isActive = true;
            ApplyLaserVisibility(L, true, false);
            L.satisfied = false;
        }
        RecomputeActivationFrom(0);
    }

    private void Update()
    {
        for (int i = 0; i < links.Count; i++)
        {
            var L = links[i];
            if (!L.isActive) continue;
            if (!L.continuousScan) continue;
            CastLink(i);
        }
    }

    public void FireLinkOnce(int index)
    {
        if (index < 0 || index >= links.Count) return;
        CastLink(index);
    }

    private bool ShouldFireForIndex(int index)
    {
        if (links.Count == 0) return false;
        switch (eventFireMode)
        {
            case EventFireMode.AnyLink: return true;
            case EventFireMode.LastLink: return index == links.Count - 1;
            case EventFireMode.SpecificIndex:
                int i = Mathf.Clamp(eventIndex, 0, links.Count - 1);
                return index == i;
            default: return false;
        }
    }

    private void CastLink(int index)
    {
        var L = links[index];
        if (L.emitter == null) return;

        Vector3 dir = GetDirection(L);
        if (L.drawDebugRay) Debug.DrawRay(L.emitter.position, dir * L.maxDistance, Color.cyan);

        var q = L.hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        bool hitValid = false;

        if (Physics.Raycast(L.emitter.position, dir, out RaycastHit hit, L.maxDistance, Physics.DefaultRaycastLayers, q))
        {
            if (hit.collider.CompareTag(triggerTag) && IsSameOrAncestorOrDescendant(hit.collider.transform, L.receiver))
                hitValid = true;
        }

        bool wasSatisfied = L.satisfied;
        ApplyLaserVisibility(L, false, hitValid);

        if (hitValid && !L.satisfied)
        {
            L.satisfied = true;
            L.everSatisfied = true;
            if (ShouldFireForIndex(index))
                onAnyLinkSatisfiedOnce?.Invoke();
            RecomputeActivationFrom(index);
        }
        else if (!hitValid && L.satisfied)
        {
            L.satisfied = false;
            if (ShouldFireForIndex(index) && L.returnOnLose)
                onAnyLinkLoseAfterSatisfied?.Invoke();
            RecomputeActivationFrom(index);
        }
        else if (!hitValid && wasSatisfied == false)
        {
            RecomputeActivationFrom(index);
        }
    }

    private void RecomputeActivationFrom(int indexChanged)
    {
        if (links == null || links.Count == 0) return;

        for (int i = indexChanged; i < links.Count - 1; i++)
        {
            var cur = links[i];
            var next = links[i + 1];
            if (!cur.enableNextOnSatisfied) break;

            bool shouldActive = cur.satisfied;
            if (next.isActive != shouldActive)
            {
                next.isActive = shouldActive;
                if (!shouldActive)
                {
                    next.satisfied = false;
                    ApplyLaserVisibility(next, true, false);
                }
                else
                {
                    ApplyLaserVisibility(next, true, false);
                }
            }

            if (!shouldActive)
            {
                for (int j = i + 2; j < links.Count; j++)
                {
                    if (!links[j - 1].enableNextOnSatisfied) break;
                    links[j].isActive = false;
                    links[j].satisfied = false;
                    ApplyLaserVisibility(links[j], true, false);
                }
                break;
            }
        }
    }

    private void ApplyLaserVisibility(RayLink L, bool initial, bool hit)
    {
        if (L.laser == null) return;
        if (L.laserMode == LaserMode.Manual) return;
        if (L.laserMode == LaserMode.OnWhenActive)
        {
            L.laser.SetActive(L.isActive);
            return;
        }
        if (L.laserMode == LaserMode.OnWhenHit)
        {
            if (initial) { L.laser.SetActive(false); return; }
            L.laser.SetActive(hit);
        }
    }

    private Vector3 GetDirection(RayLink L)
    {
        switch (L.directionMode)
        {
            case DirectionMode.AxisForward:
                if (L.directionAxis != null) return L.directionAxis.forward;
                if (L.emitter != null) return L.emitter.forward;
                return Vector3.forward;
            case DirectionMode.EmitterForward:
                return L.emitter ? L.emitter.forward : Vector3.forward;
            case DirectionMode.WorldVector:
                return L.worldDirection.sqrMagnitude > 0f ? L.worldDirection.normalized : Vector3.forward;
            case DirectionMode.TowardsTarget:
                if (L.emitter != null && L.receiver != null)
                {
                    Vector3 v = L.receiver.position - L.emitter.position;
                    return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
                }
                return Vector3.forward;
            default:
                return Vector3.forward;
        }
    }

    private bool IsSameOrAncestorOrDescendant(Transform a, Transform b)
    {
        if (a == null || b == null) return false;
        Transform t = a;
        while (t != null)
        {
            if (t == b) return true;
            t = t.parent;
        }
        t = b;
        while (t != null)
        {
            if (t == a) return true;
            t = t.parent;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (links == null) return;
        Gizmos.matrix = Matrix4x4.identity;
        for (int i = 0; i < links.Count; i++)
        {
            var L = links[i];
            if (L == null) continue;
            if (L.emitter)
            {
                Vector3 dir = GetDirection(L);
                Gizmos.color = i == 0 ? Color.cyan : Color.yellow;
                Gizmos.DrawLine(L.emitter.position, L.emitter.position + dir * Mathf.Max(1f, L.maxDistance * 0.1f));
            }
            if (L.receiver)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(L.receiver.position, Vector3.one * 0.2f);
            }
        }
    }
#endif
}
