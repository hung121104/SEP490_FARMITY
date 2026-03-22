using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Singleton that drives the Global Light 2D each frame based on
/// TimeManagerView.hour/minute, the active season, and rain state.
/// </summary>
public class DayNightCycleManager : MonoBehaviour
{
    public static DayNightCycleManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TimeManagerView timeManager;
    [SerializeField] private SeasonManagerView seasonManager;
    [SerializeField] private Light2D globalLight;

    [Header("Config")]
    [SerializeField] private DayNightCycleConfig config;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    /// <summary>0‥1 representing midnight→midnight (0 = 00:00, 0.5 = 12:00).</summary>
    public float DayProgress { get; private set; }

    /// <summary>Current computed intensity (after rain multiplier).</summary>
    public float CurrentIntensity { get; private set; }

    /// <summary>Current cloud shadow opacity (0 at night, scaled by rain).</summary>
    public float CloudShadowOpacity { get; private set; }

    /// <summary>The ScriptableObject driving all curves. Exposed so SpriteShadowShader
    /// can auto-populate its config reference from the singleton rather than needing
    /// a manual Inspector assignment on every prefab instance.</summary>
    public DayNightCycleConfig Config => config;

    // rain lerp state
    private float _rainBlend;   // 0 = clear, 1 = full rain
    private bool  _isRaining;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        WeatherView.OnRainStarted += HandleRainStarted;
        WeatherView.OnRainStopped += HandleRainStopped;
    }

    void OnDisable()
    {
        WeatherView.OnRainStarted -= HandleRainStarted;
        WeatherView.OnRainStopped -= HandleRainStopped;
    }

    void Start()
    {
        // If the scene already has rain active, pick it up immediately
        _isRaining = WeatherView.IsRaining;
        _rainBlend = _isRaining ? 1f : 0f;
    }

    void Update()
    {
        if (timeManager == null || config == null || globalLight == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[DayNight] Missing ref — time:{timeManager != null} config:{config != null} light:{globalLight != null}");
            return;
        }

        // 1. Compute dayProgress
        DayProgress = (timeManager.hour + timeManager.minute / 60f) / 24f;

        // 2. Pick season curves
        bool isSunny = seasonManager == null || seasonManager.CurrentSeason == Season.Sunny;
        AnimationCurve intensityCurve = isSunny ? config.sunnyIntensity : config.rainyIntensity;
        Gradient colorGradient        = isSunny ? config.sunnyColor     : config.rainyColor;

        // 3. Sample curves
        float baseIntensity = intensityCurve.Evaluate(DayProgress);
        Color baseColor     = colorGradient.Evaluate(DayProgress);

        // 4. Rain blend (smooth lerp)
        float rainTarget = _isRaining ? 1f : 0f;
        float lerpSpeed  = config.rainTransitionDuration > 0f
            ? Time.deltaTime / config.rainTransitionDuration
            : 1f;
        _rainBlend = Mathf.MoveTowards(_rainBlend, rainTarget, lerpSpeed);

        float rainMul = Mathf.Lerp(1f, config.rainIntensityMultiplier, _rainBlend);
        CurrentIntensity = baseIntensity * rainMul;

        // 5. Apply to Global Light 2D
        globalLight.intensity = CurrentIntensity;
        globalLight.color     = baseColor;

        // 6. Cloud shadow opacity (driven by config curve + rain multiplier)
        float baseCloudOpacity = config.cloudShadowOpacity.Evaluate(DayProgress);
        float cloudRainMul     = Mathf.Lerp(1f, config.cloudRainOpacityMultiplier, _rainBlend);
        CloudShadowOpacity     = Mathf.Clamp01(baseCloudOpacity * cloudRainMul);

        if (showDebugLogs && Time.frameCount % 120 == 0)
            Debug.Log($"[DayNight] hour={timeManager.hour} min={timeManager.minute:F0} dayProgress={DayProgress:F3} intensity={CurrentIntensity:F3} color={baseColor} rain={_rainBlend:F2} cloudOpacity={CloudShadowOpacity:F2}");
    }

    // ─── Weather callbacks ───────────────────────────────────────

    private void HandleRainStarted() => _isRaining = true;
    private void HandleRainStopped() => _isRaining = false;
}
