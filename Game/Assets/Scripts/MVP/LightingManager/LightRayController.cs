using UnityEngine;

/// <summary>
/// Enables / disables a particle-based light-ray overlay during
/// golden-hour windows.  Attach to a GameObject with a ParticleSystem
/// that is a child of the main camera.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class LightRayController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private DayNightCycleConfig config;

    private ParticleSystem _ps;
    private bool _wasPlaying;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (DayNightCycleManager.Instance == null || config == null)
        {
            StopIfPlaying();
            return;
        }

        float t = DayNightCycleManager.Instance.DayProgress;

        bool inMorning = t >= config.morningRaysStart && t <= config.morningRaysEnd;
        bool inEvening = t >= config.eveningRaysStart && t <= config.eveningRaysEnd;
        bool shouldPlay = (inMorning || inEvening) && !WeatherView.IsRaining;

        if (shouldPlay && !_wasPlaying)
        {
            _ps.Play();
            _wasPlaying = true;
        }
        else if (!shouldPlay && _wasPlaying)
        {
            StopIfPlaying();
        }
    }

    private void StopIfPlaying()
    {
        if (_wasPlaying)
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _wasPlaying = false;
        }
    }
}
