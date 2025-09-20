using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class WallMover : MonoBehaviour
{
    public Transform wall;
    public List<Transform> forwardWaypoints = new List<Transform>();
    public List<float> segmentDurations = new List<float>() { 1.0f };
    public List<AnimationCurve> segmentCurves = new List<AnimationCurve>();
    public bool allowReturn = true;
    public Transform cameraToShake;
    public float shakeAmplitude = 0.1f;
    public float shakeFrequency = 12f;
    public List<MonoBehaviour> playerControllersToDisable;
    public bool disableWholeObject = false;
    public bool drawPathGizmos = true;

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
            foreach (var wp in forwardWaypoints)
                if (wp) nodes.Add(wp.position);
        }
        else
        {
            for (int i = forwardWaypoints.Count - 1; i >= 0; i--)
                if (forwardWaypoints[i]) nodes.Add(forwardWaypoints[i].position);
            nodes.Add(_originalPos);
        }
        AnimationCurve defaultCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        for (int seg = 0; seg < nodes.Count - 1; seg++)
        {
            Vector3 p0 = (seg == 0) ? nodes[0] : nodes[seg];
            Vector3 p1 = nodes[seg + 1];
            float dur = segmentDurations.Count == nodes.Count - 1
                      ? Mathf.Max(0.0001f, segmentDurations[seg])
                      : Mathf.Max(0.0001f, (segmentDurations.Count > 0 ? segmentDurations[0] : 1.0f));
            AnimationCurve curve = segmentCurves.Count == nodes.Count - 1
                      ? (segmentCurves[seg] != null ? segmentCurves[seg] : defaultCurve)
                      : (segmentCurves.Count > 0 && segmentCurves[0] != null ? segmentCurves[0] : defaultCurve);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float k = curve.Evaluate(Mathf.Clamp01(t));
                wall.position = Vector3.LerpUnclamped(p0, p1, k);
                yield return null;
            }
            wall.position = p1;
        }
        _movedForward = forward;
        StopShake();
        SetControllersEnabled(true);
        _isMoving = false;
        _moveCo = null;
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
}
