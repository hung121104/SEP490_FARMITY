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

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _resourceConfigs.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/resource-configs/catalog";

        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[ResourceCatalogManager] Failed to fetch catalog from {url}: {request.error}");
            yield break;
        }

        ResourceCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<ResourceCatalogResponse>(
                request.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ResourceCatalogManager] JSON parse error: {ex.Message}");
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
}
