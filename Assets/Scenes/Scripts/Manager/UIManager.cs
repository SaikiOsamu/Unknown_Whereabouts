using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private GameObject GameOverMenu;
    [SerializeField] private GameObject TutorialMenu;
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
    public void ShowTutorialMenu()
    {
        TutorialMenu.SetActive(true);
        SettingMenu.SetActive(false);
        GameOverMenu.SetActive(false);
    }
    public void ShowSettingMenu()
    {
        SettingMenu.SetActive(true);
        GameOverMenu.SetActive(false);
        TutorialMenu.SetActive(false);
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