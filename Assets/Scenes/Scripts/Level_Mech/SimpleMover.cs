using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class SimpleMover : MonoBehaviour
{
    [Header("Basic")]
    public Transform target;
    public Transform startPoint;
    public Transform endPoint;

    [Header("Motion")]
    public bool useLocalSpace = false;
    public bool placeAtStartOnAwake = true;
    public bool followMovingAnchor = true;
    public float duration = 1f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Player Lock (Optional)")]
    [Tooltip("Drag scripts to disable while moving, e.g., PlayerInput/CharacterMotor/etc.")]
    public Behaviour[] playerScriptsToDisable;

    [Header("Camera Shake (Optional)")]
    public bool enableCameraShake = false;
    public Transform cameraToShake;          // if null, falls back to Camera.main
    public float shakeAmplitude = 0.1f;      // units in local space
    public float shakeFrequency = 12f;       // noise speed

    private Coroutine _moveCo;
    private bool _isMoving = false;
    private bool _atEnd = false;
    private Vector3 _extraOffset = Vector3.zero;
    private bool _controlsLockedByThis = false;

    // shake internals
    private Transform _cam;
    private Vector3 _camBaseLocalPos;
    private Coroutine _shakeCo;

    private void Awake()
    {
        if (!target) target = transform;

        // camera cache for shake
        _cam = cameraToShake ? cameraToShake : (Camera.main ? Camera.main.transform : null);
        if (_cam) _camBaseLocalPos = _cam.localPosition;

        if (placeAtStartOnAwake && startPoint)
        {
            if (useLocalSpace)
                target.localPosition = WorldToLocal(startPoint.position, target.parent) + _extraOffset;
            else
                target.position = startPoint.position + _extraOffset;
            _atEnd = false;
        }
    }

    [ContextMenu("Forward")]
    public void Forward()
    {
        if (_isMoving || !endPoint || !startPoint || !target) return;
        StartMove(true);
    }

    [ContextMenu("Backward")]
    public void Backward()
    {
        if (_isMoving || !endPoint || !startPoint || !target) return;
        StartMove(false);
    }

    public void NudgeVertical(float delta = 0.1f)
    {
        _extraOffset += Vector3.up * delta;
        ApplyOffsetIfIdle();
    }

    public void TranslateOffset(Vector3 delta)
    {
        _extraOffset += delta;
        ApplyOffsetIfIdle();
    }

    public void RefreshSnapToCurrentEndpoint()
    {
        ApplyOffsetIfIdle(true);
    }

    private void StartMove(bool toEnd)
    {
        if (_moveCo != null)
        {
            StopCoroutine(_moveCo);
            _moveCo = null;
            UnlockPlayerControls();
            StopShake();
        }
        _moveCo = StartCoroutine(MoveCo(toEnd));
    }

    private IEnumerator MoveCo(bool toEnd)
    {
        _isMoving = true;

        if (AudioManager.Instance) AudioManager.Instance.Play("WallMove_SFX");

        LockPlayerControls();
        StartShake();

        Vector3 p0 = useLocalSpace ? target.localPosition : target.position;
        Transform anchor = toEnd ? endPoint : startPoint;

        Vector3 p1StartWorld = anchor.position;
        Vector3 p1StartLocal = WorldToLocal(p1StartWorld, target.parent);

        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = curve.Evaluate(Mathf.Clamp01(t));

            Vector3 goal;
            if (followMovingAnchor)
            {
                goal = useLocalSpace
                    ? WorldToLocal(anchor.position, target.parent) + _extraOffset
                    : anchor.position + _extraOffset;
            }
            else
            {
                goal = (useLocalSpace ? p1StartLocal : p1StartWorld) + _extraOffset;
            }

            Vector3 pos = Vector3.LerpUnclamped(p0, goal, k);

            if (useLocalSpace) target.localPosition = pos;
            else target.position = pos;

            yield return null;
        }

        if (useLocalSpace) target.localPosition = (followMovingAnchor ? WorldToLocal(anchor.position, target.parent) : p1StartLocal) + _extraOffset;
        else target.position = (followMovingAnchor ? anchor.position : p1StartWorld) + _extraOffset;

        _atEnd = toEnd;
        _isMoving = false;
        _moveCo = null;

        StopShake();
        UnlockPlayerControls();
    }

    private void ApplyOffsetIfIdle(bool force = false)
    {
        if ((!force && _isMoving) || !startPoint || !endPoint || !target) return;
        Transform anchor = _atEnd ? endPoint : startPoint;
        if (useLocalSpace)
        {
            Vector3 baseLocal = WorldToLocal(anchor.position, target.parent);
            target.localPosition = baseLocal + _extraOffset;
        }
        else
        {
            target.position = anchor.position + _extraOffset;
        }
    }

    private static Vector3 WorldToLocal(Vector3 worldPos, Transform localSpaceRoot)
    {
        if (!localSpaceRoot) return worldPos;
        return localSpaceRoot.InverseTransformPoint(worldPos);
    }

    private void LockPlayerControls()
    {
        if (playerScriptsToDisable == null || playerScriptsToDisable.Length == 0) return;
        if (_controlsLockedByThis) return;

        foreach (var b in playerScriptsToDisable)
        {
            if (!b) continue;
            if (b.enabled) b.enabled = false;
        }
        _controlsLockedByThis = true;
    }

    private void UnlockPlayerControls()
    {
        if (!_controlsLockedByThis) return;
        if (playerScriptsToDisable != null)
        {
            foreach (var b in playerScriptsToDisable)
            {
                if (!b) continue;
                b.enabled = true;
            }
        }
        _controlsLockedByThis = false;
    }

    // ---------------- Camera Shake ----------------
    private void StartShake()
    {
        if (!enableCameraShake) return;
        if (_cam == null)
        {
            _cam = cameraToShake ? cameraToShake : (Camera.main ? Camera.main.transform : null);
            if (_cam) _camBaseLocalPos = _cam.localPosition;
        }
        if (_cam == null) return;

        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = StartCoroutine(ShakeCo());
    }

    private void StopShake()
    {
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = null;

        if (_cam != null) _cam.localPosition = _camBaseLocalPos;
    }

    private IEnumerator ShakeCo()
    {
        float t = 0f;
        while (_isMoving && enableCameraShake)
        {
            t += Time.deltaTime * shakeFrequency;
            float ox = (Mathf.PerlinNoise(t, 0.0f) - 0.5f) * 2f * shakeAmplitude;
            float oy = (Mathf.PerlinNoise(0.0f, t + 13.37f) - 0.5f) * 2f * shakeAmplitude;
            if (_cam) _cam.localPosition = _camBaseLocalPos + new Vector3(ox, oy, 0f);
            yield return null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform t = target ? target : transform;
        if (startPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.08f);
        }
        if (endPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.08f);
        }
        if (startPoint && endPoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
        if (t)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(t.position, Vector3.one * 0.1f);
        }
    }
#endif

    private void OnDisable()
    {
        StopShake();
        UnlockPlayerControls();
        if (_moveCo != null) { StopCoroutine(_moveCo); _moveCo = null; }
        _isMoving = false;
    }

    private void OnDestroy()
    {
        StopShake();
        UnlockPlayerControls();
    }
}
