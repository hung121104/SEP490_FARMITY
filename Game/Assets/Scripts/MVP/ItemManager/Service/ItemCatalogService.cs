using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Singleton MonoBehaviour — the client-side item catalog.
/// Loads item data from a local JSON mock (or future remote API endpoint),
/// downloads icon sprites from CDN URLs, and provides typed item lookups.
///
/// Usage:
///   1. Add to a persistent GameObject in your scene.
///   2. Assign <see cref="catalogJsonAsset"/> in the Inspector (TextAsset from Resources/).
///   3. Await <see cref="IsReady"/> == true before calling any Get methods.
///   4. Use <see cref="GetItemData"/> / <see cref="GetItemData{T}"/> to retrieve items.
///
/// Future: set <see cref="catalogApiUrl"/> to your NestJS /items endpoint URL instead.
/// </summary>
public class ItemCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ItemCatalogService Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Catalog Source")]
    [Tooltip("Drag mock_item_catalog.json here for local testing.")]
    [SerializeField] private TextAsset catalogJsonAsset;

    [Tooltip("Live NestJS endpoint URL (e.g. https://api.farmity.com/items). Overrides TextAsset when set.")]
    [SerializeField] private string catalogApiUrl = "";

    // ── Internal State ────────────────────────────────────────────────────────
    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        Converters = { new ItemDataConverter() }
    };

    private readonly Dictionary<string, ItemData> _catalog     = new();
    private readonly Dictionary<string, Sprite>   _spriteCache = new();

    /// <summary>True once catalog JSON is fully parsed and all icon sprites downloaded.</summary>
    public bool IsReady { get; private set; }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!string.IsNullOrEmpty(catalogApiUrl))
            StartCoroutine(LoadCatalogFromUrl(catalogApiUrl));
        else if (catalogJsonAsset != null)
            StartCoroutine(LoadCatalogFromJson(catalogJsonAsset));
        else
            Debug.LogWarning("[ItemCatalogService] No catalog source assigned.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns the base ItemData for any item ID.</summary>
    public ItemData GetItemData(string itemId)
    {
        _catalog.TryGetValue(itemId, out var data);
        return data;
    }

    /// <summary>
    /// Returns a typed subclass instance for the given item ID.
    /// Example: <c>GetItemData&lt;ToolData&gt;("tool_hoe")</c>
    /// Returns null if not found or if the item is not of type T.
    /// </summary>
    public T GetItemData<T>(string itemId) where T : ItemData
        => GetItemData(itemId) as T;

    /// <summary>Returns the cached icon Sprite, or null if not yet downloaded.</summary>
    public Sprite GetCachedSprite(string itemId)
    {
        _spriteCache.TryGetValue(itemId, out var s);
        return s;
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    /// <summary>Load catalog from a local Unity TextAsset (JSON in Resources/).</summary>
    public IEnumerator LoadCatalogFromJson(TextAsset json)
    {
        IsReady = false;
        _catalog.Clear();
        _spriteCache.Clear();

        if (json == null) { Debug.LogError("[ItemCatalogService] TextAsset is null."); yield break; }

        ItemCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<ItemCatalogResponse>(json.text, _jsonSettings);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemCatalogService] JSON parse error: {e.Message}");
            yield break;
        }

        if (response?.items == null || response.items.Count == 0)
        {
            Debug.LogError("[ItemCatalogService] Catalog parsed 0 items. Check JSON.");
            yield break;
        }

        foreach (var item in response.items)
            _catalog[item.itemID] = item;

        Debug.Log($"[ItemCatalogService] Parsed {_catalog.Count} items from local JSON.");
        yield return DownloadAllSprites(response.items);

        IsReady = true;
        Debug.Log("[ItemCatalogService] Catalog ready.");
    }

    /// <summary>Load catalog from a remote URL (future NestJS endpoint).</summary>
    public IEnumerator LoadCatalogFromUrl(string url)
    {
        IsReady = false;
        _catalog.Clear();
        _spriteCache.Clear();

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ItemCatalogService] Failed to fetch from {url}: {req.error}");
            yield break;
        }

        ItemCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<ItemCatalogResponse>(req.downloadHandler.text, _jsonSettings);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ItemCatalogService] Remote JSON parse error: {e.Message}");
            yield break;
        }

        if (response?.items == null) { Debug.LogError("[ItemCatalogService] Remote catalog is empty."); yield break; }

        foreach (var item in response.items)
            _catalog[item.itemID] = item;

        Debug.Log($"[ItemCatalogService] Fetched {_catalog.Count} items from {url}");
        yield return DownloadAllSprites(response.items);

        IsReady = true;
        Debug.Log("[ItemCatalogService] Catalog ready (remote).");
    }

    // ── Sprite Download ───────────────────────────────────────────────────────

    private IEnumerator DownloadAllSprites(IEnumerable<ItemData> items)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.iconUrl))
                yield return DownloadSprite(item.itemID, item.iconUrl);
        }
    }

    private IEnumerator DownloadSprite(string itemId, string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ItemCatalogService] Icon download failed for '{itemId}': {req.error}");
            yield break;
        }

        var tex    = DownloadHandlerTexture.GetContent(req);
        
        // Pixel art settings: crisp filtering and 16 pixels per unit
        tex.filterMode = FilterMode.Point;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        _spriteCache[itemId] = sprite;
        Debug.Log($"[ItemCatalogService] Icon ready for '{itemId}'.");
    }
}
