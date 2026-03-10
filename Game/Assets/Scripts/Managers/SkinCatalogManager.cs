using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton that fetches the skin catalog from the backend, caches PNG
/// spritesheets to disk, slices them into Sprite arrays, and makes them
/// available to any DynamicSpriteSwapper at runtime.
///
/// Lifecycle
/// ---------
///   1. FetchCatalog()  — GET /game-data/skin-configs → JSON array
///   2. For each entry download (or load from disk) the PNG
///   3. Slice into Sprite[] using configId + cellSize
///   4. Expose via GetSprites(configId)
/// </summary>
public class SkinCatalogManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SkinCatalogManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired once all sheets in the catalog have been loaded.</summary>
    public event Action OnCatalogReady;

    // ── State ─────────────────────────────────────────────────────────────────

    /// configId → sliced Sprite[]
    private readonly Dictionary<string, Sprite[]> _catalog =
        new Dictionary<string, Sprite[]>();

    private bool _isReady;
    public bool IsReady => _isReady;

    // ── Internal DTOs ─────────────────────────────────────────────────────────

    [Serializable]
    private class SkinConfigEntry
    {
        public string configId;
        public string spritesheetUrl;
        public int cellSize = 64;
    }

    [Serializable]
    private class SkinConfigList
    {
        public SkinConfigEntry[] items;
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(FetchCatalog());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the sliced Sprite[] for <paramref name="configId"/>, or null
    /// if the catalog is not yet ready or the configId is unknown.
    /// </summary>
    public Sprite[] GetSprites(string configId)
    {
        if (string.IsNullOrEmpty(configId)) return null;
        _catalog.TryGetValue(configId, out var sprites);
        return sprites;
    }

    /// <summary>
    /// Forces a full re-download of the catalog (ignores disk cache).
    /// Useful after an admin updates a spritesheet URL.
    /// </summary>
    public void RefreshCatalog()
    {
        _isReady = false;
        _catalog.Clear();
        StartCoroutine(FetchCatalog());
    }

    // ── Private: Network ──────────────────────────────────────────────────────

    private IEnumerator FetchCatalog()
    {
        string url = $"{AppConfig.ApiBaseUrl}/game-data/skin-configs";

        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[SkinCatalogManager] Failed to fetch catalog from {url}: " +
                $"{request.error}");
            yield break;
        }

        SkinConfigEntry[] entries = ParseCatalogJson(request.downloadHandler.text);
        if (entries == null || entries.Length == 0)
        {
            Debug.LogWarning("[SkinCatalogManager] Catalog returned 0 entries.");
            MarkReady();
            yield break;
        }

        // Load all sheets concurrently via nested coroutines
        int pending = entries.Length;
        foreach (var entry in entries)
        {
            StartCoroutine(LoadSheet(entry, () =>
            {
                pending--;
                if (pending <= 0) MarkReady();
            }));
        }
    }

    // ── Private: Sheet Loading & Disk Cache ───────────────────────────────────

    private IEnumerator LoadSheet(SkinConfigEntry entry, Action onDone)
    {
        // Sanitise configId before using it as a filename
        string safeId = Regex.Replace(entry.configId, @"[^a-zA-Z0-9_\-]", "_");
        string cachePath = Path.Combine(
            Application.persistentDataPath, "SkinCache", $"{safeId}.png");

        byte[] rawBytes = null;

        // 1 — Try disk cache first
        if (File.Exists(cachePath))
        {
            try
            {
                rawBytes = File.ReadAllBytes(cachePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[SkinCatalogManager] Could not read cache for '{entry.configId}': " +
                    $"{ex.Message}. Will re-download.");
                rawBytes = null;
            }
        }

        // 2 — Download if cache miss
        if (rawBytes == null)
        {
            using var request = UnityWebRequestTexture.GetTexture(entry.spritesheetUrl);
            request.timeout = 30;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[SkinCatalogManager] Failed to download sheet '{entry.configId}' " +
                    $"from {entry.spritesheetUrl}: {request.error}");
                onDone?.Invoke();
                yield break;
            }

            // Retrieve PNG bytes from the already-downloaded texture
            var tex = DownloadHandlerTexture.GetContent(request);
            rawBytes = tex.EncodeToPNG();
            Destroy(tex); // discard GPU copy — we'll rebuild below

            // 3 — Write to disk cache (fire and forget)
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                File.WriteAllBytes(cachePath, rawBytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[SkinCatalogManager] Could not cache '{entry.configId}' to disk: " +
                    $"{ex.Message}");
            }
        }

        // 4 — Decode bytes into a Texture2D and slice
        int cellSize = entry.cellSize > 0 ? entry.cellSize : 64;
        Texture2D sheet = DecodeTexture(rawBytes, cellSize);
        if (sheet == null)
        {
            Debug.LogError(
                $"[SkinCatalogManager] Failed to decode texture for '{entry.configId}'.");
            onDone?.Invoke();
            yield break;
        }

        Sprite[] sprites = SliceSheet(sheet, entry.configId, cellSize);
        _catalog[entry.configId] = sprites;

        onDone?.Invoke();
    }

    // ── Private: Texture Decode & Slicing ────────────────────────────────────

    /// <summary>
    /// Decodes raw PNG bytes into a Texture2D configured for pixel art.
    /// FilterMode.Point prevents bilinear blurring on pixel art.
    /// </summary>
    private static Texture2D DecodeTexture(byte[] png, int cellSize)
    {
        // Create a minimal texture; LoadImage will resize automatically.
        var tex = new Texture2D(cellSize, cellSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp,
        };

        if (!tex.LoadImage(png))
        {
            Destroy(tex);
            return null;
        }

        tex.Apply(false, true); // make read-only on GPU for efficiency
        return tex;
    }

    /// <summary>
    /// Slices <paramref name="sheet"/> into a row-major Sprite[] where each
    /// cell is <paramref name="cellSize"/>×<paramref name="cellSize"/> px.
    /// Pivot is Bottom-Center (0.5, 0) — correct for character spritesheets.
    /// </summary>
    private static Sprite[] SliceSheet(Texture2D sheet, string configId, int cellSize)
    {
        int cols = sheet.width  / cellSize;
        int rows = sheet.height / cellSize;

        if (cols == 0 || rows == 0)
        {
            Debug.LogError(
                $"[SkinCatalogManager] Sheet '{configId}' ({sheet.width}×{sheet.height}) " +
                $"is smaller than cellSize {cellSize}.");
            return Array.Empty<Sprite>();
        }

        var sprites = new Sprite[cols * rows];
        int index   = 0;

        // Iterate top-to-bottom, left-to-right to match Unity's row ordering.
        // Unity's texture origin is bottom-left, so row 0 = the BOTTOM row.
        // We flip the row iteration so index 0 = top-left of the sheet,
        // which matches how animators export frames.
        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < cols; col++)
            {
                var rect = new Rect(
                    col * cellSize,
                    row * cellSize,
                    cellSize,
                    cellSize);

                sprites[index++] = Sprite.Create(
                    sheet,
                    rect,
                    new Vector2(0.5f, 0f),          // Bottom-Center pivot
                    pixelsPerUnit: 16,
                    extrude: 0,
                    meshType: SpriteMeshType.FullRect);
            }
        }

        return sprites;
    }

    // ── Private: JSON Parsing ─────────────────────────────────────────────────

    /// <summary>
    /// The backend returns a plain JSON array. JsonUtility only handles objects,
    /// so we wrap it in a temporary root object before parsing.
    /// </summary>
    private static SkinConfigEntry[] ParseCatalogJson(string json)
    {
        try
        {
            string wrapped = $"{{\"items\":{json}}}";
            return JsonUtility.FromJson<SkinConfigList>(wrapped)?.items;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SkinCatalogManager] JSON parse error: {ex.Message}");
            return null;
        }
    }

    private void MarkReady()
    {
        _isReady = true;
        OnCatalogReady?.Invoke();
        Debug.Log(
            $"[SkinCatalogManager] Catalog ready — {_catalog.Count} sheet(s) loaded.");
    }
}
