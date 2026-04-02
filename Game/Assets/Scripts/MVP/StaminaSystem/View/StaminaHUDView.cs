using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD component that visualises the local player's triple-layer stamina using two Sliders.
///
/// Hierarchy expected on your HUD Canvas:
///   StaminaBar (this script)
///     ├─ ViableSlider    (Slider)           ← min 0 / max driven by MaxStamina (200)
///     │    └─ Fill       (Image)            ← tint via ViableColor
///     ├─ CurrentSlider   (Slider)           ← same max, sits on top via sibling order
///     │    └─ Fill       (Image)            ← tinted green/orange/red by threshold
///     ├─ StaminaText     (TextMeshProUGUI)  ← "87 / 200"
///     └─ ExhaustedIcon   (GameObject)       ← shown while current ≈ 0
///
/// Set both Sliders to:
///   • Direction = Left To Right
///   • Min Value = 0
///   • Max Value = 200  (or leave at 1 — the script sets it at runtime)
///   • Interactable = OFF
///   • Whole Numbers = OFF
///
/// Attach to a persistent HUD canvas so it survives scene transitions.
/// </summary>
public class StaminaHUDView : MonoBehaviour
{
    // ─── Sliders ──────────────────────────────────────────────────────────────
    [Header("Sliders")]
    [Tooltip("Background Slider showing viable stamina (max = MaxStamina).")]
    [SerializeField] private Slider viableSlider;

    [Tooltip("Foreground Slider showing current stamina (max = MaxStamina).")]
    [SerializeField] private Slider currentSlider;

    // ─── Fill Images inside the Sliders ───────────────────────────────────────
    [Header("Fill Images (inside each Slider)")]
    [Tooltip("The Fill Image child of ViableSlider — used for color tinting.")]
    [SerializeField] private Image viableFillImage;

    [Tooltip("The Fill Image child of CurrentSlider — tinted by stamina ratio.")]
    [SerializeField] private Image currentFillImage;

    // ─── Optional UI elements ─────────────────────────────────────────────────
    [Header("Optional UI")]
    [Tooltip("TextMeshPro label showing e.g. '87 / 200'.")]
    [SerializeField] private TextMeshProUGUI staminaText;

    [Tooltip("A GameObject (icon / text) shown when current stamina is at or near zero.")]
    [SerializeField] private GameObject exhaustedIndicator;

    // ─── Color tiers for current fill ─────────────────────────────────────────
    [Header("Current-bar Colors")]
    [SerializeField] private Color normalColor   = new Color(0.25f, 0.88f, 0.35f, 1f);
    [SerializeField] private Color lowColor      = new Color(1.00f, 0.65f, 0.10f, 1f);
    [SerializeField] private Color criticalColor = new Color(0.92f, 0.12f, 0.12f, 1f);

    [Header("Viable-bar Color")]
    [SerializeField] private Color viableColor   = new Color(0.15f, 0.45f, 0.20f, 1f);

    // ─── Thresholds ───────────────────────────────────────────────────────────
    [Header("Thresholds")]
    [Tooltip("current/max ratio below which the bar turns orange.")]
    [SerializeField] private float lowThreshold      = 0.25f;

    [Tooltip("current/max ratio below which the bar turns red.")]
    [SerializeField] private float criticalThreshold = 0.10f;

    [Tooltip("current/max ratio below which the exhausted icon shows.")]
    [SerializeField] private float exhaustedThreshold = 0.02f;

    // ─── Smoothing ────────────────────────────────────────────────────────────
    [Header("Smoothing")]
    [Tooltip("Higher = faster bar response.  8 is a good starting value.")]
    [SerializeField] private float fillSmoothSpeed = 8f;

    // ─── Pulse / flash on exhaustion ──────────────────────────────────────────
    [Header("Low-stamina Pulse")]
    [Tooltip("Enable a slow pulse on the current fill when critically low.")]
    [SerializeField] private bool  enableCriticalPulse = true;
    [SerializeField] private float pulseSpeed          = 3f;
    [Tooltip("Alpha oscillates between these values during the pulse.")]
    [SerializeField] private float pulseAlphaMin       = 0.45f;
    [SerializeField] private float pulseAlphaMax       = 1.00f;

    // ─── Boost Effect Visuals ─────────────────────────────────────────────────
    [Header("Regen Boost Effect")]
    [Tooltip("GameObject shown (e.g. an icon) while regen boost is active.")]
    [SerializeField] private GameObject regenBoostIndicator;
    [Tooltip("Optional TMP label showing remaining regen boost seconds.")]
    [SerializeField] private TextMeshProUGUI regenBoostTimerText;
    [Tooltip("Current bar tint while regen boost is active.")]
    [SerializeField] private Color regenBoostBarColor = new Color(0.20f, 0.85f, 1.00f, 1f);

    [Header("Tool Efficiency Effect")]
    [Tooltip("GameObject shown (e.g. an icon) while tool efficiency reduction is active.")]
    [SerializeField] private GameObject toolEfficiencyIndicator;
    [Tooltip("Optional TMP label showing remaining tool efficiency seconds.")]
    [SerializeField] private TextMeshProUGUI toolEfficiencyTimerText;
    [Tooltip("Viable bar tint while tool efficiency reduction is active.")]
    [SerializeField] private Color toolEfficiencyBarColor = new Color(1.00f, 0.85f, 0.15f, 1f);

