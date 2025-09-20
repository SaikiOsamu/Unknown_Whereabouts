using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class WallMover : MonoBehaviour
{
    [Header("Target & Path")]
    public Transform wall;
    public List<Transform> forwardWaypoints = new List<Transform>();

    [Header("Move Segments (between nodes)")]
    public List<float> segmentDurations = new List<float>() { 1.0f };
    public List<AnimationCurve> segmentCurves = new List<AnimationCurve>();

    [Header("Return Settings")]
    public bool allowReturn = true;

    [Header("Camera Shake")]
    public Transform cameraToShake;
    public float shakeAmplitude = 0.1f;
    public float shakeFrequency = 12f;

    [Header("Disable Controllers While Moving")]
    public List<MonoBehaviour> playerControllersToDisable;
    public bool disableWholeObject = false;

    [Header("Gizmos")]
    public bool drawPathGizmos = true;

    [Header("Rotation At Waypoints")]
    public bool applyRotationOnForward = true;
    public bool applyRotationOnBackward = true;

    public bool rotationIsDelta = true;
    public bool autoMirrorRotateTimingOnReturn = true;
    public bool autoInvertRotationOnReturn = true;

    public bool rotateBeforeMoveForward = false;
    public bool rotateBeforeMoveBackward = false;

    public List<bool> rotateAtWaypoint = new List<bool>();
    public List<Vector3> rotateEulerAtWaypoint = new List<Vector3>();
    public List<float> rotateDurations = new List<float>() { 0.5f };
    public List<AnimationCurve> rotateCurves = new List<AnimationCurve>();

    public List<bool> rotateAtWaypointBackward = new List<bool>();
    public List<Vector3> rotateEulerAtWaypointBackward = new List<Vector3>();
    public List<float> rotateDurationsBackward = new List<float>();
    public List<AnimationCurve> rotateCurvesBackward = new List<AnimationCurve>();

    private Vector3 _camBaseLocalPos;
    private Transform _cam;
    private Coroutine _moveCo;
    private Coroutine _shakeCo;
    private bool _isMoving = false;
    private bool _movedForward = false;
    private Vector3 _originalPos;
    private Quaternion _baseRot;

    private void Awake()
    {
        if (wall == null) wall = transform;
        _originalPos = wall.position;
        _baseRot = wall.rotation;
        _cam = cameraToShake ? cameraToShake : (Camera.main ? Camera.main.transform : null);
        if (_cam) _camBaseLocalPos = _cam.localPosition;
    }

    public void StartMoveForward()
    {
        if (_isMoving || _movedForward) return;
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveAlongPathCo(true));
    }

    public void StartMoveBackward()
    {
        if (!_isMoving && _movedForward && allowReturn)
        {
            if (_moveCo != null) StopCoroutine(_moveCo);
            _moveCo = StartCoroutine(MoveAlongPathCo(false));
        }
    }

    private IEnumerator MoveAlongPathCo(bool forward)
    {
        int total = forwardWaypoints.Count;
        int validCount = 0;
        for (int i = 0; i < total; i++) if (forwardWaypoints[i]) validCount++;

        if (forward && validCount == 0) yield break;

        _isMoving = true;
        SetControllersEnabled(false);
        StartShake();

        List<Vector3> nodes = new List<Vector3>();
        List<int> nodeWpIdx = new List<int>(); // 每个节点对应的原始 waypoint 索引；起点/原点为 -1
        List<int> segToWpIdx = new List<int>(); // 每段运动的“目的地 waypoint 索引”；原点为 -1

        if (forward)
        {
            nodes.Add(wall.position); nodeWpIdx.Add(-1);
            for (int i = 0; i < total; i++)
            {
                var wp = forwardWaypoints[i];
                if (!wp) continue;
                nodes.Add(wp.position);
                nodeWpIdx.Add(i);
            }
        }
        else
        {
            int lastValid = -1;
            for (int i = total - 1; i >= 0; i--)
            {
                if (forwardWaypoints[i]) { lastValid = i; break; }
            }

            if (lastValid >= 0)
            {
                nodes.Add(forwardWaypoints[lastValid].position);
                nodeWpIdx.Add(lastValid);
                for (int i = lastValid - 1; i >= 0; i--)
                {
                    var wp = forwardWaypoints[i];
                    if (!wp) continue;
                    nodes.Add(wp.position);
                    nodeWpIdx.Add(i);
                }
            }
            else
            {
                nodes.Add(wall.position);
                nodeWpIdx.Add(-1);
            }
            nodes.Add(_originalPos);
            nodeWpIdx.Add(-1);
        }

        for (int s = 0; s < nodes.Count - 1; s++)
            segToWpIdx.Add(nodeWpIdx[s + 1]);

        bool rotateBeforeThisDir = forward
            ? rotateBeforeMoveForward
            : (autoMirrorRotateTimingOnReturn ? !rotateBeforeMoveForward : rotateBeforeMoveBackward);

        AnimationCurve defaultMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        for (int seg = 0; seg < nodes.Count - 1; seg++)
        {
            int wpIdxDest = segToWpIdx[seg];

            if (rotateBeforeThisDir)
            {
                if (forward && applyRotationOnForward && wpIdxDest >= 0)
                    yield return RotateAtWaypointForward(wpIdxDest);
                if (!forward && applyRotationOnBackward && wpIdxDest >= 0)
                    yield return RotateAtWaypointBackward(wpIdxDest);
            }

            Vector3 p0 = nodes[seg];
            Vector3 p1 = nodes[seg + 1];

            float dur = PickFloat_Broadcast(segmentDurations, seg, 1.0f);
            dur = Mathf.Max(0.0001f, dur);
            AnimationCurve curve = PickCurve_Broadcast(segmentCurves, seg, defaultMoveCurve);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float k = curve.Evaluate(Mathf.Clamp01(t));
                wall.position = Vector3.LerpUnclamped(p0, p1, k);
                yield return null;
            }
            wall.position = p1;

            if (!rotateBeforeThisDir)
            {
                if (forward && applyRotationOnForward && wpIdxDest >= 0)
                    yield return RotateAtWaypointForward(wpIdxDest);
                if (!forward && applyRotationOnBackward && wpIdxDest >= 0)
                    yield return RotateAtWaypointBackward(wpIdxDest);
            }
        }

        _movedForward = forward;
        StopShake();
        SetControllersEnabled(true);
        _isMoving = false;
        _moveCo = null;
    }

    private IEnumerator RotateAtWaypointForward(int wpIdx)
    {
        bool doRotate = PickBool_Strict(rotateAtWaypoint, wpIdx, false);
        if (!doRotate) yield break;

        Vector3 euler = PickVector_Strict(rotateEulerAtWaypoint, wpIdx, Vector3.zero);
        float dur = Mathf.Max(0.0001f, PickFloat_Strict(rotateDurations, wpIdx, 0.5f));
        AnimationCurve curve = PickCurve_Strict(rotateCurves, wpIdx, AnimationCurve.EaseInOut(0, 0, 1, 1));

        Quaternion startRot = wall.rotation;
        Quaternion endRot = rotationIsDelta
            ? startRot * Quaternion.Euler(euler)
            : _baseRot * Quaternion.Euler(euler);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = curve.Evaluate(Mathf.Clamp01(t));
            wall.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        wall.rotation = endRot;
    }

    private IEnumerator RotateAtWaypointBackward(int wpIdx)
    {
        bool hasBackwardTables = (rotateAtWaypointBackward != null && rotateAtWaypointBackward.Count > 0);
        bool doRotate = hasBackwardTables
            ? PickBool_Strict(rotateAtWaypointBackward, wpIdx, false)
            : PickBool_Strict(rotateAtWaypoint, wpIdx, false);
        if (!doRotate) yield break;

        Vector3 baseEuler = hasBackwardTables
            ? PickVector_Strict(rotateEulerAtWaypointBackward, wpIdx, Vector3.zero)
            : PickVector_Strict(rotateEulerAtWaypoint, wpIdx, Vector3.zero);

        Vector3 euler = rotationIsDelta && !hasBackwardTables && autoInvertRotationOnReturn
            ? -baseEuler
            : baseEuler;

        float dur = Mathf.Max(0.0001f, hasBackwardTables
            ? PickFloat_Strict(rotateDurationsBackward, wpIdx, 0.5f)
            : PickFloat_Strict(rotateDurations, wpIdx, 0.5f));

        AnimationCurve curve = hasBackwardTables
            ? PickCurve_Strict(rotateCurvesBackward, wpIdx, AnimationCurve.EaseInOut(0, 0, 1, 1))
            : PickCurve_Strict(rotateCurves, wpIdx, AnimationCurve.EaseInOut(0, 0, 1, 1));

        Quaternion startRot = wall.rotation;
        Quaternion endRot = rotationIsDelta
            ? startRot * Quaternion.Euler(euler)
            : _baseRot * Quaternion.Euler(euler);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = curve.Evaluate(Mathf.Clamp01(t));
            wall.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        wall.rotation = endRot;
    }

    private void StartShake()
    {
        if (_cam == null) return;
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = StartCoroutine(ShakeCo());
    }

    private void StopShake()
    {
        if (_cam != null) _cam.localPosition = _camBaseLocalPos;
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = null;
    }

    private IEnumerator ShakeCo()
    {
        float t = 0f;
        while (_isMoving)
        {
            t += Time.deltaTime * shakeFrequency;
            float ox = (Mathf.PerlinNoise(t, 0.0f) - 0.5f) * 2f * shakeAmplitude;
            float oy = (Mathf.PerlinNoise(0.0f, t + 13.37f) - 0.5f) * 2f * shakeAmplitude;
            if (_cam) _cam.localPosition = _camBaseLocalPos + new Vector3(ox, oy, 0f);
            yield return null;
        }
    }

    private void SetControllersEnabled(bool enabled)
    {
        if (playerControllersToDisable == null) return;
        foreach (var c in playerControllersToDisable)
        {
            if (c == null) continue;
            if (disableWholeObject) c.gameObject.SetActive(enabled);
            else c.enabled = enabled;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawPathGizmos) return;
        var w = wall ? wall : transform;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube((Application.isPlaying ? _originalPos : w.position), Vector3.one * 0.2f);
        if (forwardWaypoints != null && forwardWaypoints.Count > 0)
        {
            Gizmos.color = Color.green;
            Vector3 prev = w.position;
            foreach (var wp in forwardWaypoints)
            {
                if (!wp) continue;
                Gizmos.DrawLine(prev, wp.position);
                Gizmos.DrawWireCube(wp.position, Vector3.one * 0.2f);
                prev = wp.position;
            }
        }
    }
