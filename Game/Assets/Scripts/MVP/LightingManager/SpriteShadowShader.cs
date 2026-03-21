using UnityEngine;

/// <summary>
/// Shader-based projected shadow for 2D sprites (URP).
///
/// Creates a child SpriteRenderer driven by the "Custom/ProjectedShadow2D"
/// shader, which shears the sprite silhouette in the sun direction rather
/// than simply flipping it — producing a more convincing projected-shadow.
///
/// How to use:
///   1. Create a Material using the "Custom/ProjectedShadow2D" shader.
///   2. Replace SpriteShadow on your character prefab with this component.
///   3. Assign the material in the Inspector.
///   4. DayNightCycleConfig is found automatically from DayNightCycleManager.
///      You can also pin a config override in the Inspector if needed.
///
/// Shadow shape is driven by DayNightCycleManager.DayProgress each frame;
/// no manual input is required beyond the initial setup.
/// </summary>
// Run very late in LateUpdate so DynamicSpriteSwapper (and the Animator) have
// already written the final sprite for this frame before we copy it.
[DefaultExecutionOrder(9000)]
public class SpriteShadowShader : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional: pin a specific DayNightCycleConfig. If left empty, the config is " +
             "fetched automatically from DayNightCycleManager.Instance at runtime.")]
    [SerializeField] private DayNightCycleConfig config;
    [Tooltip("Material using the Custom/ProjectedShadow2D shader.")]
    [SerializeField] private Material shadowMaterial;
    [Tooltip("The SpriteRenderer whose sprite is shadowed. Leave empty to auto-detect:\n" +
             "• Same GameObject: uses GetComponent<SpriteRenderer>()\n" +
             "• No SR on this GameObject: uses GetComponentInChildren (e.g. resource prefabs\n" +
             "  where the renderer is on a child like 'ResourceSpriteRender').\n" +
             "Set this explicitly when auto-detection picks the wrong renderer.")]
    [SerializeField] private SpriteRenderer sourceRenderer;

    [Header("Shadow Appearance")]
    [SerializeField] private Color shadowColor        = new Color(0.02f, 0.02f, 0.08f, 0.45f);
    [Tooltip("Sorting layer for the shadow renderer. Use a layer that renders " +
             "above the ground tiles but below entities (e.g. 'Ground').")]
    [SerializeField] private string shadowSortingLayer = "Ground";
    [Tooltip("Sorting order within the shadow sorting layer. Use a high value " +
             "so the shadow draws above all ground tiles.")]
    [SerializeField] private int    shadowSortingOrder = 100;

    [Header("Shadow Tuning")]
    [Tooltip("Overall shadow length. 1 = natural geometric length. Increase for longer shadows.")]
    [SerializeField] private float shadowLengthScale = 1f;
    [Tooltip("How flat the shadow appears on the ground. 0 = fully flat, 1 = same height as sprite.")]
    [SerializeField, Range(0f, 1f)] private float shadowFlattenY = 0.5f;

    // ── Cached shader property IDs (set once, reused every frame) ──────────
    private static readonly int ID_ShadowColor    = Shader.PropertyToID("_ShadowColor");
    private static readonly int ID_ShadowLeanX    = Shader.PropertyToID("_ShadowLeanX");
    private static readonly int ID_ShadowScaleX   = Shader.PropertyToID("_ShadowScaleX");
    private static readonly int ID_ShadowAlpha    = Shader.PropertyToID("_ShadowAlpha");
    private static readonly int ID_ShadowFlattenY = Shader.PropertyToID("_ShadowFlattenY");

    // ── Internal state ──────────────────────────────────────────────────────
    private SpriteRenderer        _source;
    private SpriteRenderer        _shadow;
    private MaterialPropertyBlock _mpb;
    // Last non-null sprite: keeps the shadow visible during brief null windows
    // (e.g. the one frame a DynamicSpriteSwapper clears its renderer while
    // switching configId, or while the skin catalog is still loading).
    private Sprite                _lastValidSprite;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        TryFindSource();
        TryFillConfig();
        if (_source != null)
            CreateShadowRenderer();
    }

    void OnEnable()
    {
        // Re-fill config in case the manager wasn't ready during Awake.
        TryFillConfig();
        // Try source resolution again in case it failed at Awake time
        // (e.g. catalog-loaded prefab where children weren't ready).
        if (_source == null) TryFindSource();
        // Recreate the shadow child if it was destroyed while disabled.
        if (_source != null && (_shadow == null || _shadow.gameObject == null))
            CreateShadowRenderer();
    }

    /// <summary>
    /// Resolves which SpriteRenderer to shadow.
    /// Priority: Inspector field → GetComponent (same GO) → GetComponentInChildren (skip
    /// the shadow child itself).  This allows placing SpriteShadowShader on a parent root
    /// when the actual renderer lives on a child (e.g. resource prefabs with a
    /// "ResourceSpriteRender" child that gets its sprite assigned by the catalog loader).
    /// </summary>
    private void TryFindSource()
    {
        if (_source != null) return;

        // 1. Explicit override set in Inspector
        if (sourceRenderer != null)
        {
            _source = sourceRenderer;
            return;
        }

        // 2. Same GameObject (character prefabs, etc.)
        _source = GetComponent<SpriteRenderer>();
        if (_source != null) return;

        // 3. Child search — skip any "_SpriteShadow" child we created ourselves
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject.name != "_SpriteShadow")
            {
                _source = sr;
                return;
            }
        }
    }

    private void TryFillConfig()
    {
        if (config == null && DayNightCycleManager.Instance != null)
            config = DayNightCycleManager.Instance.Config;
    }

    private void CreateShadowRenderer()
    {
        // Shadow is parented to _source's transform so it co-locates with the
        // sprite even when SpriteShadowShader lives on a parent root (resource prefabs).
        Transform shadowParent = _source != null ? _source.transform : transform;

        // Clean up any leftover child from a previous call.
        Transform existing = shadowParent.Find("_SpriteShadow");
        if (existing != null)
            Destroy(existing.gameObject);

        var go = new GameObject("_SpriteShadow");
        go.transform.SetParent(shadowParent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        _shadow                  = go.AddComponent<SpriteRenderer>();
        _shadow.sharedMaterial   = shadowMaterial;
        _shadow.sortingLayerName = shadowSortingLayer;
        _shadow.sortingOrder     = shadowSortingOrder;
    }

    void LateUpdate()
    {
        // ── 1. Ensure source renderer is resolved ───────────────────────────
        // On resource prefabs, children may not be ready during Awake on the
        // same frame as instantiation.  Retry here until found.
        if (_source == null)
        {
            TryFindSource();
            if (_source == null) return; // still not ready
        }

        // ── 2. Ensure the shadow child exists ───────────────────────────────
        if (_shadow == null || _shadow.gameObject == null)
            CreateShadowRenderer();

        // ── 3. Lazy config resolution ───────────────────────────────────────
        // DayNightCycleManager may not have been ready during Awake/OnEnable.
        TryFillConfig();

        if (DayNightCycleManager.Instance == null || config == null || shadowMaterial == null)
        {
            _shadow.enabled = false;
            return;
        }

        // ── 3. Sprite sync — use last valid sprite to survive brief null windows
        // DynamicSpriteSwapper briefly nulls its renderer when switching configId
        // or when the skin catalog is still loading.  Holding the last valid sprite
        // keeps the shadow silhouette stable instead of blinking out for one frame.
        if (_source.sprite != null)
            _lastValidSprite = _source.sprite;

        if (_lastValidSprite == null)
        {
            // No sprite has ever been set; nothing meaningful to shadow yet.
            _shadow.enabled = false;
            return;
        }

        // ── 4. Intensity check ──────────────────────────────────────────────
        float t         = DayNightCycleManager.Instance.DayProgress;
        float intensity = config.sunShadowIntensity.Evaluate(t);

        if (intensity < 0.01f)
        {
            _shadow.enabled = false;
            return;
        }

        _shadow.enabled = true;

        // ── 5. Keep material locked to the assigned shadow material ─────────
        // Something (chunk reload, renderer reset) may have replaced it with the
        // default sprite material.
        if (_shadow.sharedMaterial != shadowMaterial)
            _shadow.sharedMaterial = shadowMaterial;

        _shadow.sprite           = _lastValidSprite;
        _shadow.flipX            = _source.flipX;
        _shadow.sortingLayerName = shadowSortingLayer;
        _shadow.sortingOrder     = shadowSortingOrder;

        // ── 6. Compute shadow lean from the current sun angle ───────────────
        // Arc runs from sunriseAngle (170°, upper-left) through 90° (noon, top)
        // to sunsetAngle (10°, upper-right).
        float angleDeg = Mathf.Lerp(config.sunriseAngle, config.sunsetAngle, t);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        // leanX = cot(elevation) × length scale
        // Shadow travels LEFT → straight down → RIGHT as the sun sweeps left→right.
        float sinA  = Mathf.Max(Mathf.Abs(Mathf.Sin(angleRad)), 0.1f);
        float cosA  = Mathf.Cos(angleRad);
        float leanX = (cosA / sinA) * shadowLengthScale;

        // ── 7. Push all values to the shader via MaterialPropertyBlock ───────
        _mpb.SetColor(ID_ShadowColor,    shadowColor);
        _mpb.SetFloat(ID_ShadowLeanX,    leanX);
        _mpb.SetFloat(ID_ShadowScaleX,   1f);
        _mpb.SetFloat(ID_ShadowAlpha,    shadowColor.a * intensity);
        _mpb.SetFloat(ID_ShadowFlattenY, shadowFlattenY);
        _shadow.SetPropertyBlock(_mpb);
    }

    void OnDestroy()
    {
        if (_shadow != null)
            Destroy(_shadow.gameObject);
    }
}

