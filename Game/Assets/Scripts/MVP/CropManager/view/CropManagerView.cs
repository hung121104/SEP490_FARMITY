using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// Pure View layer for crop management.
/// Responsibilities:
///   - Wires the TimeManagerView day-change event to ICropGrowthService.
///   - Listens to ICropGrowthService.OnCropStageChanged and refreshes visuals.
///   - Manages crop visual GameObjects (sprite renderers per tile).
///
/// All business logic (growth math, plant-data lookups, domain rules)
/// lives in ICropGrowthService / CropGrowthService.
/// </summary>
public class CropManagerView : MonoBehaviourPunCallbacks
{
    public static CropManagerView Instance { get; private set; }

    // ── Inspector references ──────────────────────────────────────────────
    [Header("References")]
    public TimeManagerView timeManager;

    [Header("Plant Data")]
    [Tooltip("All PlantDataSO assets — passed to CropGrowthService.")]
    public PlantDataSO[] plantDatabase;

    [Header("Growth Settings")]
    public bool enableGrowth = true;
    [Range(0.1f, 10f)]
    public float growthSpeedMultiplier = 1f;

    [Header("Visual")]
    public Transform cropVisualsParent;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // ── Service ───────────────────────────────────────────────────────────
    private ICropGrowthService growthService;

    /// <summary>Exposed so other services (harvesting, pollen) can still resolve plant / pollen data.</summary>
    public ICropGrowthService GrowthService => growthService;

    // ── Visuals ───────────────────────────────────────────────────────────
    private Dictionary<string, GameObject> cropVisuals = new Dictionary<string, GameObject>();
    private ChunkLoadingManager chunkLoadingManager;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Resolve scene dependencies
        if (timeManager == null)
            timeManager = FindAnyObjectByType<TimeManagerView>();

        chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
        var syncManager     = FindAnyObjectByType<ChunkDataSyncManager>();

        // Build the growth service
        growthService = new CropGrowthService(
            WorldDataManager.Instance,
            syncManager,
            plantDatabase);

        // Subscribe to visual-refresh event
        growthService.OnCropStageChanged += OnCropStageChanged;

        // Subscribe to day-change
        if (timeManager != null)
            timeManager.OnDayChanged += OnDayChanged;
        else
            Debug.LogError("[CropManagerView] TimeManagerView not found!");

        // Visual parent fallback
        if (cropVisualsParent == null)
            cropVisualsParent = new GameObject("CropVisuals").transform;

        if (showDebugLogs)
            Debug.Log("[CropManagerView] Initialized.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (timeManager != null) timeManager.OnDayChanged -= OnDayChanged;
        if (growthService != null) growthService.OnCropStageChanged -= OnCropStageChanged;
    }

    // ── Day tick ──────────────────────────────────────────────────────────
    private void OnDayChanged()
    {
        if (!enableGrowth) return;
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;

        growthService.GrowAllCrops(growthSpeedMultiplier);
    }

    // ── Visual refresh (driven by service event) ──────────────────────────
    private void OnCropStageChanged(int worldX, int worldY, byte newStage)
    {
        if (chunkLoadingManager != null)
        {
            // ChunkLoadingManager handles the full visual refresh
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(new Vector3(worldX, worldY, 0));
            chunkLoadingManager.RefreshChunkVisuals(chunkPos);
            return;
        }

        // Fallback: update sprite directly
        PlantDataSO plant = growthService.GetPlantData(
            WorldDataManager.Instance.TryGetCropAtWorldPosition(new Vector3(worldX, worldY, 0),
                out UnifiedChunkData.CropTileData td) ? td.PlantId : null);

        if (plant == null || newStage >= plant.GrowthStages.Count) return;

        string key = $"{worldX}_{worldY}";
        if (cropVisuals.TryGetValue(key, out GameObject go) && go != null)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = plant.GrowthStages[newStage].stageSprite;
        }
        else
        {
            CreateCropVisual(worldX, worldY, plant, newStage);
        }
    }

    // ── Visual management ─────────────────────────────────────────────────
    private void CreateCropVisual(int worldX, int worldY, PlantDataSO plant, int stage)
    {
        if (plant == null || plant.GrowthStages.Count == 0) return;
        string key = $"{worldX}_{worldY}";

        if (cropVisuals.TryGetValue(key, out GameObject old) && old != null)
            Destroy(old);

        GameObject go = new GameObject($"Crop_{plant.PlantName}_{worldX}_{worldY}");
        go.transform.position = new Vector3(worldX + 0.5f, worldY + 0.5f, 0);
        go.transform.SetParent(cropVisualsParent);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = plant.GrowthStages[stage].stageSprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder     = 1;

        cropVisuals[key] = go;
    }

    public void UnregisterCrop(int worldX, int worldY)
    {
        string key = $"{worldX}_{worldY}";

        if (chunkLoadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(new Vector3(worldX, worldY, 0));
            chunkLoadingManager.RefreshChunkVisuals(chunkPos);
        }
        else if (cropVisuals.TryGetValue(key, out GameObject go))
        {
            if (go != null) Destroy(go);
            cropVisuals.Remove(key);
        }

        if (showDebugLogs)
            Debug.Log($"[CropManagerView] Unregistered crop at ({worldX},{worldY}).");
    }

    public void ClearAllVisuals()
    {
        foreach (var go in cropVisuals.Values)
            if (go != null) Destroy(go);
        cropVisuals.Clear();
    }

    // ── Backward-compat thin delegates (used by existing services) ─────────────
    // These simply forward to the growth service so callers don't need updating.

    public PlantDataSO GetPlantData(string plantId)          => growthService?.GetPlantData(plantId);
    public bool IsCropReadyToHarvest(int wx, int wy)         => growthService?.IsCropReadyToHarvest(wx, wy) ?? false;
    public bool IsCropAtPollenStage(int wx, int wy)          => growthService?.IsCropAtPollenStage(wx, wy) ?? false;
    public PollenDataSO GetPollenItem(int wx, int wy)        => growthService?.GetPollenItem(wx, wy);

    /// <summary>Debug-only: immediately advance the crop one stage.</summary>
    public void ForceGrowCrop(int worldX, int worldY)        => growthService?.ForceGrowCrop(worldX, worldY);

    /// <summary>0–1 growth progress for UI display.</summary>
    public float GetCropGrowthProgress(int worldX, int worldY)
    {
        if (WorldDataManager.Instance == null) return 0f;
        if (!WorldDataManager.Instance.TryGetCropAtWorldPosition(
                new Vector3(worldX, worldY, 0), out UnifiedChunkData.CropTileData td)) return 0f;
        PlantDataSO plant = growthService?.GetPlantData(td.PlantId);
        if (plant == null || plant.GrowthStages.Count == 0) return 0f;
        return (float)td.CropStage / (plant.GrowthStages.Count - 1);
    }
}
