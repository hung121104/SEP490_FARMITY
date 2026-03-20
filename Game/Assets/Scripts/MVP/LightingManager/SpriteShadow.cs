using UnityEngine;

/// <summary>
/// Simple fake shadow: duplicates the parent SpriteRenderer,
/// tints it dark, flips Y = -1, and rotates Z with the sun angle.
/// Attach to any GameObject that has a SpriteRenderer.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteShadow : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private DayNightCycleConfig config;

    [Header("Shadow Appearance")]
    [SerializeField] private Color shadowColor = new Color(0.02f, 0.02f, 0.08f, 0.45f);
    [SerializeField] private int sortingOrderOffset = -1;

    private SpriteRenderer _source;
    private SpriteRenderer _shadow;
    private Transform      _shadowTf;

    void Awake()
    {
        _source = GetComponent<SpriteRenderer>();

        var go = new GameObject("_SpriteShadow");
        go.transform.SetParent(transform, false);
        _shadowTf = go.transform;

        _shadow = go.AddComponent<SpriteRenderer>();
        _shadow.color          = shadowColor;
        _shadow.sortingLayerID = _source.sortingLayerID;
        _shadow.sortingOrder   = _source.sortingOrder + sortingOrderOffset;
        _shadow.material       = _source.sharedMaterial;
    }

    void LateUpdate()
    {
        if (DayNightCycleManager.Instance == null || config == null)
        {
            _shadow.enabled = false;
            return;
        }

        float t = DayNightCycleManager.Instance.DayProgress;

        // Fade out when intensity is 0 (night / dawn edges)
        float intensity = config.sunShadowIntensity.Evaluate(t);
        if (intensity < 0.01f)
        {
            _shadow.enabled = false;
            return;
        }
        _shadow.enabled = true;

        // Sync animated sprite
        _shadow.sprite = _source.sprite;
        _shadow.flipX  = _source.flipX;

        // Sun angle drives Z rotation: sunrise (170°) → sunset (10°)
        float sunAngleDeg = Mathf.Lerp(config.sunriseAngle, config.sunsetAngle, t);

        // X scale: 0.5 at dawn/dusk, 1.0 at noon — peaks at DayProgress 0.5
        // Uses shadow window midpoints: active ~0.25 (sunrise) to ~0.75 (sunset)
        float centerDist = Mathf.Clamp01(1f - Mathf.Abs((t - 0.5f) / 0.25f));
        float xScale = Mathf.Lerp(config.shadowXScaleDawn, config.shadowXScaleNoon, centerDist);

        // Y = -1 flips the sprite; Z rotation sweeps the shadow across the day
        _shadowTf.localScale    = new Vector3(xScale, -1f, 1f);
        _shadowTf.localRotation = Quaternion.Euler(0f, 0f, sunAngleDeg);

        // Alpha from intensity curve
        Color c = shadowColor;
        c.a = shadowColor.a * intensity;
        _shadow.color = c;

        // Keep sorting in sync if source changes at runtime
        _shadow.sortingLayerID = _source.sortingLayerID;
        _shadow.sortingOrder   = _source.sortingOrder + sortingOrderOffset;
    }

    void OnDestroy()
    {
        if (_shadowTf != null)
            Destroy(_shadowTf.gameObject);
    }
}
