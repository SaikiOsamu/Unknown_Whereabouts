using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { Menu,Setting, Level1, Level2, Level3, Paused, GameOver }

    public GameState CurrentState { get; private set; }

    private void Start()
    {
        ChangeState(GameState.Menu);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        switch (newState)
        {
            case GameState.Menu:
                Time.timeScale = 1f;
                UIManager.Instance.ShowMainMenu();
                break;
            case GameState.Level1:
                Time.timeScale = 1f;
                UIManager.Instance.HideAll();
                break;
            case GameState.Level2:
                Time.timeScale = 1f;
                UIManager.Instance.HideAll();
                break;
            case GameState.Level3:
                Time.timeScale = 1f;
                UIManager.Instance.HideAll();
                break;
            case GameState.Setting:
                Time.timeScale = 0f;
                UIManager.Instance.ShowSettingMenu();
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                UIManager.Instance.ShowPauseMenu();
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                UIManager.Instance.ShowGameOver();
                break;
        }
    }
 
    public void RestartGame()
    {
        ChangeState(GameState.Level3);
    }
}