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

    private bool _isNight;
    private AmbientZoneType _currentZone = AmbientZoneType.Default;

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
    }

    private void Start()
    {
        if (timeManager == null)
            timeManager = FindAnyObjectByType<TimeManagerView>();

        // Initial state
        EvaluateTimeOfDay(forceImmediate: true);
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
        if (night == _isNight && !forceImmediate) return;

        _isNight = night;
        SoundId targetId = _isNight ? SoundId.AmbientNightCrickets : SoundId.AmbientDayBirds;
        CrossfadeTimeSources(targetId, forceImmediate);
        if (showDebugLogs) Debug.Log($"[AmbientSoundPlayer] Time → {(_isNight ? "Night" : "Day")}");
    }

    private void CrossfadeTimeSources(SoundId id, bool immediate)
    {
        if (AudioManager.Instance == null || AudioManager.Instance.Library == null) return;
        if (!AudioManager.Instance.Library.TryGet(id, out var entry)) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        var incoming = _timeUsingA ? _timeSourceA : _timeSourceB;
        var outgoing = _timeUsingA ? _timeSourceB : _timeSourceA;
        _timeUsingA = !_timeUsingA;

        incoming.clip = clip;
        incoming.loop = true;

        if (immediate)
        {
            outgoing.Stop();
            incoming.volume = entry.volume;
            incoming.Play();
        }
        else
        {
            if (_timeFadeRoutine != null) StopCoroutine(_timeFadeRoutine);
            _timeFadeRoutine = StartCoroutine(Crossfade(outgoing, incoming, entry.volume, crossfadeDuration));
        }
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
        if (raining)
        {
            CrossfadeZoneSources(SoundId.AmbientRain);
        }
        else
        {
            // Re-evaluate zone to restore correct zone ambient
            var zone = _currentZone;
            _currentZone = AmbientZoneType.Default; // force re-evaluation
            SetZone(zone);
        }
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