    // ─── Private state ────────────────────────────────────────────────────────
    private StaminaView _staminaView;
    private float       _retryTimer;
    private const float RetryInterval = 0.5f;

    private float _displayCurrent;
    private float _displayViable;
    private float _initializedMax = -1f;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Start sliders at full so they don't flash empty before binding.
        InitSliders(200f);
        SetSliderValues(200f, 200f);
    }

    private void LateUpdate()
    {
        // ── 1. Lazily bind to the local player's StaminaView ──────────────────
        if (_staminaView == null)
        {
            _retryTimer -= Time.deltaTime;
            if (_retryTimer > 0f) return;

            _retryTimer  = RetryInterval;
            _staminaView = StaminaView.FindLocal();
            if (_staminaView == null) return;
        }

        float max     = _staminaView.MaxStamina;
        float current = _staminaView.CurrentStamina;
        float viable  = _staminaView.ViableStamina;

        if (max <= 0f) return;

        // ── 2. Update slider max when MaxStamina changes (e.g. on first bind) ─
        if (!Mathf.Approximately(_initializedMax, max))
        {
            InitSliders(max);
            _displayCurrent = current;
            _displayViable  = viable;
        }

        // ── 3. Smooth the display values (in stamina units, not ratios) ────────
        _displayCurrent = Mathf.Lerp(_displayCurrent, current, Time.deltaTime * fillSmoothSpeed);
        _displayViable  = Mathf.Lerp(_displayViable,  viable,  Time.deltaTime * fillSmoothSpeed);

        SetSliderValues(_displayCurrent, _displayViable);

        float regenRemaining = _staminaView.RegenBoostRemaining;
        float effRemaining  = _staminaView.ToolEfficiencyRemaining;
        bool  regenActive   = regenRemaining > 0f;
        bool  effActive     = effRemaining   > 0f;

        // ── 4. Current fill color ─────────────────────────────────────────────
        if (currentFillImage != null)
        {
            float ratio = current / max;

            Color targetColor;
            if (regenActive)
            {
                // Cyan tint pulses gently to signal accelerated regen
                targetColor   = regenBoostBarColor;
                float pulse   = Mathf.PingPong(Time.time * pulseSpeed, 1f);
                targetColor.a = Mathf.Lerp(0.70f, 1.00f, pulse);
            }
            else
            {
                targetColor = ratio <= criticalThreshold ? criticalColor
                            : ratio <= lowThreshold      ? lowColor
                            :                              normalColor;

                // Pulse alpha when critical
                if (enableCriticalPulse && ratio <= criticalThreshold)
                {
                    float pulse   = Mathf.PingPong(Time.time * pulseSpeed, 1f);
                    targetColor.a = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax, pulse);
                }
            }

            currentFillImage.color = Color.Lerp(currentFillImage.color, targetColor, Time.deltaTime * fillSmoothSpeed);
        }

        // Viable bar turns gold while tool efficiency reduction is active
        if (viableFillImage != null)
            viableFillImage.color = effActive ? toolEfficiencyBarColor : viableColor;

        // ── 5. Text ───────────────────────────────────────────────────────────
        if (staminaText != null)
            staminaText.text = $"{Mathf.CeilToInt(current)} / {Mathf.RoundToInt(max)}";

        // ── 6. Exhausted indicator ────────────────────────────────────────────
        if (exhaustedIndicator != null)
            exhaustedIndicator.SetActive((current / max) <= exhaustedThreshold);

        // ── 7. Regen boost indicator ──────────────────────────────────────────
        if (regenBoostIndicator != null)
            regenBoostIndicator.SetActive(regenActive);
        if (regenBoostTimerText != null)
            regenBoostTimerText.text = regenActive ? $"{regenRemaining:F1}s" : string.Empty;

        // ── 8. Tool efficiency indicator ──────────────────────────────────────
        if (toolEfficiencyIndicator != null)
            toolEfficiencyIndicator.SetActive(effActive);
        if (toolEfficiencyTimerText != null)
            toolEfficiencyTimerText.text = effActive
                ? $"-{_staminaView.ToolEfficiencyReduction * 100f:F0}% ({effRemaining:F1}s)"
                : string.Empty;
    }

    private void InitSliders(float max)
    {
        _initializedMax = max;

        if (viableSlider != null)
        {
            viableSlider.minValue    = 0f;
            viableSlider.maxValue    = max;
            viableSlider.interactable = false;
            viableSlider.wholeNumbers = false;
        }

        if (currentSlider != null)
        {
            currentSlider.minValue    = 0f;
            currentSlider.maxValue    = max;
            currentSlider.interactable = false;
            currentSlider.wholeNumbers = false;
        }
    }

    private void SetSliderValues(float current, float viable)
    {
        if (viableSlider  != null) viableSlider.value  = viable;
        if (currentSlider != null) currentSlider.value = current;
    }

    /// <summary>
    /// Call this when the local player object is destroyed / despawned,
    /// so the HUD re-searches on the next spawn.
    /// </summary>
    public void ClearBinding()
    {
        _staminaView    = null;
        _retryTimer     = 0f;
        _initializedMax = -1f;
    }
}
