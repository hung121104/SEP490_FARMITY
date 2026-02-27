using UnityEngine;
using Photon.Pun;

/// <summary>
/// Concrete implementation of ICropHarvestingService.
/// Handles world data removal, plant lookup, network broadcast, visual refresh,
/// and inventory insertion — all pure business logic with no Unity UI or input.
/// Pollen collection is delegated to ICropPollenService.
/// </summary>
public class CropHarvestingService : ICropHarvestingService
{
    private readonly WorldDataManager worldData;
    private readonly CropManagerView cropManagerView;
    private readonly ChunkDataSyncManager syncManager;
    private readonly ChunkLoadingManager loadingManager;
    private readonly InventoryGameView inventoryGameView;
    private readonly ICropPollenService pollenService;

    public CropHarvestingService(
        WorldDataManager worldData,
        CropManagerView cropManagerView,
        ChunkDataSyncManager syncManager,
        ChunkLoadingManager loadingManager,
        InventoryGameView inventoryGameView,
        ICropPollenService pollenService)
    {
        this.worldData         = worldData;
        this.cropManagerView   = cropManagerView;
        this.syncManager       = syncManager;
        this.loadingManager    = loadingManager;
        this.inventoryGameView = inventoryGameView;
        this.pollenService     = pollenService;
    }

    // ── ICropHarvestingService ────────────────────────────────────────────

    public bool IsReadyToHarvest(Vector3 worldPos)
    {
        if (worldData == null || cropManagerView == null) return false;
        if (!worldData.HasCropAtWorldPosition(worldPos)) return false;

        int wx = Mathf.FloorToInt(worldPos.x);
        int wy = Mathf.FloorToInt(worldPos.y);
        return cropManagerView.IsCropReadyToHarvest(wx, wy);
    }

    public bool TryHarvest(Vector3 worldPos, out ItemDataSO harvestedItem)
    {
        harvestedItem = null;
        if (worldData == null) return false;

        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        Vector3 snappedPos = new Vector3(worldX, worldY, 0);

        // Resolve the harvested item BEFORE removing crop data
        if (worldData.TryGetCropAtWorldPosition(snappedPos, out UnifiedChunkData.CropTileData tileData)
            && cropManagerView != null
            && !string.IsNullOrEmpty(tileData.PlantId))
        {
            PlantDataSO plantData = cropManagerView.GetPlantData(tileData.PlantId);
            if (plantData != null)
                harvestedItem = plantData.HarvestedItem;
            else
                Debug.LogWarning($"[CropHarvestingService] No PlantDataSO for plantId '{tileData.PlantId}'.");
        }

        // Remove crop from world data
        bool removed = worldData.RemoveCropAtWorldPosition(snappedPos);
        if (!removed)
        {
            Debug.LogWarning($"[CropHarvestingService] Failed to remove crop at ({worldX},{worldY}).");
            return false;
        }

        // Unregister from visual manager
        cropManagerView?.UnregisterCrop(worldX, worldY);

        // Broadcast to other clients
        syncManager?.BroadcastCropRemoved(worldX, worldY);

        // Refresh visuals
        if (loadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(snappedPos);
            loadingManager.RefreshChunkVisuals(chunkPos);
        }

        // Add item to inventory
        if (harvestedItem != null)
        {
            if (inventoryGameView != null)
            {
                bool added = inventoryGameView.AddItem(harvestedItem, 1);
                if (!added)
                    Debug.LogWarning($"[CropHarvestingService] Inventory full — could not add '{harvestedItem.itemName}'.");
                else
                    Debug.Log($"[CropHarvestingService] Added '{harvestedItem.itemName}' from ({worldX},{worldY}).");
            }
        }
        else
        {
            Debug.LogWarning($"[CropHarvestingService] Crop at ({worldX},{worldY}) has no HarvestedItem in PlantDataSO.");
        }

        return true;
    }

    public Vector3 FindNearbyHarvestableTile(Vector3 playerPos, float radius)
    {
        if (worldData == null || cropManagerView == null) return Vector3.zero;

        int px = Mathf.RoundToInt(playerPos.x);
        int py = Mathf.RoundToInt(playerPos.y);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector3 tilePos = new Vector3(px + dx, py + dy, 0);
                float dist = Vector2.Distance(new Vector2(playerPos.x, playerPos.y),
                                              new Vector2(tilePos.x, tilePos.y));
                if (dist > radius) continue;
                if (!worldData.HasCropAtWorldPosition(tilePos)) continue;

                int wx = Mathf.FloorToInt(tilePos.x);
                int wy = Mathf.FloorToInt(tilePos.y);
                if (cropManagerView.IsCropReadyToHarvest(wx, wy)) return tilePos;
            }
        }

        return Vector3.zero;
    }

    // ── Pollen harvesting — delegated to ICropPollenService ───────────────

    public bool IsReadyToCollectPollen(Vector3 worldPos)
        => pollenService != null && pollenService.CanCollectPollen(worldPos);

    public bool TryCollectPollen(Vector3 worldPos, out PollenDataSO pollenItem)
    {
        pollenItem = null;
        if (pollenService == null) return false;

        pollenItem = pollenService.TryCollectPollen(worldPos);
        return pollenItem != null;
    }
}
