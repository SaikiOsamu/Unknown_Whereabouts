using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { Menu,Level1, Level2, Level3, Paused, GameOver }

    public GameState CurrentState { get; private set; }

    private void Start()
    {
        ChangeState(GameState.Menu);
        Debug.Log("Menu");
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Menu:
            case GameState.Level1:
            case GameState.Level2:
            case GameState.Level3:
                Time.timeScale = 1f;
                UIManager.Instance.HideAll();
                if (newState == GameState.Menu)
                    UIManager.Instance.ShowMainMenu();
                break;

            case GameState.Paused:
            case GameState.GameOver:
                Time.timeScale = 0f;
                UIManager.Instance.ShowSettingMenu();
                break;
        }
    }

    public void RestartGame()
    {
        ChangeState(GameState.Level3);
    }
}