using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject GameOverMenu;
    [SerializeField] private GameObject TutorialMenu;
    [SerializeField] private GameObject SettingMenu;
    [SerializeField] private GameObject Menu;

    private bool isSettingOpen = false;

    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingMenu();
        }
    }

    private void Start()
    {
        InitializeVolumeSliders();
    }

    private void InitializeVolumeSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.value = AudioManager.Instance.GetChannelVolume(AudioChannel.BGM);
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.value = AudioManager.Instance.GetChannelVolume(AudioChannel.SFX);
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetChannelVolume(AudioChannel.BGM, value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetChannelVolume(AudioChannel.SFX, value);
    }

    public void ToggleSettingMenu()
    {
        isSettingOpen = !isSettingOpen;
        SettingMenu.SetActive(isSettingOpen);

        if (isSettingOpen)
        {
            Time.timeScale = 0f;
            GameOverMenu.SetActive(false);
            TutorialMenu.SetActive(false);
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    public void ShowTutorialMenu()
    {
        TutorialMenu.SetActive(true);
        SettingMenu.SetActive(false);
        GameOverMenu.SetActive(false);
    }

    public void ShowGameOver()
    {
        GameOverMenu.SetActive(true);
        SettingMenu.SetActive(false);
        TutorialMenu.SetActive(false);
    }

    public void HideAll()
    {
        SettingMenu.SetActive(false);
        GameOverMenu.SetActive(false);
        TutorialMenu.SetActive(false);
    }

    public void QuitGame()
    {
        AudioManager.Instance?.Play("Quit_SFX");
        Application.Quit();
    }

    public void ResumeGame()
    {
        SettingMenu.SetActive(false);
        AudioManager.Instance.Play("Resume_SFX");
        Time.timeScale = 1f;
    }
}
