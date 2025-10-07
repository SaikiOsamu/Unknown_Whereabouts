using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public struct TopSubtitleClip
{
    [TextArea] public string text;
    public float inDur;
    public float hold;
    public float outDur;
    public AnimationCurve inCurve;
    public AnimationCurve outCurve;

    public static TopSubtitleClip Make(string t, float i, float h, float o, AnimationCurve ic = null, AnimationCurve oc = null)
    {
        return new TopSubtitleClip { text = t, inDur = i, hold = h, outDur = o, inCurve = ic, outCurve = oc };
    }
}

public class Level6EndingDirector : MonoBehaviour
{
    public EndingUI endingUI;

    public List<TopSubtitleClip> topSubtitles = new List<TopSubtitleClip>()
    {
        TopSubtitleClip.Make("看这无人海", 0.6f, 1.2f, 0.5f),
        TopSubtitleClip.Make("只有海之间，波浪来自何处", 0.6f, 1.2f, 0.6f)
    };

    public string gameTitle = "UNKNOWN WHEREABOUTS";
    public float titleDelay = 0.3f;
    public float titleIn = 0.8f;
    public float titleHold = 1.6f;
    public float titleOut = 0f;

    public string dedication = "献给 —— 海的来客";
    public float dedicationIn = 0.8f;

    public bool zoomIn = true;
    public float moveDuration = 6f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Vector3 worldMoveOffset = new Vector3(0f, 0f, -2f);
    public float startFOV = 55f;
    public float endFOV = 45f;
    public Camera mainCam;

    public float blackHold = 1.2f;
    public float fadeFromBlack = 1.0f;
    public bool disableFadeManagerAutoIntro = true;

    void Awake()
    {
        if (!mainCam) mainCam = Camera.main;
        if (disableFadeManagerAutoIntro && FadeManager.Instance != null) FadeManager.Instance.autoIntroOnSceneLoaded = false;
        if (endingUI == null) endingUI = FindObjectOfType<EndingUI>(true);
    }

    IEnumerator Start()
    {
        if (endingUI == null) yield break;
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.SetFadeColor(Color.black);
            FadeManager.Instance.SetAlpha(1f);
        }
        if (blackHold > 0) yield return new WaitForSecondsRealtime(blackHold);
        if (FadeManager.Instance != null) yield return FadeManager.Instance.FadeOut(fadeFromBlack, true, (AnimationCurve)null, null);
        yield return StartCoroutine(PlayCameraMove());
        if (topSubtitles != null && topSubtitles.Count > 0) yield return endingUI.PlayTopSubtitles(topSubtitles, true);
        if (titleDelay > 0) yield return new WaitForSecondsRealtime(titleDelay);
        yield return endingUI.PlayTitle(gameTitle, titleIn, titleHold, titleOut, true);
        if (!string.IsNullOrEmpty(dedication)) yield return endingUI.ShowDedication(dedication, dedicationIn, true);
    }

    IEnumerator PlayCameraMove()
    {
        if (!mainCam) yield break;
        float dur = Mathf.Max(0.0001f, moveDuration);
        float t = 0f;
        Vector3 pos0 = mainCam.transform.position;
        float fov0 = startFOV > 0 ? startFOV : mainCam.fieldOfView;
        float fov1 = zoomIn ? endFOV : startFOV;
        if (startFOV > 0) mainCam.fieldOfView = startFOV;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float e = moveCurve.Evaluate(Mathf.Clamp01(t));
            mainCam.transform.position = Vector3.Lerp(pos0, pos0 + (zoomIn ? -worldMoveOffset : worldMoveOffset), e);
            mainCam.fieldOfView = Mathf.Lerp(fov0, fov1, e);
            yield return null;
        }
    }
}
