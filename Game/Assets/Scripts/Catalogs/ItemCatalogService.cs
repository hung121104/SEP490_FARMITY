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

    private readonly Dictionary<string, ItemData> _catalog     = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Sprite>   _spriteCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Sprite>   _structureInteractionSpriteCache = new(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>Returns a copy of all catalog items.</summary>
    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(_catalog.Values);
    }

    /// <summary>Returns all catalog items of the requested item type.</summary>
    public List<ItemData> GetItemsByType(ItemType itemType)
    {
        var result = new List<ItemData>();
        foreach (ItemData item in _catalog.Values)
        {
            if (item != null && item.itemType == itemType)
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>Returns the cached structure interaction Sprite, or null if not available.</summary>
    public Sprite GetCachedStructureInteractionSprite(string itemId)
    {
        _structureInteractionSpriteCache.TryGetValue(itemId, out var s);
        return s;
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    private const int MAX_RETRIES = 3;
    private const float RETRY_DELAY = 2f;

    public void Retry()
    {
        if (!IsReady)
        {
            CatalogProgressManager.NotifyStarted();
            StartCoroutine(FetchCatalog());
        }
    }

    /// <summary>
    /// Swaps catalog atomically; only downloads sprites for new entries.
    /// Safe to call mid-game (SSE reconnect).
    /// </summary>
    public IEnumerator SafeRefetch()
    {
        string url = $"{AppConfig.ApiBaseUrl}/game-data/items/catalog";
        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ItemCatalogService] SafeRefetch failed: {request.error}");
            yield break;
        }

        ItemCatalogResponse response = null;
        try
        {
            response = JsonConvert.DeserializeObject<ItemCatalogResponse>(
                request.downloadHandler.text, _jsonSettings);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ItemCatalogService] SafeRefetch parse error: {ex.Message}");
            yield break;
        }

        if (response?.items == null) yield break;

        var newCatalog = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in response.items)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.itemID))
                newCatalog[item.itemID] = item;
        }

        // Atomic swap — keep IsReady true
        _catalog.Clear();
        foreach (var kvp in newCatalog)
            _catalog[kvp.Key] = kvp.Value;

        // Download only missing sprites
        foreach (var item in newCatalog.Values)
        {
            if (!string.IsNullOrEmpty(item.iconUrl) && !_spriteCache.ContainsKey(item.itemID))
                yield return DownloadSprite(item.itemID, item.iconUrl);

            if (item is StructureItemData structItem
                && !string.IsNullOrEmpty(structItem.structureInteractionSpriteUrl)
                && !_structureInteractionSpriteCache.ContainsKey(item.itemID))
            {
                yield return DownloadSprite(item.itemID, structItem.structureInteractionSpriteUrl,
                    _structureInteractionSpriteCache);
            }
        }

        Debug.Log($"[ItemCatalogService] SafeRefetch complete — {_catalog.Count} item(s).");
    }

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _catalog.Clear();
        _spriteCache.Clear();
        _structureInteractionSpriteCache.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/items/catalog";

        ItemCatalogResponse response = null;

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[ItemCatalogService] Attempt {attempt}/{MAX_RETRIES} failed: {request.error}");
                if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
                continue;
            }

            bool parseOk = false;
            try
            {
                response = JsonConvert.DeserializeObject<ItemCatalogResponse>(
                    request.downloadHandler.text, _jsonSettings);
                parseOk = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ItemCatalogService] JSON parse error (attempt {attempt}): {ex.Message}");
            }
            if (parseOk) break;
            if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
        }

        if (response == null)
        {
            Debug.LogError($"[ItemCatalogService] All {MAX_RETRIES} attempts failed for {url}");
            CatalogProgressManager.NotifyFailed("Item Catalog");
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

            if (item is StructureItemData structItem
                && !string.IsNullOrEmpty(structItem.structureInteractionSpriteUrl))
            {
                yield return DownloadSprite(item.itemID, structItem.structureInteractionSpriteUrl,
                                            _structureInteractionSpriteCache);
            }
        }
    }

    private IEnumerator DownloadSprite(string itemId, string url,
                                       Dictionary<string, Sprite> targetCache = null)
    {
        targetCache ??= _spriteCache;

        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ItemCatalogService] Sprite download failed for '{itemId}': {req.error}");
            yield break;
        }

        var tex    = DownloadHandlerTexture.GetContent(req);

        // Pixel art settings: crisp filtering and 16 pixels per unit
        tex.filterMode = FilterMode.Point;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        targetCache[itemId] = sprite;
        Debug.Log($"[ItemCatalogService] Sprite ready for '{itemId}'.");
    }

    // ── Catalog Sync (real-time updates from CatalogSyncManager) ───────────

    /// <summary>
    /// Fired when an item entry is added or replaced in the catalog at runtime (admin SSE update).
    /// Subscribers (InventoryGameView, ChestGameView) use this to refresh only affected slots.
    /// Parameter: itemId of the updated item.
    /// </summary>
    public static event Action<string> OnItemUpdated;

    /// <summary>
    /// Adds or replaces an item in the catalog from a JSON string.
    /// Starts sprite download asynchronously.
    /// </summary>
    public void AddOrUpdateFromJson(string json)
    {
        try
        {
            var item = JsonConvert.DeserializeObject<ItemData>(json, _jsonSettings);
            if (item == null || string.IsNullOrWhiteSpace(item.itemID)) return;

            _catalog[item.itemID] = item;

            // Download sprites first, then notify subscribers so UI reads the fresh sprite.
            StartCoroutine(DownloadThenNotify(item));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ItemCatalogService] AddOrUpdateFromJson failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads icon sprites for the given item, then fires OnItemUpdated
    /// so subscribers refresh with both updated data AND the new sprite.
    /// </summary>
    private IEnumerator DownloadThenNotify(ItemData item)
    {
        if (!string.IsNullOrEmpty(item.iconUrl))
            yield return DownloadSprite(item.itemID, item.iconUrl);

        if (item is StructureItemData structItem
            && !string.IsNullOrEmpty(structItem.structureInteractionSpriteUrl))
        {
            yield return DownloadSprite(item.itemID, structItem.structureInteractionSpriteUrl,
                _structureInteractionSpriteCache);
        }

        OnItemUpdated?.Invoke(item.itemID);
    }

    /// <summary>Removes an item and its cached sprites from the catalog.</summary>
    public bool RemoveItem(string itemId)
    {
        _spriteCache.Remove(itemId);
        _structureInteractionSpriteCache.Remove(itemId);
        return _catalog.Remove(itemId);
    }
}
