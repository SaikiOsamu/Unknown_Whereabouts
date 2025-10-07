using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

public enum AudioChannel { BGM, SFX, UI, Ambience }

[Serializable]
public class SoundDef
{
    public string key;
    public AudioClip clip;
    public AudioChannel channel = AudioChannel.SFX;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-3f, 3f)] public float pitch = 1f;
    public bool loop = false;
    public bool playOnAwake = false;
    [Range(0f, 1f)] public float spatialBlend = 0f;
    [Range(0, 256)] public int priority = 128;
    [Min(1)] public int voices = 1;
    public AudioMixerGroup mixerGroup;
}

internal class SoundInstance
{
    public SoundDef def;
    public List<AudioSource> sources = new List<AudioSource>();
    private int _next;
    public AudioSource Next()
    {
        if (sources.Count == 0) return null;
        var src = sources[_next];
        _next = (_next + 1) % sources.Count;
        return src;
    }
}

public class AudioManager : Singleton<AudioManager>
{
    [Header("Catalog")]
    [SerializeField] private List<SoundDef> sounds = new List<SoundDef>();

    [Header("Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultMaster = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultBGM = 0.75f;
    [SerializeField, Range(0f, 1f)] private float defaultSFX = 0.75f;
    [SerializeField, Range(0f, 1f)] private float defaultUI = 0.75f;
    [SerializeField, Range(0f, 1f)] private float defaultAmb = 0.75f;

    private readonly Dictionary<string, SoundInstance> _map = new();
    private readonly Dictionary<AudioChannel, float> _channelVol = new();
    private const string PREF_MASTER = "AM_Master";
    private const string PREF_BGM = "AM_BGM";
    private const string PREF_SFX = "AM_SFX";
    private const string PREF_UI = "AM_UI";
    private const string PREF_AMB = "AM_Amb";

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        _channelVol[AudioChannel.BGM] = PlayerPrefs.GetFloat(PREF_BGM, defaultBGM);
        _channelVol[AudioChannel.SFX] = PlayerPrefs.GetFloat(PREF_SFX, defaultSFX);
        _channelVol[AudioChannel.UI] = PlayerPrefs.GetFloat(PREF_UI, defaultUI);
        _channelVol[AudioChannel.Ambience] = PlayerPrefs.GetFloat(PREF_AMB, defaultAmb);
        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER, defaultMaster);
        BuildSources();
    }

    private void BuildSources()
    {
        _map.Clear();
        var dup = sounds.Where(s => !string.IsNullOrWhiteSpace(s.key))
                        .GroupBy(s => s.key)
                        .FirstOrDefault(g => g.Count() > 1);
        if (dup != null)
            Debug.LogWarning($"[AudioManager] Duplicate key: {dup.Key}");
        foreach (var def in sounds)
        {
            if (def == null || def.clip == null || string.IsNullOrWhiteSpace(def.key)) continue;
            if (_map.ContainsKey(def.key)) continue;
            var inst = new SoundInstance { def = def };
            for (int i = 0; i < Mathf.Max(1, def.voices); i++)
            {
                var go = new GameObject($"Audio_{def.key}_v{i}");
                go.transform.SetParent(transform, false);
                var src = go.AddComponent<AudioSource>();
                src.clip = def.clip;
                src.loop = def.loop;
                src.playOnAwake = false;
                src.priority = def.priority;
                src.spatialBlend = def.spatialBlend;
                src.pitch = def.pitch;
                if (def.mixerGroup) src.outputAudioMixerGroup = def.mixerGroup;
                inst.sources.Add(src);
            }
            _map.Add(def.key, inst);
        }
        foreach (var kv in _map)
        {
            if (kv.Value.def.playOnAwake)
                Play(kv.Key);
        }
        ApplyVolumesToAllSources();
    }

    private float FinalVolume(SoundDef def, float scale = 1f)
    {
        return Mathf.Clamp01(MasterVolume * GetChannelVolume(def.channel) * def.volume * scale);
    }

