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

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // ── Cached shader property IDs (set once, reused every frame) ──────────
    private static readonly int ID_ShadowColor    = Shader.PropertyToID("_ShadowColor");
    private static readonly int ID_ShadowLeanX    = Shader.PropertyToID("_ShadowLeanX");
    private static readonly int ID_ShadowScaleX   = Shader.PropertyToID("_ShadowScaleX");
    private static readonly int ID_ShadowAlpha    = Shader.PropertyToID("_ShadowAlpha");
    private static readonly int ID_ShadowFlattenY = Shader.PropertyToID("_ShadowFlattenY");
    private static readonly int ID_ShadowYOffset  = Shader.PropertyToID("_ShadowYOffset");

    // ── Internal state ──────────────────────────────────────────────────────
    private SpriteRenderer        _source;
    private SpriteRenderer        _shadow;
    private MaterialPropertyBlock _mpb;
    private float                 _cullShiftY;
    // Last non-null sprite: keeps the shadow visible during brief null windows
    // (e.g. the one frame a DynamicSpriteSwapper clears its renderer while
    // switching configId, or while the skin catalog is still loading).
    private Sprite                _lastValidSprite;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} Awake");
        _mpb = new MaterialPropertyBlock();
        TryFindSource();
        TryFillConfig();
        if (_source != null)
        {
            CreateShadowRenderer();
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} Awake — source renderer not found yet");
        }
    }

    void OnEnable()
    {
        if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} OnEnable — source={(  _source != null ? _source.name : "null")}, shadow={(_shadow != null ? _shadow.gameObject.name : "null")}, config={( config != null ? config.name : "null")}");
        // Re-fill config in case the manager wasn't ready during Awake.
        TryFillConfig();
        // Try source resolution again in case it failed at Awake time
        // (e.g. catalog-loaded prefab where children weren't ready).
        if (_source == null) TryFindSource();
        // Recreate the shadow child if it was destroyed while disabled.
        if (_source != null && (_shadow == null || _shadow.gameObject == null))
        {
            if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} OnEnable — recreating destroyed shadow child");
            CreateShadowRenderer(); // PrimeShadowState is called inside CreateShadowRenderer
        }
        else
        {
            // Shadow already exists — re-prime it in case the sprite changed while
            // the component was disabled (e.g. skin swap, chunk re-spawn).
            PrimeShadowState();
        }
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
            if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} TryFindSource — using Inspector override: {_source.name}");
            return;
        }

        // 2. Same GameObject (character prefabs, etc.)
        _source = GetComponent<SpriteRenderer>();
        if (_source != null)
        {
            if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} TryFindSource — found on same GO: {_source.name}");
            return;
        }

        // 3. Child search — skip any "_SpriteShadowShader" child we created ourselves
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject.name != "_SpriteShadowShader")
            {
                _source = sr;
                if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} TryFindSource — found in children: {_source.name}");
                return;
            }
        }

        if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} TryFindSource — no SpriteRenderer found");
    }

    private void TryFillConfig()
    {
        if (config == null && DayNightCycleManager.Instance != null)
        {
            config = DayNightCycleManager.Instance.Config;
            if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} TryFillConfig — resolved config: {(config != null ? config.name : "null")}");
        }
        else if (config == null)
        {
            if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} TryFillConfig — DayNightCycleManager not available yet");
        }
    }

    private void CreateShadowRenderer()
    {
        // Shadow is parented to _source's transform so it co-locates with the
        // sprite even when SpriteShadowShader lives on a parent root (resource prefabs).
        Transform shadowParent = _source != null ? _source.transform : transform;

        // Clean up any leftover child from a previous call.
        Transform existing = shadowParent.Find("_SpriteShadowShader");
        if (existing != null)
        {
            if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} CreateShadowRenderer — destroying leftover shadow child");
            Destroy(existing.gameObject);
        }

        var go = new GameObject("_SpriteShadowShader");
        go.transform.SetParent(shadowParent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        _shadow                  = go.AddComponent<SpriteRenderer>();
        _shadow.sharedMaterial   = shadowMaterial;
        _shadow.sortingLayerName = shadowSortingLayer;
        _shadow.sortingOrder     = shadowSortingOrder;

        if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} CreateShadowRenderer — created under '{shadowParent.name}', material={(shadowMaterial != null ? shadowMaterial.name : "null")}, layer={shadowSortingLayer}, order={shadowSortingOrder}");

        // Eagerly prime sprite + MPB so the shadow is visible immediately —
        // before LateUpdate has ever run (editor context-menu instantiation,
        // first spawn frame, object re-enabled after a disable).
        PrimeShadowState();
    }

    /// <summary>
    /// Assigns the current source sprite to the shadow renderer and writes an
    /// initial MaterialPropertyBlock so the shadow renders correctly without
    /// waiting for the first LateUpdate.  Falls back to noon defaults when
    /// DayNightCycleManager / config are not yet available (edit-mode, early
    /// startup). Safe to call from Awake, OnEnable, and CreateShadowRenderer.
    /// </summary>
    private void PrimeShadowState()
    {
        if (_shadow == null || _source == null) return;
        if (_source.sprite == null) return;   // sprite not ready yet — LateUpdate will pick it up

        _lastValidSprite         = _source.sprite;
        _shadow.sprite           = _source.sprite;
        _shadow.flipX            = _source.flipX;
        _shadow.sortingLayerName = shadowSortingLayer;
        _shadow.sortingOrder     = shadowSortingOrder;
        UpdateCullShift(_lastValidSprite);

        if (shadowMaterial != null && _shadow.sharedMaterial != shadowMaterial)
            _shadow.sharedMaterial = shadowMaterial;

        // Use real DayProgress when available; fall back to noon (t=0.5) so the
        // shadow is visible by default when no manager exists (e.g. edit mode).
        float t = (DayNightCycleManager.Instance != null)
            ? DayNightCycleManager.Instance.DayProgress
            : 0.5f;

        float intensity;
        float leanX;
        if (config != null)
        {
            intensity      = config.sunShadowIntensity.Evaluate(t);
            float angleDeg = Mathf.Lerp(config.sunriseAngle, config.sunsetAngle, t);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float sinA     = Mathf.Max(Mathf.Abs(Mathf.Sin(angleRad)), 0.1f);
            float cosA     = Mathf.Cos(angleRad);
            leanX          = (cosA / sinA) * shadowLengthScale;
        }
        else
        {
            intensity = 1f;  // visible by default when config not yet loaded
            leanX     = 0f;  // straight-down noon shadow
        }

        _mpb.SetColor(ID_ShadowColor,    shadowColor);
        _mpb.SetFloat(ID_ShadowLeanX,    leanX);
        _mpb.SetFloat(ID_ShadowScaleX,   1f);
        _mpb.SetFloat(ID_ShadowAlpha,    shadowColor.a * intensity);
        _mpb.SetFloat(ID_ShadowFlattenY, shadowFlattenY);
        _mpb.SetFloat(ID_ShadowYOffset,  _cullShiftY);
        _shadow.SetPropertyBlock(_mpb);
        _shadow.enabled = (intensity >= 0.01f);

        if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} PrimeShadowState — sprite='{_lastValidSprite.name}', t={t:F3}, intensity={intensity:F3}, leanX={leanX:F3}, enabled={_shadow.enabled}");
    }

    void LateUpdate()
    {
        // ── 1. Ensure source renderer is resolved ───────────────────────────
        // On resource prefabs, children may not be ready during Awake on the
        // same frame as instantiation.  Retry here until found.
        if (_source == null)
        {
            TryFindSource();
            if (_source == null)
            {
                if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} LateUpdate — source still null, skipping");
                return; // still not ready
            }
        }

        // ── 2. Ensure the shadow child exists ───────────────────────────────
        if (_shadow == null || _shadow.gameObject == null)
        {
            if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} LateUpdate — shadow child missing, recreating");
            CreateShadowRenderer();
        }

        // ── 3. Lazy config resolution ───────────────────────────────────────
        // DayNightCycleManager may not have been ready during Awake/OnEnable.
        TryFillConfig();

        if (DayNightCycleManager.Instance == null || config == null || shadowMaterial == null)
        {
            if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} LateUpdate — missing dependency: DayNightCycleMgr={(DayNightCycleManager.Instance != null)}, config={(config != null)}, material={(shadowMaterial != null)} — disabling shadow");
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
            if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} LateUpdate — no valid sprite yet on source '{_source.name}', disabling shadow");
            _shadow.enabled = false;
            return;
        }

        // ── 4. Sample intensity (do NOT early-return here) ──────────────────
        // Returning early before applying sprite + MPB would leave a freshly-
        // spawned shadow renderer completely uninitialized.  When intensity
        // later rises above the threshold (e.g. dawn after an OnDayChanged
        // midnight reload), the SRP Batcher initialises the per-renderer
        // constant buffer from the material's defaults instead of the stored
        // property block, making the shadow invisible until something external
        // (like an Inspector edit) forces a rebatch.  Always keep the shadow
        // renderer primed with current data and toggle visibility at the end.
        float t         = DayNightCycleManager.Instance.DayProgress;
        float intensity = config.sunShadowIntensity.Evaluate(t);

        // ── 5. Keep material locked to the assigned shadow material ─────────
        // Something (chunk reload, renderer reset) may have replaced it with the
        // default sprite material.
        if (_shadow.sharedMaterial != shadowMaterial)
        {
            if (showDebugLogs) Debug.LogWarning($"[SpriteShadow] {name} LateUpdate — shadow material was replaced, restoring '{shadowMaterial.name}'");
            _shadow.sharedMaterial = shadowMaterial;
        }

        _shadow.sprite           = _lastValidSprite;
        _shadow.flipX            = _source.flipX;
        _shadow.sortingLayerName = shadowSortingLayer;
        _shadow.sortingOrder     = shadowSortingOrder;
        UpdateCullShift(_lastValidSprite);

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

        if (showDebugLogs && Time.frameCount % 60 == 0)
            Debug.Log($"[SpriteShadow] {name} LateUpdate — t={t:F3}, intensity={intensity:F3}, sprite='{_lastValidSprite.name}', angle={angleDeg:F1}°, leanX={leanX:F3}, enabled={(intensity >= 0.01f)}");

        // ── 7. Push all values to the shader via MaterialPropertyBlock ───────
        // Always write the MPB before toggling enabled so the SRP Batcher's
        // per-renderer CBuffer is always up-to-date (_ShadowAlpha becomes 0
        // naturally when intensity is 0, keeping the shadow invisible without
        // disabling the renderer's GPU-side state).
        _mpb.SetColor(ID_ShadowColor,    shadowColor);
        _mpb.SetFloat(ID_ShadowLeanX,    leanX);
        _mpb.SetFloat(ID_ShadowScaleX,   1f);
        _mpb.SetFloat(ID_ShadowAlpha,    shadowColor.a * intensity);
        _mpb.SetFloat(ID_ShadowFlattenY, shadowFlattenY);
        _mpb.SetFloat(ID_ShadowYOffset,  _cullShiftY);
        _shadow.SetPropertyBlock(_mpb);

        // ── 8. Toggle visibility AFTER the MPB is fully written ─────────────
        bool shouldBeEnabled = (intensity >= 0.01f);
        if (showDebugLogs && _shadow.enabled != shouldBeEnabled)
            Debug.Log($"[SpriteShadow] {name} LateUpdate — visibility changed: {_shadow.enabled} → {shouldBeEnabled} (intensity={intensity:F3})");
        _shadow.enabled = shouldBeEnabled;
    }

    /// <summary>
    /// Moves the shadow renderer transform downward so Unity's CPU culling
    /// bounds cover the projected area. The shader then adds the same amount
    /// back to keep the final shadow position unchanged.
    /// </summary>
    private void UpdateCullShift(Sprite sprite)
    {
        if (_shadow == null || sprite == null) return;

        float spriteHeight = sprite.bounds.size.y;
        _cullShiftY = spriteHeight * (0.5f + 0.5f * shadowFlattenY);

        Vector3 localPos = _shadow.transform.localPosition;
        localPos.y = -_cullShiftY;
        _shadow.transform.localPosition = localPos;
    }

    void OnDisable()
    {
        if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} OnDisable — hiding shadow renderer");
        // Hide the shadow renderer when this component is disabled so it does not
        // ghost-render while LateUpdate is no longer running.
        if (_shadow != null && _shadow.gameObject != null)
            _shadow.enabled = false;
    }

    void OnDestroy()
    {
        if (showDebugLogs) Debug.Log($"[SpriteShadow] {name} OnDestroy — destroying shadow child");
        if (_shadow != null)
            Destroy(_shadow.gameObject);
    }
}

