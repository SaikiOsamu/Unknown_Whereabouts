using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class Level_Pass : MonoBehaviour
{
    public string targetSceneName = "Level_1";
    public GameManager.GameState nextGameState = GameManager.GameState.Level1;

    [Min(0.001f)]
    public float requiredDepth = 1f;
    public float requiredHoldTime = 0.15f;
    public string playerTag = "Player";

    private bool hasPassed = false;
    private Collider triggerCol;
    private float depthHoldTimer = 0f;

    private void Awake()
    {
        triggerCol = GetComponent<Collider>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (hasPassed) return;
        if (!other.CompareTag(playerTag)) return;

        Collider playerCol = other;
        if (playerCol == null) return;

        Vector3 dir;
        float penetrationDist;
        bool overlapped = Physics.ComputePenetration(
            triggerCol, triggerCol.transform.position, triggerCol.transform.rotation,
            playerCol, playerCol.transform.position, playerCol.transform.rotation,
            out dir, out penetrationDist);

        if (overlapped && penetrationDist >= requiredDepth)
        {
            depthHoldTimer += Time.deltaTime;
            if (depthHoldTimer >= requiredHoldTime)
            {
                StartCoroutine(PassRoutine());
            }
        }
        else
        {
            depthHoldTimer = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        depthHoldTimer = 0f;
    }

    private System.Collections.IEnumerator PassRoutine()
    {
        if (hasPassed) yield break;
        hasPassed = true;

        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(nextGameState);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        requiredDepth = Mathf.Max(0.001f, requiredDepth);
        requiredHoldTime = Mathf.Max(0.0f, requiredHoldTime);
    }
#endif
}
