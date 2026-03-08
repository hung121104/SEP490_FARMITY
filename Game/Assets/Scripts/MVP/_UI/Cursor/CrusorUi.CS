using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaces the system cursor with a custom Sprite across all scenes.
///
/// Setup:
///   1. Attach this script to a GameObject and configure the inspector fields.
///   2. Make that GameObject a Prefab and save it under a Resources folder
///      (e.g. Assets/Resources/CursorManager.prefab).
///   3. Done — [RuntimeInitializeOnLoadMethod] auto-instantiates it at game
///      start without you needing to place it in every scene.
///
/// Alt setup (single scene): just drop the prefab/GameObject into your first
/// scene; DontDestroyOnLoad keeps it alive for the rest of the game.
/// </summary>
public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Cursor Sprite")]
    [Tooltip("Sprite to use as the cursor.")]
    [SerializeField] private Sprite cursorSprite;

    [Tooltip("Pixel offset of the click-point from the sprite's top-left corner (e.g. tip of an arrow).")]
    [SerializeField] private Vector2 hotspotOffset = Vector2.zero;

    [Tooltip("Scale multiplier applied to the sprite's native size.")]
    [SerializeField] private float cursorScale = 1f;

    [Header("Visibility")]
    [Tooltip("Hide the OS cursor while the app has focus.")]
    [SerializeField] private bool hideSystemCursor = true;

    // ── Runtime refs ──────────────────────────────────────────────────────
    private RectTransform _cursorRect;
    private Image         _cursorImage;

    // ── Auto-instantiate from Resources/CursorManager.prefab ─────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInstantiate()
    {
        if (Instance != null) return;

        GameObject prefab = Resources.Load<GameObject>("CursorManager");
        if (prefab != null)
        {
            Instantiate(prefab);
        }
        else
        {
            Debug.LogWarning(
                "[CursorManager] No prefab found at Resources/CursorManager. " +
                "Either save your CursorManager prefab there, or place it manually in your first scene.");
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCursorUI();
        ApplySystemCursorVisibility(true);
    }

    private void Update()
    {
        if (_cursorRect == null) return;

        // Input.mousePosition is in screen pixels, bottom-left origin.
        // With anchorMin/Max at (0,0) and pivot at (0,1) this maps 1:1.
        Vector2 pos = Input.mousePosition;
        _cursorRect.anchoredPosition = pos - hotspotOffset;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        ApplySystemCursorVisibility(hasFocus);
        if (_cursorImage != null)
            _cursorImage.enabled = hasFocus;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Cursor.visible = true;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Swap the cursor sprite at runtime (e.g. hover states).</summary>
    public void SetSprite(Sprite sprite)
    {
        cursorSprite = sprite;
        if (_cursorImage == null) return;

        _cursorImage.sprite = sprite;
        if (sprite != null)
            _cursorRect.sizeDelta = sprite.rect.size * cursorScale;
    }

    /// <summary>Show or hide the custom cursor image without touching the OS cursor.</summary>
    public void SetVisible(bool visible)
    {
        if (_cursorImage != null)
            _cursorImage.enabled = visible;
    }

    // ── Internal ──────────────────────────────────────────────────────────
    private void BuildCursorUI()
    {
        // Fullscreen overlay canvas — highest sort order so it's always on top
        var canvasGO = new GameObject("CursorCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        // CanvasScaler: keep pixel size fixed so cursor doesn't scale with resolution
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Cursor image
        var imageGO = new GameObject("CursorImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        _cursorImage = imageGO.AddComponent<Image>();
        _cursorImage.sprite        = cursorSprite;
        _cursorImage.raycastTarget = false;   // don't block UI clicks

        _cursorRect = _cursorImage.rectTransform;

        // Anchor & pivot: bottom-left of canvas, top-left of sprite at mouse point
        _cursorRect.anchorMin = Vector2.zero;
        _cursorRect.anchorMax = Vector2.zero;
        _cursorRect.pivot     = new Vector2(0f, 1f);  // top-left = click point

        if (cursorSprite != null)
            _cursorRect.sizeDelta = cursorSprite.rect.size * cursorScale;
    }

    private void ApplySystemCursorVisibility(bool appHasFocus)
    {
        Cursor.visible = !hideSystemCursor || !appHasFocus;
    }
}
