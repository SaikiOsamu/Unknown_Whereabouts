using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class FadeManager : Singleton<FadeManager>
{
    public Image fadeImage;
    public MeshRenderer fadeRenderer;

    public float defaultInDuration = 0.8f;
    public float defaultOutDuration = 1.2f;
    public bool defaultUnscaledTime = true;

    public AnimationCurve inCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve outCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Material _instancedMat;
    float _currentAlpha;
    Coroutine _running;

    protected override void Awake()
    {
        base.Awake();
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            _currentAlpha = c.a;
            if (fadeImage.raycastTarget == false) fadeImage.raycastTarget = true;
        }
        else if (fadeRenderer != null)
        {
            _instancedMat = fadeRenderer.material;
            var hasBase = _instancedMat.HasProperty("_BaseColor");
            var prop = hasBase ? "_BaseColor" : "_Color";
            var c = _instancedMat.GetColor(prop);
            _currentAlpha = c.a;
        }
    }

    public Coroutine FadeIn(float? duration = null, bool? useUnscaled = null, Action onComplete = null, AnimationCurve curve = null)
    {
        float dur = duration ?? defaultInDuration;
        bool unscaled = useUnscaled ?? defaultUnscaledTime;
        var cv = curve ?? inCurve;
        return PlayFade(1f, 0f, dur, unscaled, onComplete, cv);
    }

    public Coroutine FadeOut(float? duration = null, bool? useUnscaled = null, Action onComplete = null, AnimationCurve curve = null)
    {
        float dur = duration ?? defaultOutDuration;
        bool unscaled = useUnscaled ?? defaultUnscaledTime;
        var cv = curve ?? outCurve;
        return PlayFade(0f, 1f, dur, unscaled, onComplete, cv);
    }

    public void SetFadeColor(Color color)
    {
        if (fadeImage != null)
        {
            color.a = fadeImage.color.a;
            fadeImage.color = color;
        }
        else if (EnsureRendererMat())
        {
            var prop = _instancedMat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            color.a = _instancedMat.GetColor(prop).a;
            _instancedMat.SetColor(prop, color);
        }
    }

    public void SetAlpha(float alpha)
    {
        _currentAlpha = Mathf.Clamp01(alpha);
        ApplyAlpha(_currentAlpha);
    }

    Coroutine PlayFade(float fromAlpha, float toAlpha, float duration, bool useUnscaled, Action onComplete, AnimationCurve curve)
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
            fromAlpha = _currentAlpha;
        }
        _running = StartCoroutine(FadeRoutine(fromAlpha, toAlpha, duration, useUnscaled, curve, onComplete));
        return _running;
    }

    IEnumerator FadeRoutine(float from, float to, float duration, bool unscaled, AnimationCurve curve, Action onComplete)
    {
        if (duration <= 0f)
        {
            _currentAlpha = to;
            ApplyAlpha(_currentAlpha);
            onComplete?.Invoke();
            _running = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / duration;
            float eased = curve.Evaluate(Mathf.Clamp01(t));
            _currentAlpha = Mathf.LerpUnclamped(from, to, eased);
            ApplyAlpha(_currentAlpha);
            yield return null;
        }

        _currentAlpha = to;
        ApplyAlpha(_currentAlpha);
        onComplete?.Invoke();
        _running = null;
    }

    void ApplyAlpha(float a)
    {
        a = Mathf.Clamp01(a);
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = a;
            fadeImage.color = c;
        }
        else if (EnsureRendererMat())
        {
            var hasBase = _instancedMat.HasProperty("_BaseColor");
            var prop = hasBase ? "_BaseColor" : "_Color";
            var c = _instancedMat.GetColor(prop);
            c.a = a;
            _instancedMat.SetColor(prop, c);
        }
    }

    bool EnsureRendererMat()
    {
        if (fadeRenderer == null) return false;
        if (_instancedMat == null) _instancedMat = fadeRenderer.material;
        return _instancedMat != null;
    }
}
