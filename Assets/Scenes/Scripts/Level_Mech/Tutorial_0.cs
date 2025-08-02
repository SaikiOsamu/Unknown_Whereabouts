using UnityEngine;
using UnityEngine.SceneManagement;

public class Tutorial_0 : MonoBehaviour
{
    public string targetSceneName = "Level_1";
    private bool isPlayerInTriggerZone = false;

    private void Update()
    {
        if (isPlayerInTriggerZone && Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("Loading scene: " + targetSceneName);
            SceneManager.LoadScene(targetSceneName);
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
