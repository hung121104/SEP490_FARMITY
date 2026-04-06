using UnityEngine;
using Photon.Pun;

/// <summary>
/// Validates pollen-application attempts and morphs the receiver crop into a hybrid.
/// Reads cross-breeding tables from <see cref="PollenData.crossResults"/> (data-driven).
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
        this.worldData       = worldData;
        this.cropManagerView = cropManagerView;
        this.syncManager     = syncManager;
    }

    // ── ICropBreedingService ──────────────────────────────────────────────

    public bool CanApplyPollen(PollenData pollen, Vector3 targetWorldPos)
    {
        if (pollen == null) { Debug.LogWarning("[CropBreedingService] CanApplyPollen: pollen is null."); return false; }

        if (!worldData.TryGetCropAtWorldPosition(targetWorldPos, out var tile))
        {
            // Detailed diagnostics — identify exactly which step failed
            int wx = Mathf.FloorToInt(targetWorldPos.x);
            int wy = Mathf.FloorToInt(targetWorldPos.y);
            int secId = worldData.GetSectionIdFromWorldPosition(targetWorldPos);
            if (secId == -1)
            {
                Debug.LogWarning($"[CropBreedingService] CanApplyPollen: position ({wx},{wy}) is NOT in any active section. Check WorldDataManager section configs.");
            }
            else
            {
                UnityEngine.Vector2Int chunkPos = worldData.WorldToChunkCoords(targetWorldPos);
                var chunk = worldData.GetChunkAtWorldPosition(targetWorldPos);
                if (chunk == null)
                    Debug.LogWarning($"[CropBreedingService] CanApplyPollen: sectionId={secId} found but chunk {chunkPos} is NULL (not loaded?).");
                else
                {
                    bool hasCrop = chunk.HasCrop(wx, wy);
                    int cropCount = chunk.GetCropCount();
                    Debug.LogWarning($"[CropBreedingService] CanApplyPollen: chunk {chunkPos} exists (crops={cropCount}), HasCrop({wx},{wy})={hasCrop}. Tile exists but HasCrop=false — tile not planted via WorldDataManager?");
                }
            }
            Debug.LogWarning($"[CropBreedingService] CanApplyPollen: no crop at ({wx},{wy}).");
            return false;
        }

        if (tile.IsPollinated)
        {
            Debug.LogWarning($"[CropBreedingService] CanApplyPollen: crop at {targetWorldPos} already pollinated.");
            return false;
        }

        PlantData targetPlant = GetPlant(tile.PlantId);
        if (targetPlant == null)
        {
            Debug.LogWarning($"[CropBreedingService] CanApplyPollen: plant '{tile.PlantId}' not found in catalog.");
            return false;
        }

        // Pollen from the same species is not allowed
        if (!string.IsNullOrEmpty(pollen.sourcePlantId) && pollen.sourcePlantId == tile.PlantId)
        {
            Debug.LogWarning($"[CropBreedingService] CanApplyPollen: same-species pollen rejected ('{pollen.sourcePlantId}').");
            return false;
        }

        // Target must be at its flowering/pollen stage
        if (tile.CropStage != targetPlant.pollenStage)
        {
            Debug.LogWarning($"[CropBreedingService] CanApplyPollen: crop stage {tile.CropStage} != pollenStage {targetPlant.pollenStage} for plant '{tile.PlantId}'.");
            return false;
        }

        // There must be a defined cross result for this target
        string resultId = pollen.FindResultPlantId(tile.PlantId);
        int crossCount = pollen.crossResults?.Length ?? 0;
        Debug.Log($"[CropBreedingService] CanApplyPollen: pollen='{pollen.itemID}' crossResults={crossCount}, target='{tile.PlantId}', resultId='{resultId}'.");
        if (resultId == null)
        {
            Debug.LogWarning($"[CropBreedingService] CanApplyPollen: no crossResult entry for target '{tile.PlantId}' in pollen '{pollen.itemID}'.");
            return false;
        }

        return true;
    }

    public bool TryApplyPollen(PollenData pollen, Vector3 targetWorldPos)
    {
        if (!worldData.TryGetCropAtWorldPosition(targetWorldPos, out var tile)) return false;

        string resultPlantId = pollen.FindResultPlantId(tile.PlantId);
        if (string.IsNullOrEmpty(resultPlantId))
        {
            Debug.LogWarning($"[CropBreedingService] No cross result for '{tile.PlantId}' + pollen '{pollen.itemID}'.");
            return false;
        }

        // Verify the result plant exists in the catalog
        PlantData resultPlant = PlantCatalogService.Instance?.GetPlantData(resultPlantId);
        if (resultPlant == null)
        {
            Debug.LogWarning($"[CropBreedingService] Result plant '{resultPlantId}' not found in PlantCatalogService.");
            return false;
        }

        int wx = Mathf.FloorToInt(targetWorldPos.x);
        int wy = Mathf.FloorToInt(targetWorldPos.y);

        // Set the crop to the hybrid's pollenStage so it shows the hybridFlower sprite.
        // Growth service will advance it one more step to pollenStage+1 (hybridMature = harvestable).
        byte hybridStartStage = (byte)resultPlant.pollenStage;

        bool morphed = worldData.SetCropPlantId(targetWorldPos, resultPlantId, hybridStartStage);
        if (!morphed)
        {
            Debug.LogError($"[CropBreedingService] SetCropPlantId failed at ({wx},{wy}).");
            return false;
        }

        // Network sync
        if (PhotonNetwork.IsConnected && syncManager != null)
            syncManager.BroadcastCropCrossbred(wx, wy, resultPlantId, hybridStartStage);

        RefreshChunk(targetWorldPos);

        Debug.Log($"[CropBreedingService] Crossbred at ({wx},{wy}): '{tile.PlantId}' → '{resultPlantId}' (hybrid stage {hybridStartStage}, matures at {hybridStartStage + 1}).");
        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private PlantData GetPlant(string plantId)
    {
        if (cropManagerView?.GrowthService != null)
            return cropManagerView.GrowthService.GetPlantData(plantId);

        return PlantCatalogService.Instance?.GetPlantData(plantId);
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
