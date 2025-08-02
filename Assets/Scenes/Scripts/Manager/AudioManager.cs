using UnityEngine;
using System.Linq;
public class AudioManager : Singleton<AudioManager>
{
    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField][Range(0, 1)] private float defaultBGMVolume = 0.75f;

    [Header("SFX")]
    [SerializeField] private AudioSource[] sfxSources;
    [SerializeField][Range(0, 1)] private float defaultSFXVolume = 0.75f;

    public float BGMVolume
    {
        get => PlayerPrefs.GetFloat("BGMVolume", defaultBGMVolume);
        set
        {
            bgmSource.volume = value;
            PlayerPrefs.SetFloat("BGMVolume", value);
        }
    }

    public float SFXVolume
    {
        get => PlayerPrefs.GetFloat("SFXVolume", defaultSFXVolume);
        set
        {
            foreach (var source in sfxSources)
                source.volume = value;
            PlayerPrefs.SetFloat("SFXVolume", value);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        bgmSource.volume = BGMVolume;
        foreach (var source in sfxSources)
            source.volume = SFXVolume;
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.isPlaying)
            bgmSource.Stop();

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        AudioSource freeSource = sfxSources.FirstOrDefault(s => !s.isPlaying);
        if (freeSource != null)
            freeSource.PlayOneShot(clip, SFXVolume * volumeScale);
        else
            Debug.LogWarning("No available SFX source! Consider increasing pool size.");
    }

    public void StopAllSFX()
    {
        foreach (var source in sfxSources)
            source.Stop();
    }
}