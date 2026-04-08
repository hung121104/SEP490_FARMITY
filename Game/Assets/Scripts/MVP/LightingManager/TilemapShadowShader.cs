using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Shader-based projected shadow for 2D tilemaps (URP).
///
/// Works like SpriteShadowShader but targets a Tilemap instead of a single
/// SpriteRenderer.  For every non-null tile it spawns a child SpriteRenderer
/// driven by the same "Custom/ProjectedShadow2D" shader, positioned at the
/// tile's world centre.
///
/// How to use:
///   1. Create a Material using the "Custom/ProjectedShadow2D" shader.
///   2. Add this component to the same GameObject as the Tilemap (or assign
///      the Tilemap reference in the Inspector).
///   3. Assign the shadow material.
///   4. DayNightCycleConfig is resolved automatically from DayNightCycleManager.
///
/// Tile scanning happens once during initialisation and whenever
/// <see cref="RebuildShadows"/> is called (e.g. after the tilemap changes).
/// Shadow lean / intensity is updated every frame from DayNightCycleManager.DayProgress.
/// </summary>
public class TilemapShadowShader : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional: pin a specific DayNightCycleConfig. If left empty, the config is " +
             "fetched automatically from DayNightCycleManager.Instance at runtime.")]
    [SerializeField] private DayNightCycleConfig config;
    [Tooltip("Material using the Custom/ProjectedShadow2D shader.")]
    [SerializeField] private Material shadowMaterial;
    [Tooltip("The Tilemap to shadow. Leave empty to auto-detect on this GameObject.")]
    [SerializeField] private Tilemap sourceTilemap;

    [Header("Shadow Appearance")]
    [SerializeField] private Color shadowColor = new Color(0.02f, 0.02f, 0.08f, 0.45f);
    [Tooltip("Sorting layer for shadow renderers (e.g. 'Ground').")]
    [SerializeField] private string shadowSortingLayer = "Ground";
    [Tooltip("Sorting order within the shadow sorting layer.")]
    [SerializeField] private int shadowSortingOrder = 100;

    [Header("Shadow Tuning")]
    [Tooltip("Overall shadow length. 1 = natural geometric length.")]
    [SerializeField] private float shadowLengthScale = 1f;
    [Tooltip("How flat the shadow appears. 0 = fully flat, 1 = same height as sprite.")]
    [SerializeField, Range(0f, 1f)] private float shadowFlattenY = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // ── Cached shader property IDs ──────────────────────────────────────
    private static readonly int ID_ShadowColor    = Shader.PropertyToID("_ShadowColor");
    private static readonly int ID_ShadowLeanX    = Shader.PropertyToID("_ShadowLeanX");
    private static readonly int ID_ShadowScaleX   = Shader.PropertyToID("_ShadowScaleX");
    private static readonly int ID_ShadowAlpha    = Shader.PropertyToID("_ShadowAlpha");
    private static readonly int ID_ShadowFlattenY = Shader.PropertyToID("_ShadowFlattenY");
    private static readonly int ID_ShadowYOffset  = Shader.PropertyToID("_ShadowYOffset");

    // ── Per-tile shadow data ────────────────────────────────────────────
    private struct TileShadow
    {
        public SpriteRenderer renderer;
        public MaterialPropertyBlock mpb;
        public Sprite sprite;
        public float cullShiftY;
    }

    private readonly List<TileShadow> _shadows = new List<TileShadow>();
    private Transform _shadowRoot;

    // ── Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        TryFindTilemap();
        TryFillConfig();

        if (sourceTilemap != null)
            BuildShadows();
        else if (showDebugLogs)
            Debug.LogWarning($"[TilemapShadow] {name} Awake — tilemap not found yet");
    }

    void OnEnable()
    {
        TryFillConfig();
        if (sourceTilemap == null) TryFindTilemap();

        if (sourceTilemap != null && (_shadowRoot == null || _shadows.Count == 0))
            BuildShadows();
        else
            SetAllEnabled(true);
    }

    void OnDisable()
    {
        SetAllEnabled(false);
    }

    void OnDestroy()
    {
        if (_shadowRoot != null)
            Destroy(_shadowRoot.gameObject);
    }

    void LateUpdate()
    {
        if (sourceTilemap == null)
        {
            TryFindTilemap();
            if (sourceTilemap == null) return;
        }

        if (_shadowRoot == null || _shadows.Count == 0)
            BuildShadows();

        TryFillConfig();

        if (DayNightCycleManager.Instance == null || config == null || shadowMaterial == null)
        {
            SetAllEnabled(false);
            return;
        }

        float t         = DayNightCycleManager.Instance.DayProgress;
        float intensity = config.sunShadowIntensity.Evaluate(t);

        // Sun angle → lean
        float angleDeg = Mathf.Lerp(config.sunriseAngle, config.sunsetAngle, t);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        float sinA     = Mathf.Max(Mathf.Abs(Mathf.Sin(angleRad)), 0.1f);
        float cosA     = Mathf.Cos(angleRad);
        float leanX    = (cosA / sinA) * shadowLengthScale;

        bool shouldBeEnabled = intensity >= 0.01f;

        for (int i = 0; i < _shadows.Count; i++)
        {
            var ts = _shadows[i];
            if (ts.renderer == null) continue;

            // Re-lock material if something replaced it
            if (ts.renderer.sharedMaterial != shadowMaterial)
                ts.renderer.sharedMaterial = shadowMaterial;

            ts.mpb.SetColor(ID_ShadowColor,    shadowColor);
            ts.mpb.SetFloat(ID_ShadowLeanX,    leanX);
            ts.mpb.SetFloat(ID_ShadowScaleX,   1f);
            ts.mpb.SetFloat(ID_ShadowAlpha,    shadowColor.a * intensity);
            ts.mpb.SetFloat(ID_ShadowFlattenY, shadowFlattenY);
            ts.mpb.SetFloat(ID_ShadowYOffset,  ts.cullShiftY);
            ts.renderer.SetPropertyBlock(ts.mpb);
            ts.renderer.enabled = shouldBeEnabled;
        }

        if (showDebugLogs && Time.frameCount % 60 == 0)
            Debug.Log($"[TilemapShadow] {name} LateUpdate — t={t:F3}, intensity={intensity:F3}, leanX={leanX:F3}, tiles={_shadows.Count}, enabled={shouldBeEnabled}");
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// Rebuilds all shadow renderers from the current tilemap state.
    /// Call this after adding/removing tiles at runtime.
    /// </summary>
    public void RebuildShadows()
    {
        ClearShadows();
        BuildShadows();
    }

    // ── Internals ───────────────────────────────────────────────────────

    private void TryFindTilemap()
    {
        if (sourceTilemap != null) return;
        sourceTilemap = GetComponent<Tilemap>();
        if (sourceTilemap == null)
            sourceTilemap = GetComponentInChildren<Tilemap>();
        if (showDebugLogs)
            Debug.Log($"[TilemapShadow] {name} TryFindTilemap — {(sourceTilemap != null ? sourceTilemap.name : "null")}");
    }

    private void TryFillConfig()
    {
        if (config == null && DayNightCycleManager.Instance != null)
            config = DayNightCycleManager.Instance.Config;
    }

    private void BuildShadows()
    {
        if (sourceTilemap == null) return;

        // Create a single root object to parent all shadow renderers.
        if (_shadowRoot == null)
        {
            Transform existing = transform.Find("_TilemapShadows");
            if (existing != null) Destroy(existing.gameObject);

            var rootGo = new GameObject("_TilemapShadows");
            rootGo.transform.SetParent(transform, false);
            rootGo.transform.localPosition = Vector3.zero;
            rootGo.transform.localScale    = Vector3.one;
            rootGo.transform.localRotation = Quaternion.identity;
            _shadowRoot = rootGo.transform;
        }

        sourceTilemap.CompressBounds();
        BoundsInt bounds = sourceTilemap.cellBounds;

        // Use real DayProgress when available; fall back to noon
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
            intensity = 1f;
            leanX     = 0f;
        }

        int count = 0;
        foreach (Vector3Int cellPos in bounds.allPositionsWithin)
        {
            Sprite tileSprite = sourceTilemap.GetSprite(cellPos);
            if (tileSprite == null) continue;

            // World position of the tile centre
            Vector3 worldPos = sourceTilemap.GetCellCenterWorld(cellPos);

            var ts = CreateTileShadow(tileSprite, worldPos, intensity, leanX);
            _shadows.Add(ts);
            count++;
        }

        if (showDebugLogs)
            Debug.Log($"[TilemapShadow] {name} BuildShadows — created {count} shadow(s) from tilemap bounds {bounds}");
    }

    private TileShadow CreateTileShadow(Sprite sprite, Vector3 worldPos, float intensity, float leanX)
    {
        var go = new GameObject("_TileShadow");
        go.transform.SetParent(_shadowRoot, false);
        // Position in world space relative to the shadow root
        go.transform.position = worldPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = sprite;
        sr.sharedMaterial    = shadowMaterial;
        sr.sortingLayerName  = shadowSortingLayer;
        sr.sortingOrder      = shadowSortingOrder;

        // Culling shift — same logic as SpriteShadowShader
        float spriteHeight = sprite.bounds.size.y;
        float cullShiftY   = spriteHeight * (0.5f + 0.5f * shadowFlattenY);
        Vector3 localPos   = go.transform.localPosition;
        localPos.y        -= cullShiftY;
        go.transform.localPosition = localPos;

        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(ID_ShadowColor,    shadowColor);
        mpb.SetFloat(ID_ShadowLeanX,    leanX);
        mpb.SetFloat(ID_ShadowScaleX,   1f);
        mpb.SetFloat(ID_ShadowAlpha,    shadowColor.a * intensity);
        mpb.SetFloat(ID_ShadowFlattenY, shadowFlattenY);
        mpb.SetFloat(ID_ShadowYOffset,  cullShiftY);
        sr.SetPropertyBlock(mpb);
        sr.enabled = (intensity >= 0.01f);

        return new TileShadow
        {
            renderer   = sr,
            mpb        = mpb,
            sprite     = sprite,
            cullShiftY = cullShiftY
        };
    }

    private void ClearShadows()
    {
        _shadows.Clear();
        if (_shadowRoot != null)
        {
            Destroy(_shadowRoot.gameObject);
            _shadowRoot = null;
        }
    }

    private void SetAllEnabled(bool enabled)
    {
        for (int i = 0; i < _shadows.Count; i++)
        {
            if (_shadows[i].renderer != null)
                _shadows[i].renderer.enabled = enabled;
        }
    }
}
