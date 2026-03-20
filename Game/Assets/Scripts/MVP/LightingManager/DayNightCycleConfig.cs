using UnityEngine;

/// <summary>
/// ScriptableObject holding all curves and gradients that drive the day/night cycle.
/// Create one via Assets ▸ Create ▸ Game ▸ DayNightCycleConfig.
/// After creating, right-click the asset → Reset to get sensible default curves.
/// </summary>
[CreateAssetMenu(fileName = "DayNightCycleConfig", menuName = "Game/DayNightCycleConfig")]
public class DayNightCycleConfig : ScriptableObject
{
    // ── Sunny Season ──────────────────────────────────────────────

    [Header("Sunny Season — Global Light")]
    [Tooltip("Ambient intensity over daytime (x = 0‥1 maps to hour 0‥24)")]
    public AnimationCurve sunnyIntensity = new AnimationCurve(
        new Keyframe(0.00f, 0.05f),   // midnight
        new Keyframe(0.23f, 0.05f),   // 05:30 still dark
        new Keyframe(0.30f, 0.80f),   // 07:00 sunrise done
        new Keyframe(0.50f, 1.00f),   // 12:00 noon
        new Keyframe(0.70f, 0.80f),   // 17:00
        new Keyframe(0.80f, 0.05f),   // 19:00 sunset done
        new Keyframe(1.00f, 0.05f)    // midnight
    );

    [Tooltip("Ambient colour tint over daytime")]
    public Gradient sunnyColor;

    // ── Rainy Season ──────────────────────────────────────────────

    [Header("Rainy Season — Global Light")]
    public AnimationCurve rainyIntensity = new AnimationCurve(
        new Keyframe(0.00f, 0.03f),
        new Keyframe(0.23f, 0.03f),
        new Keyframe(0.30f, 0.50f),
        new Keyframe(0.50f, 0.60f),
        new Keyframe(0.70f, 0.50f),
        new Keyframe(0.80f, 0.03f),
        new Keyframe(1.00f, 0.03f)
    );
    public Gradient rainyColor;

    // ── Rainy Weather Override ────────────────────────────────────

    [Header("Rain Override")]
    [Tooltip("Multiplier applied to intensity when raining")]
    [Range(0f, 1f)]
    public float rainIntensityMultiplier = 0.6f;

    [Tooltip("Seconds to lerp into / out of rain palette")]
    public float rainTransitionDuration = 2f;

    // ── Sun (Spot Light 2D) ──────────────────────────────────────

    [Header("Sun Arc")]
    [Tooltip("Intensity curve for the spot \"sun\" light (x = dayProgress)")]
    public AnimationCurve sunSpotIntensity = new AnimationCurve(
        new Keyframe(0.00f, 0.00f),   // midnight — off
        new Keyframe(0.25f, 0.00f),   // 06:00 sunrise start
        new Keyframe(0.30f, 0.50f),
        new Keyframe(0.50f, 0.75f),   // noon peak
        new Keyframe(0.70f, 0.50f),
        new Keyframe(0.75f, 0.00f),   // 18:00 sunset end
        new Keyframe(1.00f, 0.00f)
    );

    [Tooltip("Arc radius in world units — smaller = steeper shadows, larger = longer shadows")]
    public float sunArcRadius = 12f;

    [Tooltip("Point-light outer radius — must be >= sunArcRadius so light reaches the ground")]
    public float sunLightOuterRadius = 25f;

    [Tooltip("Point-light outer cone angle (degrees). 360 = full circle")]
    [Range(1f, 360f)]
    public float sunLightOuterAngle = 360f;

    [Tooltip("Shadow darkness over daytime (0 = no shadow, 1 = full shadow).\nKeep low at dawn/dusk to hide stretchy shadows.")]
    public AnimationCurve sunShadowIntensity = new AnimationCurve(
        new Keyframe(0.00f, 0.00f),   // midnight — no shadows
        new Keyframe(0.25f, 0.00f),   // 06:00
        new Keyframe(0.32f, 0.60f),   // 07:40 — shadows appear once sun is higher
        new Keyframe(0.50f, 0.70f),   // noon — strongest
        new Keyframe(0.68f, 0.60f),   // 16:20
        new Keyframe(0.75f, 0.00f),   // 18:00 — shadows gone before sunset stretch
        new Keyframe(1.00f, 0.00f)
    );

