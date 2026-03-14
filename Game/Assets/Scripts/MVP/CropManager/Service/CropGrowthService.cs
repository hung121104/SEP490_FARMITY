using UnityEngine;
using Photon.Pun;

/// <summary>
/// Concrete implementation of <see cref="ICropGrowthService"/>.
/// Owns all crop-growth business logic: stage progression, day-tick processing,
/// plant-data lookups, and domain-rule queries.
/// Completely decoupled from Unity UI — no MonoBehaviour dependency.
/// Plant data is sourced from PlantCatalogService (data-driven, no Inspector arrays).
/// </summary>
public class CropGrowthService : ICropGrowthService
{
    // ── Dependencies ──────────────────────────────────────────────────────
    private readonly WorldDataManager worldData;
    private readonly ChunkDataSyncManager syncManager;

    // Cached to avoid FindAnyObjectByType per decay tick
    private ChunkLoadingManager _chunkLoader;

    // ── Configuration ─────────────────────────────────────────────────────
    /// <inheritdoc/>
    public float WateringSpeedMultiplier { get; set; } = 2f;

    /// <inheritdoc/>
    public float WaterDecayDurationMinutes { get; set; } = 24f;

    /// <inheritdoc/>
    public bool IsRaining { get; set; }

    // ── Events ────────────────────────────────────────────────────────────
    /// <inheritdoc/>
    public event System.Action<int, int, byte> OnCropStageChanged;

    // ─────────────────────────────────────────────────────────────────────
    public CropGrowthService(
        WorldDataManager worldData,
        ChunkDataSyncManager syncManager)
    {
        this.worldData   = worldData;
        this.syncManager = syncManager;
    }

    // ── ICropGrowthService : plant-data lookup ────────────────────────────

    public PlantData GetPlantData(string plantId)
    {
        if (string.IsNullOrEmpty(plantId)) return null;

        PlantData plant = PlantCatalogService.Instance?.GetPlantData(plantId);
        if (plant == null)
            Debug.LogWarning($"[CropGrowthService] PlantId '{plantId}' not found in PlantCatalogService.");
        return plant;
    }

    // ── ICropGrowthService : domain-rule queries ──────────────────────────

