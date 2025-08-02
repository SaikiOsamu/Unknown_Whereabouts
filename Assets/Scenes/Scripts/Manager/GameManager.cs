using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { Tutorial,Level1, Level2, Level3, Setting, GameOver }

    public GameState CurrentState { get; private set; }

    private void Start()
    {
        ChangeState(GameState.Tutorial);
        Debug.Log("Menu");
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Tutorial:
            case GameState.Level1:
            case GameState.Level2:
            case GameState.Level3:
                Time.timeScale = 1f;
                UIManager.Instance.HideAll();
                if (newState == GameState.Tutorial)
                    UIManager.Instance.ShowTutorialMenu();
                break;

            case GameState.Setting:
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }
    }

    public void RestartGame()
    {
        ChangeState(GameState.Level3);
    }
}