    [Tooltip("Shadow softness (blur). 0 = hard pixel edges, 1 = blurry.")]
    [Range(0f, 1f)]
    public float sunShadowSoftness = 0.3f;

    [Tooltip("Shadow X scale at dawn/dusk (sun low on horizon)")]
    public float shadowXScaleDawn = 0.5f;

    [Tooltip("Shadow X scale at noon (sun overhead)")]
    public float shadowXScaleNoon = 1.0f;

    [Tooltip("Sunrise angle (degrees). 0 = right, 90 = up")]
    public float sunriseAngle = 170f;

    [Tooltip("Sunset angle (degrees)")]
    public float sunsetAngle = 10f;

    // ── Sprite Shadows (fake projected shadows) ──────────────────

    [Header("Sprite Shadows")]
    [Tooltip("Max stretch multiplier when sun is near the horizon")]
    public float maxShadowStretch = 2.5f;

    [Tooltip("Min stretch at noon (directly overhead)")]
    public float minShadowStretch = 0.5f;

    [Tooltip("How far the shadow offsets from the sprite base (scales with stretch)")]
    public float shadowOffsetMultiplier = 0.3f;

    // ── Light Rays ───────────────────────────────────────────────

    [Header("Light Rays (golden hour)")]
    [Tooltip("DayProgress window start for morning rays")]
    [Range(0f, 1f)] public float morningRaysStart = 0.25f;   // ~06:00
    [Range(0f, 1f)] public float morningRaysEnd   = 0.375f;  // ~09:00

    [Tooltip("DayProgress window for evening rays")]
    [Range(0f, 1f)] public float eveningRaysStart = 0.708f;   // ~17:00
    [Range(0f, 1f)] public float eveningRaysEnd   = 0.833f;   // ~20:00

    // ── Shadow Culling ───────────────────────────────────────────

    [Header("Shadow Culling")]
    [Tooltip("Extra world‑units beyond camera rect to keep shadows enabled")]
    public float shadowCullBuffer = 4f;

    [Tooltip("Disable all shadow casters entirely when night (intensity below this)")]
    [Range(0f, 1f)]
    public float shadowNightThreshold = 0.1f;

    // ── Default gradients (called on asset creation + inspector Reset) ──

    void Reset()
    {
        // Sunny: dark-blue night → warm-orange sunrise → white noon → orange sunset → dark-blue
        sunnyColor = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(0.10f, 0.10f, 0.25f), 0.00f),  // midnight
                new GradientColorKey(new Color(0.10f, 0.10f, 0.25f), 0.22f),  // pre-dawn
                new GradientColorKey(new Color(1.00f, 0.65f, 0.30f), 0.28f),  // sunrise
                new GradientColorKey(new Color(1.00f, 0.98f, 0.90f), 0.40f),  // morning
                new GradientColorKey(Color.white,                    0.50f),   // noon
                new GradientColorKey(new Color(1.00f, 0.98f, 0.90f), 0.65f),  // afternoon
                new GradientColorKey(new Color(1.00f, 0.50f, 0.20f), 0.77f),  // sunset
                new GradientColorKey(new Color(0.10f, 0.10f, 0.25f), 0.83f),  // dusk
            },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };

        // Rainy: desaturated, grey-blue tones
        rainyColor = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(new Color(0.08f, 0.08f, 0.18f), 0.00f),
                new GradientColorKey(new Color(0.08f, 0.08f, 0.18f), 0.22f),
                new GradientColorKey(new Color(0.50f, 0.55f, 0.60f), 0.30f),
                new GradientColorKey(new Color(0.65f, 0.70f, 0.75f), 0.50f),
                new GradientColorKey(new Color(0.50f, 0.55f, 0.60f), 0.70f),
                new GradientColorKey(new Color(0.08f, 0.08f, 0.18f), 0.82f),
            },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        };
    }
}
