using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Fades a child Light2D on the player over 20 in-game minutes when nighttime starts (18:30)
/// and fades it back out over 20 in-game minutes at dawn (6:00).
/// Attach to the Player prefab root. A Light2D child will be auto-created if none is assigned.
/// </summary>
public class PlayerLightView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D playerLight;

    [Header("Schedule")]
    [SerializeField] private int lightOnHour = 18;
    [SerializeField] private int lightOnMinute = 30;
    [SerializeField] private int lightOffHour = 6;
    [SerializeField] private int lightOffMinute = 0;

    [Header("Fade")]
    [Tooltip("In-game minutes to fade from 0 to full intensity (and vice versa).")]
    [SerializeField] private float fadeDurationGameMinutes = 20f;

    [Header("Light Settings (used when auto-creating)")]
    [SerializeField] private float lightRadius = 6f;
    [SerializeField] private float maxIntensity = 0.8f;
    [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.8f, 1f);

    private TimeManagerView _timeManager;
    private float _currentBlend; // 0 = off, 1 = full

    private void Awake()
    {
        _timeManager = FindFirstObjectByType<TimeManagerView>();

        if (playerLight == null)
            CreateDefaultLight();
    }

    private void Start()
    {
        if (playerLight == null) return;

        // Snap to correct state on spawn
        _currentBlend = IsNightTime() ? 1f : 0f;
        ApplyIntensity();
    }

    private void Update()
    {
        if (_timeManager == null || playerLight == null) return;

        float targetBlend = IsNightTime() ? 1f : 0f;

        if (Mathf.Approximately(_currentBlend, targetBlend)) return;

        // Calculate fade speed: full fade in fadeDurationGameMinutes of in-game time.
        // timeSpeed = in-game minutes per real second.
        float gameMinutesPerSecond = _timeManager.timeSpeed;
        float fadePerSecond = gameMinutesPerSecond / Mathf.Max(fadeDurationGameMinutes, 0.01f);
        float step = fadePerSecond * Time.deltaTime;

        _currentBlend = Mathf.MoveTowards(_currentBlend, targetBlend, step);
        ApplyIntensity();
    }

    private void ApplyIntensity()
    {
        playerLight.intensity = _currentBlend * maxIntensity;
        playerLight.enabled = _currentBlend > 0f;
    }

    private bool IsNightTime()
    {
        if (_timeManager == null) return false;

        float currentTime = _timeManager.hour + _timeManager.minute / 60f;
        float onTime = lightOnHour + lightOnMinute / 60f;
        float offTime = lightOffHour + lightOffMinute / 60f;

        // Night wraps past midnight: on at 18:30, off at 6:00
        if (onTime > offTime)
            return currentTime >= onTime || currentTime < offTime;

        return currentTime >= onTime && currentTime < offTime;
    }

    private void CreateDefaultLight()
    {
        GameObject lightGO = new GameObject("PlayerLight");
        lightGO.transform.SetParent(transform);
        lightGO.transform.localPosition = Vector3.zero;

        playerLight = lightGO.AddComponent<Light2D>();
        playerLight.lightType = Light2D.LightType.Point;
        playerLight.pointLightOuterRadius = lightRadius;
        playerLight.pointLightInnerRadius = lightRadius * 0.3f;
        playerLight.intensity = 0f;
        playerLight.color = lightColor;
        playerLight.enabled = false;
    }
}
