using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStartFader : MonoBehaviour
{
    public float fadeInDuration = 1.0f;
    public bool useUnscaledTime = true;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FadeManager.Instance == null) return;
        FadeManager.Instance.SetFadeColor(Color.black);
        FadeManager.Instance.SetAlpha(1f);
        FadeManager.Instance.FadeIn(fadeInDuration, useUnscaledTime);
    }
}
