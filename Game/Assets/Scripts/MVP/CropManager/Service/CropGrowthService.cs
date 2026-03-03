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

    public void GrowAllCrops(float speedMultiplier)
    {
        if (worldData == null) return;

        int cropsGrown = 0;
        int cropsReady = 0;

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
                    worldData.IncrementCropAge(worldPos);

                    if (!worldData.TryGetCropAtWorldPosition(worldPos, out UnifiedChunkData.CropTileData tileData))
                        continue;

                    PlantData plant = GetPlantData(tileData.PlantId);
                    if (plant == null) continue;

                    // Hybrid: grows from pollenStage → pollenStage+1 (mature), then stops.
                    // Normal: grows until last growthStages entry.
                    int effectiveLastStage = plant.isHybrid
                        ? plant.pollenStage + 1
                        : plant.growthStages.Count - 1;

                    if (tileData.CropStage >= effectiveLastStage) continue;

                    int nextStageIndex = tileData.CropStage + 1;
                    // Hybrid mature step may not have a growthStages entry — default to 1 day.
                    int ageRequired = (nextStageIndex < plant.growthStages.Count)
                        ? Mathf.RoundToInt(plant.growthStages[nextStageIndex].age / speedMultiplier)
                        : 1;

                    if (tileData.TotalAge < ageRequired) continue;

                    byte newStage = (byte)nextStageIndex;
                    worldData.UpdateCropStage(worldPos, newStage);

                    if (PhotonNetwork.IsConnected && syncManager != null)
                        syncManager.BroadcastCropStageUpdated(tile.WorldX, tile.WorldY, newStage);

                    OnCropStageChanged?.Invoke(tile.WorldX, tile.WorldY, newStage);
                    cropsGrown++;

                    if (newStage >= plant.growthStages.Count - 1)
                    {
                        cropsReady++;
                        Debug.Log($"[CropGrowthService] '{tileData.PlantId}' at ({tile.WorldX},{tile.WorldY}) ready to harvest.");
                    }
                    else
                    {
                        Debug.Log($"[CropGrowthService] '{tileData.PlantId}' at ({tile.WorldX},{tile.WorldY}) → stage {newStage}.");
                    }
                }
            }
        }

        if (cropsGrown > 0)
            Debug.Log($"[CropGrowthService] Growth tick: {cropsGrown} advanced, {cropsReady} ready to harvest.");
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
        int  newAge   = (newStage < plant.growthStages.Count)
            ? plant.growthStages[newStage].age
            : tileData.TotalAge + 1;

        worldData.UpdateCropStage(worldPos, newStage);
        worldData.UpdateCropAge(worldPos, newAge);

        OnCropStageChanged?.Invoke(worldX, worldY, newStage);
        Debug.Log($"[CropGrowthService] Force-grew ({worldX},{worldY}) → stage {newStage}.");
    }
}