    public bool IsCropReadyToHarvest(int worldX, int worldY)
    {
        if (worldData == null) return false;
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out UnifiedChunkData.CropTileData tileData))
            return false;

        PlantData plant = GetPlantData(tileData.PlantId);
        if (plant == null) return false;

        // Hybrid plants: harvestable stage is pollenStage+1 (mature).
        // Normal plants: harvestable stage is the last entry in growthStages.
        int harvestStage = plant.isHybrid
            ? plant.pollenStage + 1
            : plant.growthStages.Count - 1;

        return tileData.CropStage >= harvestStage;
    }

    public bool IsCropAtPollenStage(int worldX, int worldY)
    {
        if (worldData == null) return false;
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out UnifiedChunkData.CropTileData tileData))
            return false;

        PlantData plant = GetPlantData(tileData.PlantId);
        if (plant == null || !plant.canProducePollen || string.IsNullOrEmpty(plant.pollenItemId))
            return false;

        if (tileData.CropStage != plant.pollenStage) return false;

        // Enforce per-stage harvest limit (0 = unlimited)
        if (plant.maxPollenHarvestsPerStage > 0
            && tileData.PollenHarvestCount >= plant.maxPollenHarvestsPerStage)
            return false;

        return true;
    }

    public PollenData GetPollenItem(int worldX, int worldY)
    {
        if (worldData == null) return null;
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out UnifiedChunkData.CropTileData tileData))
            return null;

        PlantData plant = GetPlantData(tileData.PlantId);
        if (plant == null || string.IsNullOrEmpty(plant.pollenItemId)) return null;
        return ItemCatalogService.Instance?.GetItemData<PollenData>(plant.pollenItemId);
    }

    // ── ICropGrowthService : growth mutations ─────────────────────────────

    public void TickGrowth(float deltaTime)
    {
        if (worldData == null || deltaTime <= 0f) return;

        for (int s = 0; s < worldData.sectionConfigs.Count; s++)
        {
            var sectionConfig = worldData.sectionConfigs[s];
            if (!sectionConfig.IsActive) continue;

            var section = worldData.GetSection(sectionConfig.SectionId);
            if (section == null) continue;

            foreach (var chunkPair in section)
            {
                UnifiedChunkData chunk = chunkPair.Value;

                foreach (var tile in chunk.GetAllCrops())
                {
                    Vector3 worldPos = new Vector3(tile.WorldX, tile.WorldY, 0);

                    PlantData plant = GetPlantData(tile.Crop.PlantId);
                    if (plant == null) continue;

                    // Hybrid: grows from pollenStage → pollenStage+1 (mature), then stops.
                    // Normal: grows until last growthStages entry.
                    int effectiveLastStage = plant.isHybrid
                        ? plant.pollenStage + 1
                        : plant.growthStages.Count - 1;

                    if (tile.Crop.CropStage >= effectiveLastStage) continue;

                    // ── Per-tile speed multiplier from watering / fertilizer ──
                    float speedMult = 1f;
                    if (tile.Crop.IsWatered)    speedMult *= WateringSpeedMultiplier;
                    if (tile.Crop.IsFertilized) speedMult *= 1.5f;

                    float addedTime = deltaTime * speedMult;
                    worldData.AddGrowthTime(worldPos, addedTime);

                    // Re-read updated timer
                    if (!worldData.TryGetCropAtWorldPosition(worldPos, out UnifiedChunkData.CropTileData updatedTile))
                        continue;

                    int nextStageIndex = updatedTile.CropStage + 1;
                    // Hybrid mature step may not have a growthStages entry — default to 60s.
                    float durationRequired = (nextStageIndex < plant.growthStages.Count)
                        ? plant.growthStages[nextStageIndex].growthDurationMinutes
                        : 60f;

                    if (updatedTile.GrowthTimer >= durationRequired)
                    {
                        byte newStage = (byte)nextStageIndex;
                        worldData.UpdateCropStage(worldPos, newStage);
                        // Carry over excess time for the next stage
                        worldData.UpdateGrowthTimer(worldPos, updatedTile.GrowthTimer - durationRequired);

                        if (PhotonNetwork.IsConnected && syncManager != null)
                            syncManager.BroadcastCropStageUpdated(tile.WorldX, tile.WorldY, newStage);

                        OnCropStageChanged?.Invoke(tile.WorldX, tile.WorldY, newStage);

                        if (newStage >= effectiveLastStage)
                        {
                            Debug.Log($"[CropGrowthService] '{updatedTile.PlantId}' at ({tile.WorldX},{tile.WorldY}) ready to harvest.");
                        }
                    }
                }
            }
        }
    }

    public void TickWaterDecay(float gameMinutesDelta)
    {
        if (worldData == null || gameMinutesDelta <= 0f) return;

        // Pause water decay while it's raining
        if (IsRaining) return;

        for (int s = 0; s < worldData.sectionConfigs.Count; s++)
        {
            var sectionConfig = worldData.sectionConfigs[s];
            if (!sectionConfig.IsActive) continue;

            var section = worldData.GetSection(sectionConfig.SectionId);
            if (section == null) continue;

            foreach (var chunkPair in section)
            {
                UnifiedChunkData chunk = chunkPair.Value;

                foreach (var tile in chunk.GetAllTiles())
                {
                    if (!tile.IsTilled || !tile.Crop.IsWatered) continue;

                    // Accumulate decay time
                    chunk.AddWaterDecayTime(tile.WorldX, tile.WorldY, gameMinutesDelta);

                    if (tile.Crop.WaterDecayTimer + gameMinutesDelta >= WaterDecayDurationMinutes)
                    {
                        chunk.UnwaterTile(tile.WorldX, tile.WorldY);

                        // Remove the watered overlay tile from the tilemap directly
                        if (_chunkLoader == null)
                            _chunkLoader = UnityEngine.Object.FindAnyObjectByType<ChunkLoadingManager>();

                        _chunkLoader?.ClearWateredTileAt(new Vector3(tile.WorldX, tile.WorldY, 0));

                        if (PhotonNetwork.IsConnected && syncManager != null)
                            syncManager.BroadcastTileUnwatered(tile.WorldX, tile.WorldY);

                        Debug.Log($"[CropGrowthService] Water evaporated at ({tile.WorldX},{tile.WorldY}) after {WaterDecayDurationMinutes} game-minutes.");
                    }
                }
            }
        }
    }

    public void ForceGrowCrop(int worldX, int worldY)
    {
        if (worldData == null) return;

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out UnifiedChunkData.CropTileData tileData))
        {
            Debug.LogWarning($"[CropGrowthService] No crop at ({worldX},{worldY}) to force grow.");
            return;
        }

        PlantData plant = GetPlantData(tileData.PlantId);
        if (plant == null) return;

        int effectiveLastStage = plant.isHybrid
            ? plant.pollenStage + 1
            : plant.growthStages.Count - 1;

        if (tileData.CropStage >= effectiveLastStage) return;

        byte newStage = (byte)(tileData.CropStage + 1);
        worldData.UpdateCropStage(worldPos, newStage);
        worldData.UpdateGrowthTimer(worldPos, 0f);

        OnCropStageChanged?.Invoke(worldX, worldY, newStage);
        Debug.Log($"[CropGrowthService] Force-grew ({worldX},{worldY}) → stage {newStage}.");
    }

    public void WaterAllTilledTiles()
    {
        if (worldData == null) return;

        int count = 0;

        for (int s = 0; s < worldData.sectionConfigs.Count; s++)
        {
            var sectionConfig = worldData.sectionConfigs[s];
            if (!sectionConfig.IsActive) continue;

            var section = worldData.GetSection(sectionConfig.SectionId);
            if (section == null) continue;

            foreach (var chunkPair in section)
            {
                UnifiedChunkData chunk = chunkPair.Value;

                foreach (var tile in chunk.GetAllTiles())
                {
                    if (!tile.IsTilled || tile.Crop.IsWatered) continue;

                    chunk.WaterTile(tile.WorldX, tile.WorldY);
                    count++;

                    if (PhotonNetwork.IsConnected && syncManager != null)
                        syncManager.BroadcastTileWatered(tile.WorldX, tile.WorldY);
                }
            }
        }

        if (count > 0)
            Debug.Log($"[CropGrowthService] Rain watered {count} tilled tiles.");
    }
}
