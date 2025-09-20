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

    private void Awake()
    {
        if (wall == null) wall = transform;
        _originalPos = wall.position;
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
        if (forward && forwardWaypoints.Count == 0) yield break;
        _isMoving = true;
        SetControllersEnabled(false);
        StartShake();

        List<Vector3> nodes = new List<Vector3>();
        nodes.Add(forward ? wall.position : (forwardWaypoints.Count > 0 ? forwardWaypoints[forwardWaypoints.Count - 1].position : wall.position));
        if (forward)
        {
            foreach (var wp in forwardWaypoints) if (wp) nodes.Add(wp.position);
        }
        else
        {
            for (int i = forwardWaypoints.Count - 1; i >= 0; i--) if (forwardWaypoints[i]) nodes.Add(forwardWaypoints[i].position);
            nodes.Add(_originalPos);
        }

        AnimationCurve defaultMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        for (int seg = 0; seg < nodes.Count - 1; seg++)
        {
            if (forward)
            {
                if (seg < forwardWaypoints.Count && applyRotationOnForward && rotateBeforeMoveForward)
                    yield return RotateStepAtIndexForward(seg);
            }
            else
            {
                if (seg < forwardWaypoints.Count && applyRotationOnBackward && rotateBeforeMoveBackward)
                    yield return RotateStepAtIndexBackward(seg);
            }

            Vector3 p0 = nodes[seg];
            Vector3 p1 = nodes[seg + 1];
            float dur = PickByIndex(segmentDurations, seg, nodes.Count - 1, 1.0f);
            dur = Mathf.Max(0.0001f, dur);
            AnimationCurve curve = PickCurveByIndex(segmentCurves, seg, nodes.Count - 1, defaultMoveCurve);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float k = curve.Evaluate(Mathf.Clamp01(t));
                wall.position = Vector3.LerpUnclamped(p0, p1, k);
                yield return null;
            }
            wall.position = p1;

            if (forward)
            {
                if (seg < forwardWaypoints.Count && applyRotationOnForward && !rotateBeforeMoveForward)
                    yield return RotateStepAtIndexForward(seg);
            }
            else
            {
                if (seg < forwardWaypoints.Count && applyRotationOnBackward && !rotateBeforeMoveBackward)
                    yield return RotateStepAtIndexBackward(seg);
            }
        }

        _movedForward = forward;
        StopShake();
        SetControllersEnabled(true);
        _isMoving = false;
        _moveCo = null;
    }

    private IEnumerator RotateStepAtIndexForward(int seg)
    {
        bool doRotate = PickBoolByIndex(rotateAtWaypoint, seg, forwardWaypoints.Count, false);
        if (!doRotate) yield break;
        Vector3 euler = PickByIndex(rotateEulerAtWaypoint, seg, forwardWaypoints.Count, Vector3.zero);
        float dur = Mathf.Max(0.0001f, PickByIndex(rotateDurations, seg, forwardWaypoints.Count, 0.5f));
        AnimationCurve curve = PickCurveByIndex(rotateCurves, seg, forwardWaypoints.Count, AnimationCurve.EaseInOut(0, 0, 1, 1));
        Quaternion startRot = wall.rotation;
        Quaternion endRot = Quaternion.Euler(wall.rotation.eulerAngles + euler);
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

    private IEnumerator RotateStepAtIndexBackward(int seg)
    {
        int iFwd = forwardWaypoints.Count - 1 - seg;
        bool doRotate;
        Vector3 euler;
        float dur;
        AnimationCurve curve;

        if (rotateAtWaypointBackward != null && rotateAtWaypointBackward.Count > 0)
        {
            doRotate = PickBoolByIndex(rotateAtWaypointBackward, seg, forwardWaypoints.Count, false);
            euler = PickByIndex(rotateEulerAtWaypointBackward, seg, forwardWaypoints.Count, Vector3.zero);
            dur = Mathf.Max(0.0001f, PickByIndex(rotateDurationsBackward, seg, forwardWaypoints.Count, 0.5f));
            curve = PickCurveByIndex(rotateCurvesBackward, seg, forwardWaypoints.Count, AnimationCurve.EaseInOut(0, 0, 1, 1));
        }
        else
        {
            doRotate = PickBoolByIndex(rotateAtWaypoint, iFwd, forwardWaypoints.Count, false);
            Vector3 baseEuler = PickByIndex(rotateEulerAtWaypoint, iFwd, forwardWaypoints.Count, Vector3.zero);
            euler = autoInvertRotationOnReturn ? -baseEuler : baseEuler;
            dur = Mathf.Max(0.0001f, PickByIndex(rotateDurations, iFwd, forwardWaypoints.Count, 0.5f));
            curve = PickCurveByIndex(rotateCurves, iFwd, forwardWaypoints.Count, AnimationCurve.EaseInOut(0, 0, 1, 1));
        }

        if (!doRotate) yield break;

        Quaternion startRot = wall.rotation;
        Quaternion endRot = Quaternion.Euler(wall.rotation.eulerAngles + euler);
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

    private static float PickByIndex(List<float> list, int index, int countNeeded, float fallback)
    {
        if (list == null || list.Count == 0) return fallback;
        if (list.Count == countNeeded) return list[Mathf.Clamp(index, 0, list.Count - 1)];
        return list[Mathf.Clamp(0, 0, list.Count - 1)];
    }

    private static Vector3 PickByIndex(List<Vector3> list, int index, int countNeeded, Vector3 fallback)
    {
        if (list == null || list.Count == 0) return fallback;
        if (list.Count == countNeeded) return list[Mathf.Clamp(index, 0, list.Count - 1)];
        return list[Mathf.Clamp(0, 0, list.Count - 1)];
    }

    private static bool PickBoolByIndex(List<bool> list, int index, int countNeeded, bool fallback)
    {
        if (list == null || list.Count == 0) return fallback;
        if (list.Count == countNeeded) return list[Mathf.Clamp(index, 0, list.Count - 1)];
        return list[Mathf.Clamp(0, 0, list.Count - 1)];
    }

    private static AnimationCurve PickCurveByIndex(List<AnimationCurve> list, int index, int countNeeded, AnimationCurve fallback)
    {
        if (fallback == null) fallback = AnimationCurve.EaseInOut(0, 0, 1, 1);
        if (list == null || list.Count == 0) return fallback;
        if (list.Count == countNeeded) return list[Mathf.Clamp(index, 0, list.Count - 1)] ?? fallback;
        return list[Mathf.Clamp(0, 0, list.Count - 1)] ?? fallback;
    }
}
