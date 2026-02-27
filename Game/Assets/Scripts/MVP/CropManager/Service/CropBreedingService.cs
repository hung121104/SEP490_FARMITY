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

    public bool CanApplyPollen(PollenData pollen, Vector3 targetWorldPos)
    {
        if (pollen == null) return false;
        // TODO: Reconnect crossResults check when PollenData.crossResults is wired (requires PlantData refactor)
        // if (pollen.crossResults == null) return false;
        if (!worldData.TryGetCropAtWorldPosition(targetWorldPos, out var tile)) return false;
        if (tile.IsPollinated) return false;

        PlantDataSO targetPlant = GetPlant(tile.PlantId);
        if (targetPlant == null) return false;
        // TODO: if (targetPlant == pollen.sourcePlant) return false; — deferred
        if (tile.CropStage != targetPlant.pollenStage) return false;

        // TODO: return FindCrossResult(pollen, targetPlant, out _);
        Debug.LogWarning("[CropBreedingService] CanApplyPollen: crossResult check deferred until PlantData is refactored.");
        return false;
    }

    public bool TryApplyPollen(PollenData pollen, Vector3 targetWorldPos)
    {
        // TODO: Reconnect this when PollenData.crossResults and PlantData are fully wired
        Debug.LogWarning("[CropBreedingService] TryApplyPollen: deferred until PlantData refactor.");
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    // TODO: Restore FindCrossResult when PollenData.crossResults is wired
    // private static bool FindCrossResult(PollenData pollen, PlantDataSO target, out PollenData.CrossResult result) { ... }

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
