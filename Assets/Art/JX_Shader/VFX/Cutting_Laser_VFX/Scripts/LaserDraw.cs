using System.Collections.Generic;
using UnityEngine;

public class LaserMover : MonoBehaviour
{
    public RayChainSystem.DirectionMode directionMode = RayChainSystem.DirectionMode.AxisForward;
    public Transform emitter;
    public Transform directionAxis;
    public Transform target;
    public Vector3 worldDirection = Vector3.forward;

    [Header("Laser Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Motion")]
    public float speed = 10f;
    public bool continuousAim = false;
    public float maxDistance = 0f;
    public bool destroyOnExceed = false;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public bool useSphereCast = false;
    public float collisionRadius = 0.05f;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("FX")]
    public GameObject spark;

    [Header("History")]
    public List<Vector3> positionHistory = new List<Vector3>();
    public int historyMaxCount = 4096;

    private Vector3 _moveDir = Vector3.forward;
    private LineRenderer _line;
    private float _axisDistance;
    private float _startAxisDistance;
    private bool _stopped;

    private void OnEnable()
    {
        _stopped = false;
        _moveDir = ComputeDirection().normalized;
        if (_moveDir.sqrMagnitude < 1e-6f) _moveDir = Vector3.forward;

        _line = GetComponent<LineRenderer>();
        if (_line != null && startPoint && endPoint)
        {
            _line.positionCount = 2;
            _line.SetPosition(0, startPoint.position);
            _line.SetPosition(1, endPoint.position);
        }

        Vector3 axisOrigin; Vector3 axisDir;
        GetAxis(out axisOrigin, out axisDir);

        Vector3 startPos = startPoint ? startPoint.position : transform.position;
        _startAxisDistance = ProjectSignedDistanceOnAxis(startPos, axisOrigin, axisDir);

        if (endPoint == null)
        {
            GameObject ep = new GameObject("EndPoint");
            ep.transform.SetPositionAndRotation(startPos, Quaternion.identity);
            endPoint = ep.transform;
        }

        _axisDistance = ProjectSignedDistanceOnAxis(endPoint.position, axisOrigin, axisDir);
        endPoint.position = axisOrigin + axisDir * _axisDistance;

        if (spark) spark.SetActive(false);
        RecordHistory(endPoint.position);
    }

    private void Update()
    {
        if (_stopped) return;

        Vector3 axisOrigin; Vector3 axisDir;
        GetAxis(out axisOrigin, out axisDir);

        if (continuousAim)
        {
            var newDir = ComputeDirection().normalized;
            if (newDir.sqrMagnitude > 1e-6f) _moveDir = newDir;
        }

        float axialSpeed = Mathf.Max(0f, Vector3.Dot(_moveDir, axisDir)) * speed;
        float delta = axialSpeed * Time.deltaTime;

        float startDistNow = ProjectSignedDistanceOnAxis(startPoint ? startPoint.position : transform.position, axisOrigin, axisDir);
        float range = maxDistance > 0f ? maxDistance : 1000f;

        Vector3 rayOrigin = axisOrigin + axisDir * startDistNow;
        RaycastHit hit = new RaycastHit();
        bool anyHit = useSphereCast
            ? Physics.SphereCast(rayOrigin, collisionRadius, axisDir, out hit, range, collisionMask, triggerInteraction)
            : Physics.Raycast(rayOrigin, axisDir, out hit, range, collisionMask, triggerInteraction);

        if (anyHit)
        {
            _axisDistance = Vector3.Dot(hit.point - axisOrigin, axisDir);
            if (spark)
            {
                if (!spark.activeSelf) spark.SetActive(true);
                spark.transform.position = hit.point;
            }
        }
        else
        {
            if (spark && spark.activeSelf) spark.SetActive(false);
            float baseDist = Mathf.Max(_axisDistance, startDistNow);
            _axisDistance = baseDist + delta;

            if (maxDistance > 0f)
            {
                float maxAllowed = _startAxisDistance + maxDistance;
                if (_axisDistance >= maxAllowed)
                {
                    _axisDistance = maxAllowed;
                    if (destroyOnExceed) { Destroy(gameObject); return; }
                    _stopped = true;
                }
            }
        }

        Vector3 finalPos = axisOrigin + axisDir * _axisDistance;
        if (endPoint != null) endPoint.position = finalPos;

        if (_line != null && startPoint && endPoint)
        {
            _line.SetPosition(0, startPoint.position);
            _line.SetPosition(1, endPoint.position);
        }

        RecordHistory(finalPos);
    }

    private Vector3 ComputeDirection()
    {
        switch (directionMode)
        {
            case RayChainSystem.DirectionMode.AxisForward:
                if (directionAxis) return directionAxis.forward;
                if (emitter) return emitter.forward;
                return Vector3.forward;
            case RayChainSystem.DirectionMode.EmitterForward:
                return emitter ? emitter.forward : Vector3.forward;
            case RayChainSystem.DirectionMode.WorldVector:
                return worldDirection.sqrMagnitude > 0f ? worldDirection.normalized : Vector3.forward;
            case RayChainSystem.DirectionMode.TowardsTarget:
                if (emitter && target)
                {
                    Vector3 v = target.position - emitter.position;
                    return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
                }
                if (target)
                {
                    Vector3 v2 = target.position - transform.position;
                    return v2.sqrMagnitude > 0f ? v2.normalized : Vector3.forward;
                }
                return Vector3.forward;
            default:
                return Vector3.forward;
        }
    }

    private void GetAxis(out Vector3 origin, out Vector3 dir)
    {
        if (directionAxis != null)
        {
            origin = directionAxis.position;
            dir = directionAxis.forward.normalized;
        }
        else if (emitter != null)
        {
            origin = emitter.position;
            dir = emitter.forward.normalized;
        }
        else
        {
            origin = transform.position;
            dir = transform.forward.sqrMagnitude > 1e-6f ? transform.forward.normalized : Vector3.forward;
        }
    }

    private float ProjectSignedDistanceOnAxis(Vector3 worldPos, Vector3 axisOrigin, Vector3 axisDir)
    {
        return Vector3.Dot(worldPos - axisOrigin, axisDir);
    }

    private void RecordHistory(Vector3 pos)
    {
        positionHistory.Add(pos);
        if (historyMaxCount > 0 && positionHistory.Count > historyMaxCount) positionHistory.RemoveAt(0);
    }
}
