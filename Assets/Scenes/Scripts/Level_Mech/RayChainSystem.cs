using UnityEngine;
using System.Collections;

public class RayChainSystem : MonoBehaviour
{
    public enum DirectionMode { AxisForward, EmitterForward, WorldVector, TowardsTarget }

    [Header("EmitterA -> ReceiverA")]
    public Transform emitterA;
    public Transform directionAxisA;
    public DirectionMode directionModeA = DirectionMode.AxisForward;
    public Vector3 worldDirectionA = Vector3.forward;
    public float maxDistanceA = 100f;
    public Transform receiverA;
    public bool autoFireAOnStart = true;
    public bool continuousScanA = true;
    public bool hitTriggersA = false;

    [Header("EmitterB -> ReceiverB")]
    public Transform emitterB;
    public Transform directionAxisB;
    public DirectionMode directionModeB = DirectionMode.AxisForward;
    public Vector3 worldDirectionB = Vector3.forward;
    public float maxDistanceB = 100f;
    public Transform receiverB;
    public bool startWithBInactive = true;
    public bool hitTriggersB = false;

    [Header("Wall Move (translation only)")]
    public Transform wall;
    public Transform wallTarget;
    public float moveDuration = 1.0f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    public bool drawDebugRay = true;

    private bool _emitterBEnabled = false;
    private bool _isMoving = false;
    private bool _aSatisfied = false;
    private bool _wallMoved = false;

    private const string LightTriggerTag = "LightTrigger";

    private void Start()
    {
        if (startWithBInactive && emitterB != null) emitterB.gameObject.SetActive(false);
        if (autoFireAOnStart)
        {
            if (continuousScanA) _aSatisfied = false;
            else FireFromEmitterA();
        }
    }

    private void Update()
    {
        if (continuousScanA && !_aSatisfied) CastFromEmitterA();
        if (_emitterBEnabled) CastFromEmitterB();
    }

    public void FireFromEmitterA()
    {
        CastFromEmitterA();
    }

    private void CastFromEmitterA()
    {
        if (emitterA == null || receiverA == null) return;

        Vector3 dirA = GetDirectionA();
        if (drawDebugRay) Debug.DrawRay(emitterA.position, dirA * maxDistanceA, Color.cyan);

        var qA = hitTriggersA ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        if (Physics.Raycast(emitterA.position, dirA, out RaycastHit hit, maxDistanceA, Physics.DefaultRaycastLayers, qA))
        {
            if (hit.collider.CompareTag(LightTriggerTag) && IsSameOrAncestorOrDescendant(hit.collider.transform, receiverA))
            {
                _aSatisfied = true;
                EnableEmitterB();
            }
        }
    }

    private void EnableEmitterB()
    {
        _emitterBEnabled = true;
        if (emitterB != null && !emitterB.gameObject.activeSelf) emitterB.gameObject.SetActive(true);
    }

    private void CastFromEmitterB()
    {
        if (emitterB == null) return;

        Vector3 dirB = GetDirectionB();
        if (drawDebugRay) Debug.DrawRay(emitterB.position, dirB * maxDistanceB, Color.yellow);

        var qB = hitTriggersB ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        if (Physics.Raycast(emitterB.position, dirB, out RaycastHit hit, maxDistanceB, Physics.DefaultRaycastLayers, qB))
        {
            if (receiverB != null &&
                hit.collider.CompareTag(LightTriggerTag) &&
                IsSameOrAncestorOrDescendant(hit.collider.transform, receiverB))
            {
                TryMoveWall();
            }
        }
    }

    private void TryMoveWall()
    {
        if (_isMoving || _wallMoved || wall == null || wallTarget == null) return;
        StartCoroutine(MoveWallCo());
    }

    private IEnumerator MoveWallCo()
    {
        _isMoving = true;

        Vector3 p0 = wall.position;
        Vector3 p1 = wallTarget.position;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, moveDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = moveCurve.Evaluate(Mathf.Clamp01(t));
            wall.position = Vector3.LerpUnclamped(p0, p1, k);
            yield return null;
        }

        wall.position = p1;
        _isMoving = false;
        _wallMoved = true;
    }

    private Vector3 GetDirectionA()
    {
        switch (directionModeA)
        {
            case DirectionMode.AxisForward:
                if (directionAxisA != null) return directionAxisA.forward;
                if (emitterA != null) return emitterA.forward;
                return Vector3.forward;
            case DirectionMode.EmitterForward:
                return emitterA ? emitterA.forward : Vector3.forward;
            case DirectionMode.WorldVector:
                return worldDirectionA.sqrMagnitude > 0f ? worldDirectionA.normalized : Vector3.forward;
            case DirectionMode.TowardsTarget:
                if (emitterA != null && receiverA != null)
                {
                    Vector3 v = receiverA.position - emitterA.position;
                    return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
                }
                return Vector3.forward;
            default:
                return Vector3.forward;
        }
    }

    private Vector3 GetDirectionB()
    {
        switch (directionModeB)
        {
            case DirectionMode.AxisForward:
                if (directionAxisB != null) return directionAxisB.forward;
                if (emitterB != null) return emitterB.forward;
                return Vector3.forward;
            case DirectionMode.EmitterForward:
                return emitterB ? emitterB.forward : Vector3.forward;
            case DirectionMode.WorldVector:
                return worldDirectionB.sqrMagnitude > 0f ? worldDirectionB.normalized : Vector3.forward;
            case DirectionMode.TowardsTarget:
                if (emitterB != null && receiverB != null)
                {
                    Vector3 v = receiverB.position - emitterB.position;
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
        Transform t = a; while (t != null) { if (t == b) return true; t = t.parent; }
        t = b; while (t != null) { if (t == a) return true; t = t.parent; }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawDebugRay) return;
        if (emitterA)
        {
            Vector3 dirA = GetDirectionA();
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(emitterA.position, emitterA.position + dirA * Mathf.Max(1f, maxDistanceA * 0.1f));
        }
        if (_emitterBEnabled && emitterB)
        {
            Vector3 dirB = GetDirectionB();
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(emitterB.position, emitterB.position + dirB * Mathf.Max(1f, maxDistanceB * 0.1f));
        }
        if (wall && wallTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(wall.position, wallTarget.position);
            Gizmos.DrawWireCube(wallTarget.position, Vector3.one * 0.2f);
        }
    }
#endif
}
