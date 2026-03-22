using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Moves a Spot / Point Light 2D along a semicircular arc to simulate
/// a moving sun, producing directional shadows via ShadowCaster2D.
/// Reads DayProgress from DayNightCycleManager.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class SunLight2DController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayNightCycleConfig config;

    [Header("Pivot")]
    [Tooltip("World-space centre of the sun arc (usually the map centre)")]
    [SerializeField] private Transform arcPivot;

    private Light2D _light;

    void Awake()
    {
        _light = GetComponent<Light2D>();
    }

    void LateUpdate()
    {
        if (DayNightCycleManager.Instance == null || config == null)
            return;

        float t = DayNightCycleManager.Instance.DayProgress;

        // ── intensity ──
        _light.intensity = config.sunSpotIntensity.Evaluate(t);

        // ── radius & angle (must be large enough to reach the ground) ──
        _light.pointLightOuterRadius = config.sunLightOuterRadius;
        _light.pointLightInnerRadius = config.sunLightOuterRadius * 0.5f;
        _light.pointLightOuterAngle  = config.sunLightOuterAngle;
        _light.pointLightInnerAngle  = config.sunLightOuterAngle * 0.8f;

        // ── shadow control (fade to 0 at dawn/dusk to hide infinite stretch) ──
        _light.shadowIntensity = config.sunShadowIntensity.Evaluate(t);
        _light.shadowSoftness  = config.sunShadowSoftness;

        // ── position along arc ──
        // Lerp from sunsetAngle (left/east) → sunriseAngle (right/west) so the
        // visual sun travels LEFT → RIGHT across the sky, while the shadow shader
        // (which uses sunriseAngle→sunsetAngle) casts shadows RIGHT → LEFT.
        float angleDeg = Mathf.Lerp(config.sunsetAngle, config.sunriseAngle, t);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector3 pivot = arcPivot != null ? arcPivot.position : Vector3.zero;
        Vector3 offset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * config.sunArcRadius;
        transform.position = pivot + offset;

        // ── rotation (point toward pivot so shadows fall away) ──
        Vector3 dir = pivot - transform.position;
        float zRot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, zRot);
    }
}
