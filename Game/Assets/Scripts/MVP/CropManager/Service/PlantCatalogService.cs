using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Singleton MonoBehaviour — the client-side plant catalog.
/// Loads plant data from a local JSON mock (or future remote API endpoint),
/// downloads stage icon sprites from CDN URLs, and provides typed plant lookups.
///
/// Usage:
///   1. Add to a persistent GameObject in your scene (same one as ItemCatalogService).
///   2. Assign <see cref="catalogJsonAsset"/> in the Inspector (TextAsset from Resources/).
///   3. Await <see cref="IsReady"/> == true before calling any Get methods.
///   4. Use <see cref="GetPlantData"/> / <see cref="GetStageSprite"/> to retrieve data.
///
/// Future: set <see cref="catalogApiUrl"/> to your NestJS /plants endpoint URL instead.
/// </summary>
public class PlantCatalogService : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static PlantCatalogService Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Catalog Source")]
    [Tooltip("Drag mock_plant_catalog.json here for local testing.")]
    [SerializeField] private TextAsset catalogJsonAsset;

    [Tooltip("Live NestJS endpoint URL (e.g. https://api.farmity.com/plants). Overrides TextAsset when set.")]
    [SerializeField] private string catalogApiUrl = "";

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

        if (!string.IsNullOrEmpty(catalogApiUrl))
            StartCoroutine(LoadCatalogFromUrl(catalogApiUrl));
        else if (catalogJsonAsset != null)
            StartCoroutine(LoadCatalogFromJson(catalogJsonAsset));
        else
            Debug.LogWarning("[PlantCatalogService] No catalog source assigned.");
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

    /// <summary>Load catalog from a local Unity TextAsset (JSON in Resources/).</summary>
    public IEnumerator LoadCatalogFromJson(TextAsset json)
    {
        IsReady = false;
        _catalog.Clear();
        _spriteCache.Clear();

        if (json == null) { Debug.LogError("[PlantCatalogService] TextAsset is null."); yield break; }

        PlantCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<PlantCatalogResponse>(json.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlantCatalogService] JSON parse error: {e.Message}");
            yield break;
        }

        if (response?.plants == null || response.plants.Count == 0)
        {
            Debug.LogError("[PlantCatalogService] Catalog parsed 0 plants. Check JSON.");
            yield break;
        }

        foreach (var plant in response.plants)
            _catalog[plant.plantId] = plant;

        Debug.Log($"[PlantCatalogService] Parsed {_catalog.Count} plants from local JSON.");
        yield return DownloadAllSprites(response.plants);

        IsReady = true;
        Debug.Log("[PlantCatalogService] Catalog ready.");
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
            Debug.LogError($"[PlantCatalogService] Failed to fetch from {url}: {req.error}");
            yield break;
        }

        PlantCatalogResponse response;
        try
        {
            response = JsonConvert.DeserializeObject<PlantCatalogResponse>(req.downloadHandler.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlantCatalogService] Remote JSON parse error: {e.Message}");
            yield break;
        }

        if (response?.plants == null) { Debug.LogError("[PlantCatalogService] Remote catalog is empty."); yield break; }

        foreach (var plant in response.plants)
            _catalog[plant.plantId] = plant;

        Debug.Log($"[PlantCatalogService] Fetched {_catalog.Count} plants from {url}");
        yield return DownloadAllSprites(response.plants);

        IsReady = true;
        Debug.Log("[PlantCatalogService] Catalog ready (remote).");
    }

    // ── Sprite Download ───────────────────────────────────────────────────────

    private IEnumerator DownloadAllSprites(IEnumerable<PlantData> plants)
    {
        foreach (var plant in plants)
        {
            // Download normal growth stage sprites
            for (int i = 0; i < plant.growthStages.Count; i++)
            {
                var stage = plant.growthStages[i];
                if (!string.IsNullOrEmpty(stage.stageIconUrl))
                    yield return DownloadSprite($"{plant.plantId}_{i}", stage.stageIconUrl);
            }

            // Download hybrid-specific sprites
            if (plant.isHybrid)
            {
                if (!string.IsNullOrEmpty(plant.hybridFlowerIconUrl))
                    yield return DownloadSprite($"{plant.plantId}_hybrid_flower", plant.hybridFlowerIconUrl);
                if (!string.IsNullOrEmpty(plant.hybridMatureIconUrl))
                    yield return DownloadSprite($"{plant.plantId}_hybrid_mature", plant.hybridMatureIconUrl);
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
