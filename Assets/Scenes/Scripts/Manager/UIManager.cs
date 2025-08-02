using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject PauseMenu;
    [SerializeField] private GameObject GameOverMenu;
    [SerializeField] private GameObject MainMenu;
    [SerializeField] private GameObject SettingMenu;
    [SerializeField] private GameObject Menu;

    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    protected override void Awake()
    {
        base.Awake(); 

        DontDestroyOnLoad(Menu.transform.root.gameObject);
    
    }

    private void Start()
    {
        InitializeVolumeSliders();
    }

    private void InitializeVolumeSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.value = AudioManager.Instance.BGMVolume;
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance.SFXVolume;
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }
    public void ShowMainMenu()
    {
        MainMenu.SetActive(true);
        PauseMenu.SetActive(false);
        GameOverMenu.SetActive(false);
    }
    public void ShowSettingMenu()
    {
        SettingMenu.SetActive(true);
    }
    public void ShowPauseMenu()
    {
        PauseMenu.SetActive(true);
        GameOverMenu.SetActive(false);
        MainMenu.SetActive(false);
    }

    public void ShowGameOver()
    {
        GameOverMenu.SetActive(true);
        PauseMenu.SetActive(false);
        MainMenu.SetActive(false);
    }

    public void HideAll()
    {
        PauseMenu.SetActive(false);
        GameOverMenu.SetActive(false);
        MainMenu.SetActive(false);
    }

    public void OnStartButtonClick()
    {
        SceneManager.LoadScene("DB_TestScene");
        GameManager.Instance.ChangeState(GameManager.GameState.Level1);
    }


    public void OnResumeButtonClick()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.Level1& GameManager.GameState.Level2 & GameManager.GameState.Level3);
        Debug.Log("Resume");
    }

    public void OnSettingButtonClick()
    {
        ShowSettingMenu();
        Time.timeScale = 0f;
    }
    public void OnSettingButtonClick_Closed()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Level1 ||
            GameManager.Instance.CurrentState == GameManager.GameState.Level2 ||
            GameManager.Instance.CurrentState == GameManager.GameState.Level3 ||
            GameManager.Instance.CurrentState == GameManager.GameState.Menu)
        {
            Time.timeScale = 1f;
        }
        SettingMenu.SetActive(false);

    }
    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = value;
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SFXVolume = value;
    }
}