    private float _masterVolume = 1f;
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PREF_MASTER, _masterVolume);
            ApplyVolumesToAllSources();
        }
    }

    public float GetChannelVolume(AudioChannel ch) =>
        _channelVol.TryGetValue(ch, out var v) ? v : 1f;

    public void SetChannelVolume(AudioChannel ch, float v)
    {
        v = Mathf.Clamp01(v);
        _channelVol[ch] = v;
        switch (ch)
        {
            case AudioChannel.BGM: PlayerPrefs.SetFloat(PREF_BGM, v); break;
            case AudioChannel.SFX: PlayerPrefs.SetFloat(PREF_SFX, v); break;
            case AudioChannel.UI: PlayerPrefs.SetFloat(PREF_UI, v); break;
            case AudioChannel.Ambience: PlayerPrefs.SetFloat(PREF_AMB, v); break;
        }
        ApplyVolumesToAllSources();
    }

    private void ApplyVolumesToAllSources()
    {
        foreach (var inst in _map.Values)
        {
            foreach (var src in inst.sources)
            {
                src.volume = FinalVolume(inst.def);
            }
        }
    }

    public void Play(string key, float volumeScale = 1f, bool restartIfPlaying = false)
    {
        if (!_map.TryGetValue(key, out var inst) || inst.sources.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] Not found: {key}");
            return;
        }
        var src = inst.Next();
        if (src == null) return;
        if (restartIfPlaying && src.isPlaying) src.Stop();
        if (!inst.def.loop)
        {
            src.pitch = inst.def.pitch;
            src.spatialBlend = inst.def.spatialBlend;
            src.priority = inst.def.priority;
            src.outputAudioMixerGroup = inst.def.mixerGroup;
            src.PlayOneShot(inst.def.clip, FinalVolume(inst.def, volumeScale));
        }
        else
        {
            src.clip = inst.def.clip;
            src.loop = true;
            src.volume = FinalVolume(inst.def, volumeScale);
            src.pitch = inst.def.pitch;
            src.spatialBlend = inst.def.spatialBlend;
            src.priority = inst.def.priority;
            src.outputAudioMixerGroup = inst.def.mixerGroup;
            src.Play();
        }
    }

    public void PlayAt(string key, Vector3 position, float volumeScale = 1f)
    {
        if (!_map.TryGetValue(key, out var inst) || inst.sources.Count == 0)
        {
            Debug.LogWarning($"[AudioManager] Not found: {key}");
            return;
        }
        var src = inst.Next();
        if (src == null) return;
        src.transform.position = position;
        if (!inst.def.loop)
        {
            src.PlayOneShot(inst.def.clip, FinalVolume(inst.def, volumeScale));
        }
        else
        {
            src.clip = inst.def.clip;
            src.loop = true;
            src.volume = FinalVolume(inst.def, volumeScale);
            src.Play();
        }
    }

    public void Stop(string key)
    {
        if (!_map.TryGetValue(key, out var inst)) return;
        foreach (var s in inst.sources) s.Stop();
    }

    public void StopChannel(AudioChannel ch)
    {
        foreach (var inst in _map.Values)
        {
            if (inst.def.channel == ch)
                foreach (var s in inst.sources) s.Stop();
        }
    }

    public void StopAll()
    {
        foreach (var inst in _map.Values)
            foreach (var s in inst.sources) s.Stop();
    }

    public void PlayBGM(string key, float fadeOut = 0.3f, float fadeIn = 0.3f)
    {
        if (fadeOut > 0f)
            StartCoroutine(FadeOutChannel(AudioChannel.BGM, fadeOut, () => Play(key)));
        else
        {
            StopChannel(AudioChannel.BGM);
            Play(key);
        }
        if (fadeIn > 0f)
            StartCoroutine(FadeInKey(key, fadeIn));
    }

    public void PlayBGM(params string[] keys)
    {
        if (keys == null || keys.Length == 0) return;
        foreach (var key in keys)
        {
            Play(key, 1f, false);
        }
    }


    private System.Collections.IEnumerator FadeOutChannel(AudioChannel ch, float duration, Action onDone)
    {
        var playing = _map.Values
            .Where(i => i.def.channel == ch)
            .SelectMany(i => i.sources)
            .Where(s => s.isPlaying)
            .ToList();

        var t = 0f;
        var vols = playing.Select(s => s.volume).ToArray();

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            for (int i = 0; i < playing.Count; i++)
                playing[i].volume = vols[i] * k;
            yield return null;
        }
        foreach (var s in playing) s.Stop();
        onDone?.Invoke();
    }

    private System.Collections.IEnumerator FadeInKey(string key, float duration)
    {
        yield return null;
        if (!_map.TryGetValue(key, out var inst)) yield break;
        foreach (var s in inst.sources)
        {
            if (!s.isPlaying) continue;
            float target = FinalVolume(inst.def, 1f);
            s.volume = 0f;
            float t = 0f;
            while (t < duration && s != null)
            {
                t += Time.unscaledDeltaTime;
                s.volume = Mathf.Lerp(0f, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            s.volume = target;
        }
    }
    private void Prewarm(SoundInstance inst)
    {
        foreach (var s in inst.sources)
        {
            if (inst.def.loop) { s.volume = 0f; s.Play(); s.Stop(); }
            else { s.PlayOneShot(inst.def.clip, 0f); }
        }
    }

}
