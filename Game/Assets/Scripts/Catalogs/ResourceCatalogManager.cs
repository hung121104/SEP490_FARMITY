using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Singleton that fetches and stores resource configs from
/// GET /game-data/resource-configs/catalog.
/// </summary>
public class ResourceCatalogManager : MonoBehaviour
{
    public static ResourceCatalogManager Instance { get; private set; }

    // Key: resourceId
    private readonly Dictionary<string, ResourceConfigData> _resourceConfigs =
        new Dictionary<string, ResourceConfigData>();

    public IReadOnlyDictionary<string, ResourceConfigData> resourceConfigs => _resourceConfigs;

    public bool IsReady { get; private set; }

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
        CatalogProgressManager.NotifyStarted();
        StartCoroutine(FetchCatalog());
    }

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
    /// Swaps catalog atomically. Safe to call mid-game (SSE reconnect).
    /// </summary>
    public IEnumerator SafeRefetch()
    {
        string url = $"{AppConfig.ApiBaseUrl}/game-data/resource-configs/catalog";
        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ResourceCatalogManager] SafeRefetch failed: {request.error}");
            yield break;
        }

        ResourceCatalogResponse response = null;
        try
        {
            response = JsonConvert.DeserializeObject<ResourceCatalogResponse>(
                request.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ResourceCatalogManager] SafeRefetch parse error: {ex.Message}");
            yield break;
        }

        if (response?.resources == null) yield break;

        _resourceConfigs.Clear();
        foreach (var config in response.resources)
        {
            if (config != null && !string.IsNullOrWhiteSpace(config.resourceId))
                _resourceConfigs[config.resourceId] = config;
        }

        Debug.Log($"[ResourceCatalogManager] SafeRefetch complete — {_resourceConfigs.Count} resource(s).");
    }

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _resourceConfigs.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/resource-configs/catalog";

        ResourceCatalogResponse response = null;

        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = 15;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[ResourceCatalogManager] Attempt {attempt}/{MAX_RETRIES} failed: {request.error}");
                if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
                continue;
            }

            bool parseOk = false;
            try
            {
                response = JsonConvert.DeserializeObject<ResourceCatalogResponse>(
                    request.downloadHandler.text);
                parseOk = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ResourceCatalogManager] JSON parse error (attempt {attempt}): {ex.Message}");
            }
            if (parseOk) break;
            if (attempt < MAX_RETRIES) yield return new WaitForSeconds(RETRY_DELAY);
        }

        if (response == null)
        {
            Debug.LogError($"[ResourceCatalogManager] All {MAX_RETRIES} attempts failed for {url}");
            CatalogProgressManager.NotifyFailed("Resource Catalog");
            yield break;
        }

        if (response?.resources == null || response.resources.Length == 0)
        {
            Debug.LogWarning("[ResourceCatalogManager] Catalog returned 0 resources.");
            IsReady = true;
            yield break;
        }

        foreach (ResourceConfigData config in response.resources)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.resourceId))
            {
                Debug.LogWarning("[ResourceCatalogManager] Skipping entry with missing resourceId.");
                continue;
            }

            _resourceConfigs[config.resourceId] = config;
        }

        IsReady = true;
        Debug.Log($"[ResourceCatalogManager] Catalog ready with {_resourceConfigs.Count} resource config(s).");
        CatalogProgressManager.NotifyCompleted();
    }

    public ResourceConfigData GetResourceConfig(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId)) return null;

        _resourceConfigs.TryGetValue(resourceId, out ResourceConfigData config);
        return config;
    }

    // ── Real-time Sync (SSE) ──────────────────────────────────────────────

    /// <summary>
    /// Adds or updates a single resource config from a JSON string (SSE real-time sync).
    /// </summary>
    public void AddOrUpdateFromJson(string json)
    {
        try
        {
            var config = JsonConvert.DeserializeObject<ResourceConfigData>(json);
            if (config == null || string.IsNullOrWhiteSpace(config.resourceId)) return;
            _resourceConfigs[config.resourceId] = config;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ResourceCatalogManager] AddOrUpdateFromJson failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a resource config by ID (SSE real-time delete).
    /// </summary>
    public bool RemoveResource(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId)) return false;
        return _resourceConfigs.Remove(resourceId);
    }

    // ── Fallback Injection (late-join orphaned data) ────────────────────────

}
