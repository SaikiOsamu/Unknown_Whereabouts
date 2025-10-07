using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EndingUI : MonoBehaviour
{
    public CanvasGroup topGroup;
    public TMP_Text topText;
    public CanvasGroup titleGroup;
    public TMP_Text titleText;
    public CanvasGroup dedicateGroup;
    public TMP_Text dedicateText;

    void Awake()
    {
        SetGroup(topGroup, 0f);
        SetGroup(titleGroup, 0f);
        SetGroup(dedicateGroup, 0f);
        if (topGroup) topGroup.blocksRaycasts = false;
        if (titleGroup) titleGroup.blocksRaycasts = false;
        if (dedicateGroup) dedicateGroup.blocksRaycasts = false;
    }

    public IEnumerator PlayTopSubtitles(IList<TopSubtitleClip> clips, bool unscaled)
    {
        if (clips == null || clips.Count == 0 || topGroup == null || topText == null) yield break;
        for (int i = 0; i < clips.Count; i++)
        {
            var c = clips[i];
            if (string.IsNullOrEmpty(c.text)) continue;
            topText.text = c.text;
            yield return Fade(topGroup, 0f, 1f, c.inDur, unscaled, c.inCurve);
            if (c.hold > 0f) yield return Hold(c.hold, unscaled);
            yield return Fade(topGroup, 1f, 0f, c.outDur, unscaled, c.outCurve);
        }
    }

    public IEnumerator PlayTitle(string text, float inDur, float hold, float outDur, bool unscaled)
    {
        if (!titleGroup || !titleText || string.IsNullOrEmpty(text)) yield break;
        titleText.text = text;
        yield return Fade(titleGroup, 0f, 1f, inDur, unscaled, null);
        if (hold > 0) yield return Hold(hold, unscaled);
        if (outDur > 0) yield return Fade(titleGroup, 1f, 0f, outDur, unscaled, null);
    }

    public IEnumerator ShowDedication(string text, float inDur, bool unscaled)
    {
        if (!dedicateGroup || !dedicateText || string.IsNullOrEmpty(text)) yield break;
        dedicateText.text = text;
        yield return Fade(dedicateGroup, 0f, 1f, inDur, unscaled, null);
    }

    static void SetGroup(CanvasGroup g, float a)
    {
        if (!g) return;
        g.alpha = a;
        g.interactable = false;
        g.blocksRaycasts = false;
    }

    IEnumerator Fade(CanvasGroup g, float from, float to, float dur, bool unscaled, AnimationCurve curve)
    {
        if (!g) yield break;
        dur = Mathf.Max(0.0001f, dur);
        float t = 0f;
        while (t < 1f)
        {
            t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            float e = curve != null ? curve.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);
            g.alpha = Mathf.LerpUnclamped(from, to, e);
            yield return null;
        }
        g.alpha = to;
        g.blocksRaycasts = g.interactable = (to > 0.001f);
    }

    static IEnumerator Hold(float s, bool unscaled)
    {
        float t = 0f;
        while (t < s)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }
}
