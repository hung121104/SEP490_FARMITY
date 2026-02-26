using UnityEngine;
using Photon.Pun;

/// <summary>
/// Concrete implementation of <see cref="ICropGrowthService"/>.
/// Owns all crop-growth business logic: stage progression, day-tick processing,
/// plant-data lookups, and domain-rule queries.
/// Completely decoupled from Unity UI — no MonoBehaviour dependency.
/// </summary>
public class CropGrowthService : ICropGrowthService
{
    // ── Dependencies ──────────────────────────────────────────────────────
    private readonly WorldDataManager worldData;
    private readonly ChunkDataSyncManager syncManager;
    private readonly PlantDataSO[] plantDatabase;

    // ── Events ────────────────────────────────────────────────────────────
    /// <inheritdoc/>
    public event System.Action<int, int, byte> OnCropStageChanged;

    // ─────────────────────────────────────────────────────────────────────
    public CropGrowthService(
        WorldDataManager worldData,
        ChunkDataSyncManager syncManager,
        PlantDataSO[] plantDatabase)
    {
        this.worldData     = worldData;
        this.syncManager   = syncManager;
        this.plantDatabase = plantDatabase;
    }

    // ── ICropGrowthService : plant-data lookup ────────────────────────────

    public PlantDataSO GetPlantData(string plantId)
    {
        if (plantDatabase == null || string.IsNullOrEmpty(plantId)) return null;
        foreach (var plant in plantDatabase)
            if (plant != null && plant.PlantId == plantId) return plant;
        Debug.LogWarning($"[CropGrowthService] PlantId '{plantId}' not found in plantDatabase.");
        return null;
    }

    // ── ICropGrowthService : domain-rule queries ──────────────────────────

    public bool IsCropReadyToHarvest(int worldX, int worldY)
    {
        if (worldData == null) return false;
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out CropChunkData.TileData tileData))
            return false;

        PlantDataSO plant = GetPlantData(tileData.PlantId);
        return plant != null && tileData.CropStage >= plant.GrowthStages.Count - 1;
    }

    public bool IsCropAtPollenStage(int worldX, int worldY)
    {
        if (worldData == null) return false;
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out CropChunkData.TileData tileData))
            return false;

        PlantDataSO plant = GetPlantData(tileData.PlantId);
        if (plant == null || !plant.canProducePollen || plant.PollenItem == null)
            return false;

        if (tileData.CropStage != plant.pollenStage) return false;

        // Enforce per-stage harvest limit (0 = unlimited)
        if (plant.maxPollenHarvestsPerStage > 0
            && tileData.PollenHarvestCount >= plant.maxPollenHarvestsPerStage)
            return false;

        return true;
    }

    public PollenDataSO GetPollenItem(int worldX, int worldY)
    {
        if (worldData == null) return null;
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out CropChunkData.TileData tileData))
            return null;

        return GetPlantData(tileData.PlantId)?.PollenItem;
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
                CropChunkData chunk = chunkPair.Value;

                foreach (var tile in chunk.GetAllCrops())
                {
                    if (!tile.HasCrop) continue;

                    Vector3 worldPos = new Vector3(tile.WorldX, tile.WorldY, 0);
                    worldData.IncrementCropAge(worldPos);

                    if (!worldData.TryGetCropAtWorldPosition(worldPos, out CropChunkData.TileData tileData))
                        continue;

                    PlantDataSO plant = GetPlantData(tileData.PlantId);
                    if (plant == null || tileData.CropStage >= plant.GrowthStages.Count - 1) continue;

                    int nextStageIndex = tileData.CropStage + 1;
                    int ageRequired    = Mathf.RoundToInt(plant.GrowthStages[nextStageIndex].age / speedMultiplier);

                    if (tileData.TotalAge < ageRequired) continue;

                    byte newStage = (byte)nextStageIndex;
                    worldData.UpdateCropStage(worldPos, newStage);

                    if (PhotonNetwork.IsConnected && syncManager != null)
                        syncManager.BroadcastCropStageUpdated(tile.WorldX, tile.WorldY, newStage);

                    OnCropStageChanged?.Invoke(tile.WorldX, tile.WorldY, newStage);
                    cropsGrown++;

                    if (newStage >= plant.GrowthStages.Count - 1)
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
        if (!worldData.TryGetCropAtWorldPosition(worldPos, out CropChunkData.TileData tileData))
        {
            Debug.LogWarning($"[CropGrowthService] No crop at ({worldX},{worldY}) to force grow.");
            return;
        }

        PlantDataSO plant = GetPlantData(tileData.PlantId);
        if (plant == null || tileData.CropStage >= plant.GrowthStages.Count - 1) return;

        byte newStage = (byte)(tileData.CropStage + 1);
        int  newAge   = newStage < plant.GrowthStages.Count ? plant.GrowthStages[newStage].age : tileData.TotalAge;

        worldData.UpdateCropStage(worldPos, newStage);
        worldData.UpdateCropAge(worldPos, newAge);

        OnCropStageChanged?.Invoke(worldX, worldY, newStage);
        Debug.Log($"[CropGrowthService] Force-grew ({worldX},{worldY}) → stage {newStage}.");
    }
}
