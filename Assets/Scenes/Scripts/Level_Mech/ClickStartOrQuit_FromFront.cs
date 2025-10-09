using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickStartOrQuit_FromFront : MonoBehaviour
{
    public enum ActionType { StartGame, QuitGame }

    public Camera cam;
    public LayerMask clickableLayers = ~0;
    public float rayMaxDistance = 1000f;

    public bool requireFrontClick = true;
    [Range(0f, 1f)] public float frontDotThreshold = 0.2f;

    public ActionType action = ActionType.StartGame;
    public string sceneToLoad = "Game";
    public bool disableAfterClick = true;

    public bool useFade = true;
    public Color fadeColor = Color.black;
    public float fadeDuration = 0.6f;
    public float waitBeforeLoad = 1.0f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!TryGetComponent<Collider>(out _))
            Debug.LogWarning($"{name} 需要 Collider 才能被点击。");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryHandlePointer(Input.mousePosition);

        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
            TryHandlePointer(Input.touches[0].position);
    }

    void TryHandlePointer(Vector2 screenPos)
    {
        if (!cam) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, clickableLayers, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider || !hit.collider.transform.IsChildOf(transform)) return;
            if (requireFrontClick && !IsFrontHit(ray, hit)) return;

            RunAction();
            if (disableAfterClick) enabled = false;
        }
    }

    bool IsFrontHit(Ray ray, RaycastHit hit)
    {
        float dot = Vector3.Dot(hit.normal, -ray.direction.normalized);
        return dot >= frontDotThreshold;
    }

    void RunAction()
    {
        switch (action)
        {
            case ActionType.StartGame:
                if (!string.IsNullOrEmpty(sceneToLoad))
                    StartCoroutine(StartGameRoutine());
                else
                    Debug.LogWarning("未设置 sceneToLoad，无法开始游戏。");
                break;

            case ActionType.QuitGame:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    IEnumerator StartGameRoutine()
    {
#if UNITY_EDITOR
        UnityEditor.Selection.activeObject = null;
#endif
        if (waitBeforeLoad > 0f)
            yield return new WaitForSeconds(waitBeforeLoad);

        if (useFade && FadeManager.Instance != null)
        {
            FadeManager.Instance.SetFadeColor(fadeColor);
            bool done = false;
            FadeManager.Instance.TransitionToScene(
                sceneToLoad,
                fadeInDur: fadeDuration,
                fadeOutDur: fadeDuration,
                unscaled: true,
                inCv: null,
                outCv: null,
                onComplete: () => { done = true; }
            );
            while (!done) yield return null;
        }
        else
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (TryGetComponent<Collider>(out var col))
        {
            Gizmos.color = Color.cyan;
            Gizmos.matrix = col.transform.localToWorldMatrix;
            if (col is BoxCollider bc)
                Gizmos.DrawWireCube(bc.center, bc.size);
        }
    }
#endif
}
