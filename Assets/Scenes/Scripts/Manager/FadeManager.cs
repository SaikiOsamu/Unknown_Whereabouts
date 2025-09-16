using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class FadeManager : Singleton<FadeManager>
{
    [Serializable]
    public struct LevelSubtitle
    {
        public string sceneName;
        [TextArea] public string subtitle;
    }

    public Image fadeImage;
    public MeshRenderer fadeRenderer;

    public float defaultInDuration = 0.8f;
    public float defaultOutDuration = 1.2f;
    public bool defaultUnscaledTime = true;
    public AnimationCurve inCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve outCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public CanvasGroup subtitleGroup;
    public TMP_Text subtitleTMP;
    public Text subtitleUI;
    public List<LevelSubtitle> subtitles = new List<LevelSubtitle>();
    [TextArea] public string defaultSubtitle = "";
    public float subtitleInDuration = 0.6f;
    public float subtitleHoldDuration = 2.0f;
    public float subtitleOutDuration = 0.6f;
    public bool subtitleUnscaledTime = true;
    public AnimationCurve subtitleInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve subtitleOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool fadeOutAfterSubtitle = true;

    public bool loadThenActivateAfterFadeIn = false;
    public bool autoIntroOnSceneLoaded = true;

    Material _instancedMat;
    float _currentAlpha;
    Coroutine _fadeRoutine;
    Coroutine _transitionRoutine;
    Dictionary<string, string> _subtitleMap;
    bool _managedLoadInProgress;

    const string BASE_COLOR = "_BaseColor";
    const string COLOR = "_Color";

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            var c = fadeImage.color;
            _currentAlpha = c.a;
            if (!fadeImage.raycastTarget) fadeImage.raycastTarget = true;
        }
        else if (fadeRenderer != null)
        {
            _instancedMat = fadeRenderer.material;
            var prop = _instancedMat.HasProperty(BASE_COLOR) ? BASE_COLOR : COLOR;
            var c = _instancedMat.GetColor(prop);
            _currentAlpha = c.a;
        }

        if (subtitleGroup != null)
        {
            subtitleGroup.alpha = 0f;
            subtitleGroup.blocksRaycasts = false;
            subtitleGroup.interactable = false;
        }

        BuildSubtitleMap();
        if (!subtitleUnscaledTime) subtitleUnscaledTime = defaultUnscaledTime;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void BuildSubtitleMap()
    {
        _subtitleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < subtitles.Count; i++)
        {
            var s = subtitles[i];
            if (!string.IsNullOrEmpty(s.sceneName))
                _subtitleMap[s.sceneName] = s.subtitle ?? "";
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_managedLoadInProgress) return;
        if (!autoIntroOnSceneLoaded) return;
        StartCoroutine(AutoIntroSequence());
    }

    IEnumerator AutoIntroSequence()
    {
        yield return PlaySubtitleIfAny();
        if (fadeOutAfterSubtitle)
            yield return FadeRoutine(_currentAlpha, 0f, defaultOutDuration, defaultUnscaledTime, outCurve, null);
    }

    public Coroutine TransitionToScene(
        string sceneName,
        float? fadeInDur = null,
        float? fadeOutDur = null,
        bool? unscaled = null,
        AnimationCurve inCv = null,
        AnimationCurve outCv = null,
        Action onComplete = null)
    {
        CancelAll();
        _transitionRoutine = StartCoroutine(SceneTransitionSequence(
            sceneName,
            fadeInDur ?? defaultInDuration,
            fadeOutDur ?? defaultOutDuration,
            unscaled ?? defaultUnscaledTime,
            inCv ?? inCurve,
            outCv ?? outCurve,
            onComplete
        ));
        return _transitionRoutine;
    }

    public Coroutine FadeIn(float? duration = null, bool? useUnscaled = null, AnimationCurve curve = null, Action onComplete = null)
        => PlayFade(_currentAlpha, 1f, duration ?? defaultInDuration, useUnscaled ?? defaultUnscaledTime, curve ?? inCurve, onComplete);

    public Coroutine FadeOut(float? duration = null, bool? useUnscaled = null, AnimationCurve curve = null, Action onComplete = null)
        => PlayFade(_currentAlpha, 0f, duration ?? defaultOutDuration, useUnscaled ?? defaultUnscaledTime, curve ?? outCurve, onComplete);

    public void SetFadeColor(Color color)
    {
        if (fadeImage != null)
        {
            color.a = fadeImage.color.a;
            fadeImage.color = color;
        }
        else if (EnsureRendererMat())
        {
            var prop = _instancedMat.HasProperty(BASE_COLOR) ? BASE_COLOR : COLOR;
            color.a = _instancedMat.GetColor(prop).a;
            _instancedMat.SetColor(prop, color);
        }
    }

    public void SetAlpha(float alpha)
    {
        _currentAlpha = Mathf.Clamp01(alpha);
        ApplyAlpha(_currentAlpha);
    }

    IEnumerator SceneTransitionSequence(
        string targetScene,
        float inDur, float outDur, bool unscaled, AnimationCurve inCv, AnimationCurve outCv,
        Action onComplete)
    {
        _managedLoadInProgress = true;

        yield return FadeRoutine(_currentAlpha, 1f, inDur, unscaled, inCv, null);

        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        if (loadThenActivateAfterFadeIn)
        {
            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
                yield return null;
            op.allowSceneActivation = true;
        }
        while (!op.isDone)
            yield return null;

        yield return PlaySubtitleIfAny();

        if (fadeOutAfterSubtitle)
            yield return FadeRoutine(1f, 0f, outDur, unscaled, outCv, null);

        onComplete?.Invoke();
        _managedLoadInProgress = false;
        _transitionRoutine = null;
    }

    IEnumerator PlaySubtitleIfAny()
    {
        if (subtitleGroup == null) yield break;

        string scene = SceneManager.GetActiveScene().name ?? "";
        if (!_subtitleMap.TryGetValue(scene, out var text))
            text = defaultSubtitle;

        if (string.IsNullOrEmpty(text)) yield break;

        if (subtitleTMP != null) subtitleTMP.text = text;
        else if (subtitleUI != null) subtitleUI.text = text;

        yield return SubtitleFade(0f, 1f, subtitleInDuration, subtitleUnscaledTime, subtitleInCurve);

        float timer = 0f;
        while (timer < subtitleHoldDuration)
        {
            float dt = subtitleUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += dt;
            yield return null;
        }

        yield return SubtitleFade(1f, 0f, subtitleOutDuration, subtitleUnscaledTime, subtitleOutCurve);
    }

    IEnumerator SubtitleFade(float from, float to, float duration, bool unscaled, AnimationCurve cv)
    {
        if (subtitleGroup == null || duration <= 0f)
        {
            if (subtitleGroup != null) subtitleGroup.alpha = to;
            yield break;
        }
        float t = 0f;
        while (t < 1f)
        {
            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / Mathf.Max(0.0001f, duration);
            float eased = cv.Evaluate(Mathf.Clamp01(t));
            subtitleGroup.alpha = Mathf.LerpUnclamped(from, to, eased);
            yield return null;
        }
        subtitleGroup.alpha = to;
    }

    Coroutine PlayFade(float from, float to, float duration, bool unscaled, AnimationCurve curve, Action onComplete)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(from, to, duration, unscaled, curve, onComplete));
        return _fadeRoutine;
    }

    IEnumerator FadeRoutine(float from, float to, float duration, bool unscaled, AnimationCurve curve, Action onComplete)
    {
        if (duration <= 0f)
        {
            _currentAlpha = to;
            ApplyAlpha(_currentAlpha);
            onComplete?.Invoke();
            _fadeRoutine = null;
            yield break;
        }

        float t = 0f;
        float dur = Mathf.Max(0.0001f, duration);
        while (t < 1f)
        {
            float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / dur;
            float eased = curve.Evaluate(Mathf.Clamp01(t));
            _currentAlpha = Mathf.LerpUnclamped(from, to, eased);
            ApplyAlpha(_currentAlpha);
            yield return null;
        }
        _currentAlpha = to;
        ApplyAlpha(_currentAlpha);
        onComplete?.Invoke();
        _fadeRoutine = null;
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
            var prop = _instancedMat.HasProperty(BASE_COLOR) ? BASE_COLOR : COLOR;
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

    void CancelAll()
    {
        if (_transitionRoutine != null) { StopCoroutine(_transitionRoutine); _transitionRoutine = null; }
        if (_fadeRoutine != null) { StopCoroutine(_fadeRoutine); _fadeRoutine = null; }
    }

    public Coroutine PlayLevelIntro(bool? doFadeOutAfterSubtitle = null, Action onComplete = null)
    {
        var finalFadeOut = doFadeOutAfterSubtitle ?? fadeOutAfterSubtitle;
        return StartCoroutine(PlayLevelIntroRoutine(finalFadeOut, onComplete));
    }

    IEnumerator PlayLevelIntroRoutine(bool finalFadeOut, Action onComplete)
    {
        yield return PlaySubtitleIfAny();
        if (finalFadeOut)
            yield return FadeRoutine(_currentAlpha, 0f, defaultOutDuration, defaultUnscaledTime, outCurve, null);
        onComplete?.Invoke();
    }

    public Coroutine FadeIn(float? duration = null, bool? useUnscaled = null, Action onComplete = null, AnimationCurve curve = null)
        => FadeIn(duration, useUnscaled, curve, onComplete);

    public Coroutine FadeOut(float? duration = null, bool? useUnscaled = null, Action onComplete = null, AnimationCurve curve = null)
        => FadeOut(duration, useUnscaled, curve, onComplete);
}
