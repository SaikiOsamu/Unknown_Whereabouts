using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetLevelOnOverlap : MonoBehaviour
{
    [SerializeField] float fadeInDuration = 0.6f;
    [SerializeField] float fadeOutDuration = 0.8f;
    [SerializeField] bool useUnscaledTime = true;

    bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        var scene = SceneManager.GetActiveScene();

        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.TransitionToScene(
                scene.name,
                fadeInDuration,
                fadeOutDuration,
                useUnscaledTime,
                null,
                null,
                () => _triggered = false
            );
        }
        else
        {
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
