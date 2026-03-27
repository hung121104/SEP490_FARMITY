using UnityEngine;

/// <summary>
/// Drives the CloudShadow2D shader on a camera-following quad.
///
/// How to use:
///   1. Create a Material using "Custom/CloudShadow2D" shader.
///   2. Attach this component to any GameObject in the scene.
///   3. Assign the material in the Inspector.
///   4. The cnoise texture is generated automatically at startup — no manual
///      texture wiring needed.
///
/// The quad is positioned just above the ground sorting layer so it darkens
/// terrain and objects below the player layer.
/// </summary>
public class CloudShadowController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Material using the Custom/CloudShadow2D shader.")]
    [SerializeField] private Material cloudMaterial;

    [Tooltip("Optional: pin a specific DayNightCycleConfig. " +
             "If empty, fetched from DayNightCycleManager.Instance.")]
    [SerializeField] private DayNightCycleConfig config;

    [Header("Quad Settings")]
    [Tooltip("Sorting layer for the cloud shadow quad (should render above ground).")]
    [SerializeField] private string sortingLayerName = "Ground";

    [Tooltip("Sorting order within the sorting layer (high = draws on top of ground tiles).")]
    [SerializeField] private int sortingOrder = 200;

    [Tooltip("How much larger the quad is than the camera view (multiplier). " +
             "Prevents edge pop-in when the camera moves.")]
    [SerializeField] private float quadSizeMultiplier = 3f;

    // ── Shader property IDs ─────────────────────────────────────────────
    private static readonly int ID_CloudNoise     = Shader.PropertyToID("_CloudNoise");
    private static readonly int ID_CloudScale     = Shader.PropertyToID("_CloudScale");
    private static readonly int ID_CloudSpeed     = Shader.PropertyToID("_CloudSpeed");
    private static readonly int ID_CloudContrast  = Shader.PropertyToID("_CloudContrast");
    private static readonly int ID_CloudThreshold = Shader.PropertyToID("_CloudThreshold");
    private static readonly int ID_CloudDirX      = Shader.PropertyToID("_CloudDirX");
    private static readonly int ID_CloudDirY      = Shader.PropertyToID("_CloudDirY");
    private static readonly int ID_CloudDiverge   = Shader.PropertyToID("_CloudDiverge");
    private static readonly int ID_CloudShadowMin = Shader.PropertyToID("_CloudShadowMin");
    private static readonly int ID_CloudOpacity   = Shader.PropertyToID("_CloudOpacity");

    // ── Internal state ──────────────────────────────────────────────────
    private GameObject     _quadGO;
    private SpriteRenderer _quadRenderer;
    private MaterialPropertyBlock _mpb;
    private Camera         _cam;

    // ── Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _cam = Camera.main;
        TryFillConfig();
        CreateQuad();
        EnsureCNoiseTexture();
    }

    void OnEnable()
    {
        TryFillConfig();
        if (_quadGO != null)
            _quadGO.SetActive(true);
    }

    void OnDisable()
    {
        if (_quadGO != null)
            _quadGO.SetActive(false);
    }

    void OnDestroy()
    {
        if (_quadGO != null)
            Destroy(_quadGO);
    }

    void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return;
        }

        TryFillConfig();

        // Position quad centred on camera
        Vector3 camPos = _cam.transform.position;
        _quadGO.transform.position = new Vector3(camPos.x, camPos.y, 0f);

        // Scale quad to cover camera view with margin
        float orthoSize = _cam.orthographicSize;
        float aspect    = _cam.aspect;
        float height    = orthoSize * 2f * quadSizeMultiplier;
        float width     = height * aspect;
        _quadGO.transform.localScale = new Vector3(width, height, 1f);

        // Push shader parameters from config
        PushShaderParams();
    }

    // ── Setup ───────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a cellular (Worley) noise texture at runtime and assigns it
    /// to the material's _CloudNoise slot.  Runs once in Awake so no manual
    /// texture asset needs to be created or assigned.
    /// </summary>
    private void EnsureCNoiseTexture()
    {
        if (cloudMaterial == null) return;

        // Skip if the material already has a texture assigned
        if (cloudMaterial.GetTexture(ID_CloudNoise) != null) return;

        const int res      = 256;
        const int cells    = 6;
        const int oct      = 3;

        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode   = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.name       = "cnoise_runtime";

        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float v = CloudShadowNoise.TileableCellular(x, y, res, cells, oct);
            tex.SetPixel(x, y, new Color(v, v, v, 1f));
        }

        tex.Apply();
        cloudMaterial.SetTexture(ID_CloudNoise, tex);
    }

    private void TryFillConfig()
    {
        if (config == null && DayNightCycleManager.Instance != null)
            config = DayNightCycleManager.Instance.Config;
    }

    private void CreateQuad()
    {
        _quadGO = new GameObject("CloudShadowQuad");
        _quadGO.transform.SetParent(transform, false);

        // Use a SpriteRenderer with a white 1x1 sprite so sorting layers work
        // in URP 2D. MeshRenderer doesn't participate in 2D sorting.
        _quadRenderer = _quadGO.AddComponent<SpriteRenderer>();
        _quadRenderer.sprite = CreateWhiteSprite();
        _quadRenderer.material = cloudMaterial;
        _quadRenderer.sortingLayerName = sortingLayerName;
        _quadRenderer.sortingOrder = sortingOrder;
        _quadRenderer.drawMode = SpriteDrawMode.Simple;
    }

    /// <summary>
    /// Creates a tiny 4×4 white sprite at runtime so we don't need an asset.
    /// The shader ignores the texture colour — it only uses the noise texture.
    /// </summary>
    private Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < 16; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();
        // 100 PPU so a 4×4 texture maps to 0.04 world units; LateUpdate scales it.
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }

    // ── Shader parameter push ───────────────────────────────────────────

    private void PushShaderParams()
    {
        if (config == null || _quadRenderer == null) return;

        // Opacity from DayNightCycleManager (time-of-day + rain)
        float opacity = 0f;
        if (DayNightCycleManager.Instance != null)
            opacity = DayNightCycleManager.Instance.CloudShadowOpacity;

        _quadRenderer.GetPropertyBlock(_mpb);

        _mpb.SetFloat(ID_CloudScale,     config.cloudScale);
        _mpb.SetFloat(ID_CloudSpeed,     config.cloudSpeed);
        _mpb.SetFloat(ID_CloudContrast,  config.cloudContrast);
        _mpb.SetFloat(ID_CloudThreshold, config.cloudThreshold);
        _mpb.SetFloat(ID_CloudDirX,      config.cloudDirection.x);
        _mpb.SetFloat(ID_CloudDirY,      config.cloudDirection.y);
        _mpb.SetFloat(ID_CloudDiverge,   config.cloudDivergeAngle);
        _mpb.SetFloat(ID_CloudShadowMin, config.cloudShadowMin);
        _mpb.SetFloat(ID_CloudOpacity,   opacity);

        _quadRenderer.SetPropertyBlock(_mpb);
    }
}
