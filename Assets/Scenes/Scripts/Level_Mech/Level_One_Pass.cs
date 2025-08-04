using UnityEngine;
using UnityEngine.SceneManagement;

public class Level_One_Pass : MonoBehaviour
{
    public string targetSceneName = "Level_1";
    private bool isPlayerInTriggerZone = false;

    private void Update()
    {
        if (isPlayerInTriggerZone && Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("Loading scene: " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
            GameManager.Instance.ChangeState(GameManager.GameState.Level2);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTriggerZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTriggerZone = false;
        }
    }
}
