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
    public float duration = 1f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _moveCo;
    private bool _isMoving = false;
    private bool _atEnd = false;
    private Vector3 _extraOffset = Vector3.zero;

    private void Awake()
    {
        if (!target) target = transform;
    }

    [ContextMenu("Forward")]
    public void Forward()
    {
        if (_isMoving || !endPoint || !startPoint) return;
        StartMove(true);
    }

    [ContextMenu("Backward")]
    public void Backward()
    {
        if (_isMoving || !endPoint || !startPoint) return;
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

    private void StartMove(bool toEnd)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveCo(toEnd));
    }

    private IEnumerator MoveCo(bool toEnd)
    {
        _isMoving = true;
        Vector3 p0 = target.position;
        Vector3 p1 = (toEnd ? endPoint.position : startPoint.position) + _extraOffset;
        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = curve.Evaluate(Mathf.Clamp01(t));
            target.position = Vector3.LerpUnclamped(p0, p1, k);
            yield return null;
        }
        target.position = p1;
        _atEnd = toEnd;
        _isMoving = false;
        _moveCo = null;
    }

    private void ApplyOffsetIfIdle()
    {
        if (_isMoving || !startPoint || !endPoint) return;
        target.position = (_atEnd ? endPoint.position : startPoint.position) + _extraOffset;
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
}
