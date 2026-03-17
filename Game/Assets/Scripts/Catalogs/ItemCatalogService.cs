using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Singleton MonoBehaviour — the client-side item catalog.
/// Fetches item data from GET /game-data/items/catalog,
/// downloads icon sprites from CDN URLs, and provides typed item lookups.
///
/// Usage:
///   1. Add to a persistent GameObject in your scene.
///   2. Await <see cref="IsReady"/> == true before calling any Get methods.
///   3. Use <see cref="GetItemData"/> / <see cref="GetItemData{T}"/> to retrieve items.
/// </summary>
public class ItemCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ItemCatalogService Instance { get; private set; }

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
    }

    private void Start()
    {
        CatalogProgressManager.NotifyStarted();
        StartCoroutine(FetchCatalog());
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

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _catalog.Clear();
        _spriteCache.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/items/catalog";

        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[ItemCatalogService] Failed to fetch catalog from {url}: {request.error}");
            yield break;
        }

        ItemCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<ItemCatalogResponse>(
                request.downloadHandler.text, _jsonSettings);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ItemCatalogService] JSON parse error: {ex.Message}");
            yield break;
        }

        if (response?.items == null || response.items.Count == 0)
        {
            Debug.LogWarning("[ItemCatalogService] Catalog returned 0 items.");
            IsReady = true;
            yield break;
        }

        foreach (ItemData item in response.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemID))
            {
                Debug.LogWarning("[ItemCatalogService] Skipping entry with missing itemID.");
                continue;
            }

            _catalog[item.itemID] = item;
        }

        Debug.Log($"[ItemCatalogService] Catalog ready with {_catalog.Count} item(s).");
        
        // Download all sprites
        yield return DownloadAllSprites(response.items);

        IsReady = true;
        CatalogProgressManager.NotifyCompleted();
    }

    // ── Sprite Download ───────────────────────────────────────────────────────

    private IEnumerator DownloadAllSprites(IEnumerable<ItemData> items)
    {
        var itemList = new System.Collections.Generic.List<ItemData>(items);
        int totalSprites = itemList.Count;
        int downloadedSprites = 0;

        foreach (var item in itemList)
        {
            if (!string.IsNullOrEmpty(item.iconUrl))
            {
                yield return DownloadSprite(item.itemID, item.iconUrl);
                downloadedSprites++;
                CatalogProgressManager.ReportProgress(downloadedSprites, totalSprites, "Item Catalog");
            }
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
