using UnityEngine;
using Photon.Pun;

/// <summary>
/// Validates pollen-application attempts and morphs the receiver crop into a hybrid.
/// All network sync goes through ChunkDataSyncManager.
/// </summary>
public class CropBreedingService : ICropBreedingService
{
    private readonly WorldDataManager worldData;
    private readonly CropManagerView  cropManagerView;
    private readonly ChunkDataSyncManager syncManager;

    public CropBreedingService(
        WorldDataManager worldData,
        CropManagerView  cropManagerView,
        ChunkDataSyncManager syncManager = null)
    {
        this.worldData      = worldData;
        this.cropManagerView = cropManagerView;
        this.syncManager    = syncManager;
    }

    // ── ICropBreedingService ──────────────────────────────────────────────

    public bool CanApplyPollen(PollenDataSO pollen, Vector3 targetWorldPos)
    {
        if (pollen == null || pollen.crossResults == null) return false;
        if (!worldData.TryGetCropAtWorldPosition(targetWorldPos, out var tile)) return false;
        if (tile.IsPollinated) return false;

        PlantDataSO targetPlant = GetPlant(tile.PlantId);
        if (targetPlant == null) return false;
        if (targetPlant == pollen.sourcePlant) return false;         // same species
        if (tile.CropStage != targetPlant.pollenStage) return false; // must be flowering

        return FindCrossResult(pollen, targetPlant, out _);
    }

    public bool TryApplyPollen(PollenDataSO pollen, Vector3 targetWorldPos)
    {
        if (!worldData.TryGetCropAtWorldPosition(targetWorldPos, out var tile)) return false;

        PlantDataSO targetPlant = GetPlant(tile.PlantId);
        if (targetPlant == null) return false;
        if (!FindCrossResult(pollen, targetPlant, out var cross)) return false;

        // Roll success chance
        if (Random.value > pollen.pollinationSuccessChance) return false;

        // The hybrid starts at its first unique stage (pollenStage = flowering)
        byte hybridStartStage = (byte)cross.resultPlant.pollenStage;

        // Mutate the tile
        bool ok = worldData.SetCropPlantId(targetWorldPos, cross.resultPlant.PlantId, hybridStartStage);
        if (!ok) return false;

        // Broadcast
        int wx = Mathf.FloorToInt(targetWorldPos.x);
        int wy = Mathf.FloorToInt(targetWorldPos.y);
        if (PhotonNetwork.IsConnected && syncManager != null)
            syncManager.BroadcastCropCrossbred(wx, wy, cross.resultPlant.PlantId, hybridStartStage);

        // Refresh local visuals
        RefreshChunk(targetWorldPos);

        Debug.Log($"[CropBreedingService] ✓ Crossbred ({wx},{wy}): {targetPlant.PlantName} + " +
                  $"{pollen.sourcePlant?.PlantName} → {cross.resultPlant.PlantName}");
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static bool FindCrossResult(PollenDataSO pollen, PlantDataSO target,
                                        out PollenDataSO.CrossResult result)
    {
        result = default;
        foreach (var r in pollen.crossResults)
        {
            if (r.targetPlant == target && r.resultPlant != null)
            {
                result = r;
                return true;
            }
        }
        return false;
    }

    private PlantDataSO GetPlant(string plantId)
    {
        if (cropManagerView != null && cropManagerView.GrowthService != null)
            return cropManagerView.GrowthService.GetPlantData(plantId);

        return Resources.Load<PlantDataSO>($"Plants/{plantId}");
    }

    private void RefreshChunk(Vector3 worldPos)
    {
        var loader = Object.FindAnyObjectByType<ChunkLoadingManager>();
        if (loader == null || worldData == null) return;
        Vector2Int chunkPos = worldData.WorldToChunkCoords(worldPos);
        if (loader.IsChunkLoaded(chunkPos))
            loader.RefreshChunkVisuals(chunkPos);
    }
}
