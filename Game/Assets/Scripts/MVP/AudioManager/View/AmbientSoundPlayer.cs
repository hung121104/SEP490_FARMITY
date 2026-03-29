using System.Collections;
using UnityEngine;

/// <summary>
/// Manages ambient background audio: time-of-day layers + biome zone layers.
/// Uses two AudioSource pairs for crossfading between tracks.
///
/// Attach to the AudioManager GameObject (or a child).
/// Reads hour from TimeManagerView to decide day/night.
/// Reads current zone from AmbientZoneTrigger callbacks.
/// </summary>
public class AmbientSoundPlayer : MonoBehaviour
{
    public static AmbientSoundPlayer Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TimeManagerView timeManager;

    [Header("Day/Night Thresholds")]
    [SerializeField] private int nightStartHour = 20;
    [SerializeField] private int nightEndHour = 6;

    [Header("Crossfade")]
    [Tooltip("Duration of ambient crossfade in seconds")]
    [SerializeField] private float crossfadeDuration = 3f;

    [Header("Rain Overlay")]
    [Tooltip("When raining, day/night ambience is multiplied by this value")]
    [Range(0f, 1f)]
    [SerializeField] private float rainDuckMultiplier = 0.55f;
    [Tooltip("Extra gain trim for the rain overlay itself to avoid masking footsteps")]
    [Range(0f, 1f)]
    [SerializeField] private float rainOverlayVolumeMultiplier = 0.45f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // Two sources for crossfading time-of-day ambient
    private AudioSource _timeSourceA;
    private AudioSource _timeSourceB;
    private bool _timeUsingA = true;

    // Two sources for crossfading zone ambient
    private AudioSource _zoneSourceA;
    private AudioSource _zoneSourceB;
    private bool _zoneUsingA = true;

    // Dedicated rain overlay source (does not replace other ambience)
    private AudioSource _rainSource;
    private float _rainBaseVolume;
    private Coroutine _rainFadeRoutine;

    private bool _isNight;
    private bool _hasStartedTimeAmbient;
    private bool _isRainActive;
    private AmbientZoneType _currentZone = AmbientZoneType.Default;

    private float _timeBaseVolumeA;
    private float _timeBaseVolumeB;