#endif

    // ---- Pick helpers ----
    // 段用：允许广播（列表只有 1 个元素时对所有段生效）
    private static float PickFloat_Broadcast(List<float> list, int segIndex, float fallback)
    {
        if (list == null || list.Count == 0) return fallback;
        if (list.Count == 1) return list[0];
        segIndex = Mathf.Clamp(segIndex, 0, list.Count - 1);
        return list[segIndex];
    }

    private static AnimationCurve PickCurve_Broadcast(List<AnimationCurve> list, int segIndex, AnimationCurve fallback)
    {
        if (fallback == null) fallback = AnimationCurve.EaseInOut(0, 0, 1, 1);
        if (list == null || list.Count == 0) return fallback;
        if (list.Count == 1) return list[0] ?? fallback;
        segIndex = Mathf.Clamp(segIndex, 0, list.Count - 1);
        return list[segIndex] ?? fallback;
    }

    // 旋转用：严格索引（缺/越界直接 fallback），绝不广播或夹到 0
    private static bool PickBool_Strict(List<bool> list, int index, bool fallback)
    {
        if (list == null || index < 0 || index >= list.Count) return fallback;
        return list[index];
    }

    private static Vector3 PickVector_Strict(List<Vector3> list, int index, Vector3 fallback)
    {
        if (list == null || index < 0 || index >= list.Count) return fallback;
        return list[index];
    }

    private static float PickFloat_Strict(List<float> list, int index, float fallback)
    {
        if (list == null || index < 0 || index >= list.Count) return fallback;
        return list[index];
    }

    private static AnimationCurve PickCurve_Strict(List<AnimationCurve> list, int index, AnimationCurve fallback)
    {
        if (fallback == null) fallback = AnimationCurve.EaseInOut(0, 0, 1, 1);
        if (list == null || index < 0 || index >= list.Count) return fallback;
        return list[index] ?? fallback;
    }
}
