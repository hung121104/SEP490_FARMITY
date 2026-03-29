using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton audio manager. Owns the SoundLibrary lookup, mixer references,
/// and a small pool of AudioSources for world one-shots.
/// 
/// Usage:  AudioManager.Instance.PlaySFX(SoundId.ToolSwing, worldPos);
///         AudioManager.Instance.PlayUI(SoundId.UIButtonClick);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private SoundLibrary soundLibrary;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup ambientGroup;

    [Header("SFX Pool")]
    [Tooltip("Number of pooled AudioSources for positional one-shots")]
    [SerializeField] private int poolSize = 8;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // exposed for sub-players
    public SoundLibrary Library => soundLibrary;
    public AudioMixerGroup SfxGroup => sfxGroup;
    public AudioMixerGroup UIGroup => uiGroup;
    public AudioMixerGroup AmbientGroup => ambientGroup;

    // ── mixer param names (expose these via AudioMixer) ──
    private const string PARAM_MASTER = "MasterVolume";
    private const string PARAM_SFX = "SFXVolume";
    private const string PARAM_UI = "UIVolume";
    private const string PARAM_AMBIENT = "AmbientVolume";

    // ── pool ──
    private AudioSource[] _pool;
    private int _poolIndex;

    // ── cooldown anti-spam ──
    private readonly Dictionary<SoundId, float> _lastPlayTime = new();
    private const float MIN_REPLAY_INTERVAL = 0.05f; // 50 ms

    // ── 2D source for UI ──
    private AudioSource _uiSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (soundLibrary != null) soundLibrary.Init();
        InitPool();
        InitUISource();
    }

    #region Public API

    /// <summary>Play a 2D sound (UI, menus). No spatialization.</summary>
    public void PlayUI(SoundId id)
    {
        if (!TryGetEntry(id, out var entry)) return;
        if (IsCooldown(id)) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        _uiSource.pitch = entry.GetRandomPitch();
        _uiSource.PlayOneShot(clip, entry.volume);
        LogPlay(id, Vector3.zero);
    }

    /// <summary>Play a 3D positional one-shot from the pool.</summary>
    public void PlaySFX(SoundId id, Vector3 worldPos)
    {
        if (!TryGetEntry(id, out var entry)) return;
        if (IsCooldown(id)) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        var src = GetPooledSource();
        src.transform.position = worldPos;
        src.pitch = entry.GetRandomPitch();
        src.outputAudioMixerGroup = sfxGroup;
        src.PlayOneShot(clip, entry.volume);
        LogPlay(id, worldPos);
    }

    /// <summary>Play directly on a specific AudioSource (e.g., player's own source).</summary>
    public void PlayOnSource(SoundId id, AudioSource source)
    {
        if (source == null) return;
        if (!TryGetEntry(id, out var entry)) return;
        if (IsCooldown(id)) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        source.pitch = entry.GetRandomPitch();
        source.PlayOneShot(clip, entry.volume);
        LogPlay(id, source.transform.position);

        if (showDebugLogs)
        {
            var listener = FindAnyObjectByType<AudioListener>();
            Debug.Log($"[AudioManager] Diagnostics — clip:{clip.name} vol:{entry.volume} " +
                      $"srcVol:{source.volume} srcMute:{source.mute} " +
                      $"spatialBlend:{source.spatialBlend} " +
                      $"mixerGroup:{(source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : "NONE")} " +
                      $"AudioListener:{(listener != null ? listener.gameObject.name : "MISSING")} " +
                      $"ListenerVolume:{AudioListener.volume} ListenerPause:{AudioListener.pause}");
        }
    }

    #endregion

    #region Volume Control

    /// <summary>Set master volume. value: 0–1</summary>
    public void SetMasterVolume(float value)
    {
        if (mainMixer != null)
            mainMixer.SetFloat(PARAM_MASTER, LinearToDecibel(value));
    }

    public void SetSFXVolume(float value)
    {
        if (mainMixer != null)
            mainMixer.SetFloat(PARAM_SFX, LinearToDecibel(value));
    }

    public void SetUIVolume(float value)
    {
        if (mainMixer != null)
            mainMixer.SetFloat(PARAM_UI, LinearToDecibel(value));
    }

    public void SetAmbientVolume(float value)
    {
        if (mainMixer != null)
            mainMixer.SetFloat(PARAM_AMBIENT, LinearToDecibel(value));
    }

    private static float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(Mathf.Clamp01(linear)) * 20f : -80f;
    }

    #endregion

    #region Internals

    private void InitPool()
    {
        _pool = new AudioSource[poolSize];
        var container = new GameObject("SFX_Pool");
        container.transform.SetParent(transform);

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"PoolSrc_{i}");
            go.transform.SetParent(container.transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f; // full 3D
            src.outputAudioMixerGroup = sfxGroup;
            src.maxDistance = 30f;
            src.rolloffMode = AudioRolloffMode.Linear;
            _pool[i] = src;
        }
    }

    private void InitUISource()
    {
        var go = new GameObject("UI_AudioSource");
        go.transform.SetParent(transform);
        _uiSource = go.AddComponent<AudioSource>();
        _uiSource.playOnAwake = false;
        _uiSource.spatialBlend = 0f; // full 2D
        _uiSource.outputAudioMixerGroup = uiGroup;
    }

    private AudioSource GetPooledSource()
    {
        var src = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _pool.Length;
        return src;
    }

    private bool TryGetEntry(SoundId id, out SoundEntry entry)
    {
        entry = null;
        if (soundLibrary == null) return false;
        if (!soundLibrary.TryGet(id, out entry))
        {
            if (showDebugLogs) Debug.LogWarning($"[AudioManager] No clip mapped for {id}");
            return false;
        }
        return true;
    }

    private bool IsCooldown(SoundId id)
    {
        float now = Time.unscaledTime;
        if (_lastPlayTime.TryGetValue(id, out float last) && now - last < MIN_REPLAY_INTERVAL)
            return true;
        _lastPlayTime[id] = now;
        return false;
    }

    private void LogPlay(SoundId id, Vector3 pos)
    {
        if (showDebugLogs) Debug.Log($"[AudioManager] Play {id} @ {pos}");
    }

    #endregion
}