    private Coroutine _timeFadeRoutine;
    private Coroutine _zoneFadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        _timeSourceA = CreateAmbientSource("TimeAmbient_A");
        _timeSourceB = CreateAmbientSource("TimeAmbient_B");
        _zoneSourceA = CreateAmbientSource("ZoneAmbient_A");
        _zoneSourceB = CreateAmbientSource("ZoneAmbient_B");
        _rainSource = CreateAmbientSource("RainAmbient");
        _rainSource.volume = 0f;
        _rainSource.loop = true;
    }

    private void OnEnable()
    {
        WeatherView.OnRainStarted += HandleRainStarted;
        WeatherView.OnRainStopped += HandleRainStopped;
    }

    private void OnDisable()
    {
        WeatherView.OnRainStarted -= HandleRainStarted;
        WeatherView.OnRainStopped -= HandleRainStopped;
    }

    private void Start()
    {
        if (timeManager == null)
            timeManager = FindAnyObjectByType<TimeManagerView>();

        // Initial state
        EvaluateTimeOfDay(forceImmediate: true);
        SetRainActive(WeatherView.IsRaining);
    }

    private void Update()
    {
        EvaluateTimeOfDay(forceImmediate: false);
    }

    #region Time-of-Day

    private void EvaluateTimeOfDay(bool forceImmediate)
    {
        // Re-resolve after scene reload (DontDestroyOnLoad survives but scene refs don't)
        if (timeManager == null)
            timeManager = FindAnyObjectByType<TimeManagerView>();
        if (timeManager == null) return;

        bool night = timeManager.hour >= nightStartHour || timeManager.hour < nightEndHour;
        // Keep trying until the ambient actually starts. This prevents a startup race
        // where AudioManager/SoundLibrary isn't ready on the first frame.
        bool shouldAttempt = forceImmediate || !_hasStartedTimeAmbient || night != _isNight;
        if (!shouldAttempt) return;

        SoundId targetId = night ? SoundId.AmbientNightCrickets : SoundId.AmbientDayBirds;
        bool started = CrossfadeTimeSources(targetId, forceImmediate || !_hasStartedTimeAmbient);
        if (!started)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[AmbientSoundPlayer] Failed to start time ambient: {targetId}. Will retry.");
            return;
        }

        _isNight = night;
        _hasStartedTimeAmbient = true;
        if (showDebugLogs) Debug.Log($"[AmbientSoundPlayer] Time ambient active → {targetId}");
    }

    private bool CrossfadeTimeSources(SoundId id, bool immediate)
    {
        if (AudioManager.Instance == null || AudioManager.Instance.Library == null) return false;
        if (!AudioManager.Instance.Library.TryGet(id, out var entry)) return false;

        var clip = entry.GetRandomClip();
        if (clip == null) return false;

        var incoming = _timeUsingA ? _timeSourceA : _timeSourceB;
        var outgoing = _timeUsingA ? _timeSourceB : _timeSourceA;
        _timeUsingA = !_timeUsingA;

        incoming.clip = clip;
        incoming.loop = true;

        if (immediate)
        {
            outgoing.Stop();
            SetTimeBaseVolume(outgoing, 0f);
            SetTimeBaseVolume(incoming, entry.volume);
            ApplyTimeSourceVolumesFromBase();
            incoming.Play();
        }
        else
        {
            if (_timeFadeRoutine != null) StopCoroutine(_timeFadeRoutine);
            _timeFadeRoutine = StartCoroutine(CrossfadeTimeSourcesRoutine(outgoing, incoming, entry.volume, crossfadeDuration));
        }

        return true;
    }

    #endregion

    #region Zone

    /// <summary>Called by AmbientZoneTrigger when the local player enters a zone.</summary>
    public void SetZone(AmbientZoneType zone)
    {
        if (zone == _currentZone) return;
        _currentZone = zone;

        SoundId targetId = zone switch
        {
            AmbientZoneType.Seaside => SoundId.AmbientSeaside,
            AmbientZoneType.Forest => SoundId.AmbientForest,
            AmbientZoneType.Cave => SoundId.AmbientRain, // placeholder — add AmbientCave to SoundId if needed
            _ => SoundId.None,
        };

        if (targetId == SoundId.None)
        {
            // Fade out zone ambient
            if (_zoneFadeRoutine != null) StopCoroutine(_zoneFadeRoutine);
            var active = _zoneUsingA ? _zoneSourceB : _zoneSourceA;
            _zoneFadeRoutine = StartCoroutine(FadeOut(active, crossfadeDuration));
            return;
        }

        CrossfadeZoneSources(targetId);
        if (showDebugLogs) Debug.Log($"[AmbientSoundPlayer] Zone → {zone}");
    }

    private void CrossfadeZoneSources(SoundId id)
    {
        if (AudioManager.Instance == null || AudioManager.Instance.Library == null) return;
        if (!AudioManager.Instance.Library.TryGet(id, out var entry)) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        var incoming = _zoneUsingA ? _zoneSourceA : _zoneSourceB;
        var outgoing = _zoneUsingA ? _zoneSourceB : _zoneSourceA;
        _zoneUsingA = !_zoneUsingA;

        incoming.clip = clip;
        incoming.loop = true;

        if (_zoneFadeRoutine != null) StopCoroutine(_zoneFadeRoutine);
        _zoneFadeRoutine = StartCoroutine(Crossfade(outgoing, incoming, entry.volume, crossfadeDuration));
    }

    #endregion

    #region Weather Hook

    /// <summary>
    /// Call when weather changes. Rain ambient overlays on top of existing ambience.
    /// Can be wired to a weather event if ManageWeather publishes one.
    /// </summary>
    public void SetRainActive(bool raining)
    {
        if (raining == _isRainActive)
            return;

        _isRainActive = raining;
        ApplyTimeSourceVolumesFromBase();

        if (raining)
        {
            if (AudioManager.Instance == null || AudioManager.Instance.Library == null)
                return;
            if (!AudioManager.Instance.Library.TryGet(SoundId.AmbientRain, out var entry))
                return;

            var clip = entry.GetRandomClip();
            if (clip == null)
                return;

            _rainBaseVolume = entry.volume * rainOverlayVolumeMultiplier;
            _rainSource.clip = clip;
            _rainSource.loop = true;
            if (!_rainSource.isPlaying)
                _rainSource.Play();

            if (_rainFadeRoutine != null) StopCoroutine(_rainFadeRoutine);
            _rainFadeRoutine = StartCoroutine(FadeSourceTo(_rainSource, _rainBaseVolume, crossfadeDuration));
            if (showDebugLogs) Debug.Log("[AmbientSoundPlayer] Rain overlay ON (ducking day/night ambience)");
        }
        else
        {
            if (_rainFadeRoutine != null) StopCoroutine(_rainFadeRoutine);
            _rainFadeRoutine = StartCoroutine(FadeSourceTo(_rainSource, 0f, crossfadeDuration, stopWhenZero: true));
            if (showDebugLogs) Debug.Log("[AmbientSoundPlayer] Rain overlay OFF");
        }
    }

    private void HandleRainStarted()
    {
        SetRainActive(true);
    }

    private void HandleRainStopped()
    {
        SetRainActive(false);
    }

    #endregion

    #region Helpers

    private AudioSource CreateAmbientSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f; // full 2D — ambient is non-positional
        src.volume = 0f;
        if (AudioManager.Instance != null)
            src.outputAudioMixerGroup = AudioManager.Instance.AmbientGroup;
        return src;
    }

    private float GetTimeDuckingMultiplier()
    {
        return _isRainActive ? rainDuckMultiplier : 1f;
    }

    private void SetTimeBaseVolume(AudioSource source, float volume)
    {
        if (source == _timeSourceA)
            _timeBaseVolumeA = Mathf.Max(0f, volume);
        else if (source == _timeSourceB)
            _timeBaseVolumeB = Mathf.Max(0f, volume);
    }

    private float GetTimeBaseVolume(AudioSource source)
    {
        if (source == _timeSourceA) return _timeBaseVolumeA;
        if (source == _timeSourceB) return _timeBaseVolumeB;
        return 0f;
    }

    private void ApplyTimeSourceVolumesFromBase()
    {
        float mul = GetTimeDuckingMultiplier();
        _timeSourceA.volume = _timeBaseVolumeA * mul;
        _timeSourceB.volume = _timeBaseVolumeB * mul;
    }

    private IEnumerator Crossfade(AudioSource outgoing, AudioSource incoming, float targetVol, float duration)
    {
        incoming.volume = 0f;
        incoming.Play();
        float startVol = outgoing.volume;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            incoming.volume = Mathf.Lerp(0f, targetVol, t);
            outgoing.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        incoming.volume = targetVol;
        outgoing.volume = 0f;
        outgoing.Stop();
    }

    private IEnumerator CrossfadeTimeSourcesRoutine(AudioSource outgoing, AudioSource incoming, float incomingBaseTarget, float duration)
    {
        float outgoingBaseStart = GetTimeBaseVolume(outgoing);
        SetTimeBaseVolume(incoming, 0f);
        ApplyTimeSourceVolumesFromBase();

        if (!incoming.isPlaying)
            incoming.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            SetTimeBaseVolume(incoming, Mathf.Lerp(0f, incomingBaseTarget, t));
            SetTimeBaseVolume(outgoing, Mathf.Lerp(outgoingBaseStart, 0f, t));
            ApplyTimeSourceVolumesFromBase();

            yield return null;
        }

        SetTimeBaseVolume(incoming, incomingBaseTarget);
        SetTimeBaseVolume(outgoing, 0f);
        ApplyTimeSourceVolumesFromBase();
        outgoing.Stop();
    }

    private IEnumerator FadeSourceTo(AudioSource source, float target, float duration, bool stopWhenZero = false)
    {
        float start = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.volume = target;
        if (stopWhenZero && target <= 0.0001f)
            source.Stop();
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        source.volume = 0f;
        source.Stop();
    }

    #endregion
}
