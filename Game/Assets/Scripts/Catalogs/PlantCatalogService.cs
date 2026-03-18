using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Singleton MonoBehaviour — the client-side plant catalog.
/// Fetches plant data from GET /game-data/plants/catalog,
/// downloads stage icon sprites from CDN URLs, and provides typed plant lookups.
///
/// Usage:
///   1. Add to a persistent GameObject in your scene (same one as ItemCatalogService).
///   2. Await <see cref="IsReady"/> == true before calling any Get methods.
///   3. Use <see cref="GetPlantData"/> / <see cref="GetStageSprite"/> to retrieve data.
/// </summary>
public class PlantCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static PlantCatalogService Instance { get; private set; }

    // ── Internal State ────────────────────────────────────────────────────────
    private readonly Dictionary<string, PlantData>               _catalog      = new();
    // Key: "{plantId}_{stageIndex}" → downloaded sprite
    private readonly Dictionary<string, Sprite>                  _spriteCache  = new();

    /// <summary>True once catalog JSON is fully parsed and all stage sprites downloaded.</summary>
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

    /// <summary>Returns the PlantData for the given plantId, or null if not found.</summary>
    public PlantData GetPlantData(string plantId)
    {
        if (string.IsNullOrEmpty(plantId)) return null;
        _catalog.TryGetValue(plantId, out var data);
        return data;
    }

    /// <summary>
    /// Returns the cached stage sprite for the given plant and stage index,
    /// handling hybrid special stages automatically.
    /// Returns null if not yet downloaded or not found.
    /// </summary>
    public Sprite GetStageSprite(string plantId, int stageIndex)
    {
        if (string.IsNullOrEmpty(plantId)) return null;

        PlantData plant = GetPlantData(plantId);
        if (plant == null) return null;

        // Hybrid: stages before pollenStage delegate to receiverPlant
        if (plant.isHybrid)
        {
            if (stageIndex < plant.pollenStage && !string.IsNullOrEmpty(plant.receiverPlantId))
                return GetStageSprite(plant.receiverPlantId, stageIndex);

            // hybridFlower = pollenStage, hybridMature = pollenStage+1
            string hybridKey = stageIndex == plant.pollenStage
                ? $"{plantId}_hybrid_flower"
                : $"{plantId}_hybrid_mature";
            _spriteCache.TryGetValue(hybridKey, out var hybridSprite);
            return hybridSprite;
        }

        string key = $"{plantId}_{stageIndex}";
        _spriteCache.TryGetValue(key, out var sprite);
        return sprite;
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    private IEnumerator FetchCatalog()
    {
        IsReady = false;
        _catalog.Clear();
        _spriteCache.Clear();

        string url = $"{AppConfig.ApiBaseUrl}/game-data/plants/catalog";

        using var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(
                $"[PlantCatalogService] Failed to fetch catalog from {url}: {request.error}");
            yield break;
        }

        PlantCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<PlantCatalogResponse>(
                request.downloadHandler.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlantCatalogService] JSON parse error: {ex.Message}");
            yield break;
        }

        if (response?.plants == null || response.plants.Count == 0)
        {
            Debug.LogWarning("[PlantCatalogService] Catalog returned 0 plants.");
            IsReady = true;
            yield break;
        }

        foreach (PlantData plant in response.plants)
        {
            if (plant == null || string.IsNullOrWhiteSpace(plant.plantId))
            {
                Debug.LogWarning("[PlantCatalogService] Skipping entry with missing plantId.");
                continue;
            }

            _catalog[plant.plantId] = plant;
        }

        Debug.Log($"[PlantCatalogService] Catalog ready with {_catalog.Count} plant(s).");
        
        // Download all sprites
        yield return DownloadAllSprites(response.plants);

        IsReady = true;
        CatalogProgressManager.NotifyCompleted();
    }

    // ── Sprite Download ───────────────────────────────────────────────────────

    private IEnumerator DownloadAllSprites(IEnumerable<PlantData> plants)
    {
        var plantList = new System.Collections.Generic.List<PlantData>(plants);
        int totalSprites = 0;
        
        // Count total sprites first
        foreach (var plant in plantList)
        {
            totalSprites += plant.growthStages.Count;
            if (plant.isHybrid)
                totalSprites += 2; // hybrid_flower + hybrid_mature
        }

        int downloadedSprites = 0;

        foreach (var plant in plantList)
        {
            // Download normal growth stage sprites
            for (int i = 0; i < plant.growthStages.Count; i++)
            {
                var stage = plant.growthStages[i];
                if (!string.IsNullOrEmpty(stage.stageIconUrl))
                {
                    yield return DownloadSprite($"{plant.plantId}_{i}", stage.stageIconUrl);
                    downloadedSprites++;
                    CatalogProgressManager.ReportProgress(downloadedSprites, totalSprites, "Plant Catalog");
                }
            }

            // Download hybrid-specific sprites
            if (plant.isHybrid)
            {
                if (!string.IsNullOrEmpty(plant.hybridFlowerIconUrl))
                {
                    yield return DownloadSprite($"{plant.plantId}_hybrid_flower", plant.hybridFlowerIconUrl);
                    downloadedSprites++;
                    CatalogProgressManager.ReportProgress(downloadedSprites, totalSprites, "Plant Catalog");
                }
                if (!string.IsNullOrEmpty(plant.hybridMatureIconUrl))
                {
                    yield return DownloadSprite($"{plant.plantId}_hybrid_mature", plant.hybridMatureIconUrl);
                    downloadedSprites++;
                    CatalogProgressManager.ReportProgress(downloadedSprites, totalSprites, "Plant Catalog");
                }
            }
        }
    }

    private IEnumerator DownloadSprite(string key, string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[PlantCatalogService] Sprite download failed for '{key}': {req.error}");
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(req);

        // Pixel art settings: crisp filtering and 16 pixels per unit
        tex.filterMode = FilterMode.Point;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16f);
        _spriteCache[key] = sprite;
        Debug.Log($"[PlantCatalogService] Sprite ready for '{key}'.");
    }
}
