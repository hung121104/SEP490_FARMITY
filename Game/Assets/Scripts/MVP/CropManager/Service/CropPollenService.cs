using UnityEngine;
using Photon.Pun;

/// <summary>
/// Implements pollen collection logic.
/// The crop is NOT removed — only the pollen item is added to the player's inventory.
/// Broadcasts the updated PollenHarvestCount to all other clients via ChunkDataSyncManager.
/// </summary>
public class CropPollenService : ICropPollenService
{
    private readonly WorldDataManager worldData;
    private readonly CropManagerView cropManagerView;
    private readonly InventoryGameView inventoryGameView;
    private readonly ChunkDataSyncManager syncManager;

    public CropPollenService(
        WorldDataManager worldData,
        CropManagerView cropManagerView,
        InventoryGameView inventoryGameView,
        ChunkDataSyncManager syncManager = null)
    {
        this.worldData        = worldData;
        this.cropManagerView  = cropManagerView;
        this.inventoryGameView = inventoryGameView;
        this.syncManager      = syncManager;
    }

    // ── ICropPollenService ────────────────────────────────────────────────

    public bool CanCollectPollen(Vector3 worldPos)
    {
        int wx = Mathf.FloorToInt(worldPos.x);
        int wy = Mathf.FloorToInt(worldPos.y);
        return cropManagerView != null && cropManagerView.IsCropAtPollenStage(wx, wy);
    }

    public PollenData TryCollectPollen(Vector3 worldPos)
    {
        int wx = Mathf.FloorToInt(worldPos.x);
        int wy = Mathf.FloorToInt(worldPos.y);

        if (cropManagerView == null) return null;

        PollenData pollen = cropManagerView.GetPollenItem(wx, wy);
        if (pollen == null)
        {
            Debug.LogWarning($"[CropPollenService] No PollenData found for crop at ({wx},{wy}).");
            return null;
        }

        if (inventoryGameView == null)
        {
            Debug.LogWarning("[CropPollenService] InventoryGameView not found — pollen not added.");
            return null;
        }

        bool added = inventoryGameView.AddItem(pollen.itemID, 1);
        if (!added)
        {
            Debug.LogWarning($"[CropPollenService] Inventory full — could not add '{pollen.itemName}'.");
            return null;
        }

        worldData.IncrementPollenHarvestCount(worldPos);

        byte newCount = 0;
        if (worldData.TryGetCropAtWorldPosition(worldPos, out UnifiedChunkData.CropTileData tileData))
            newCount = tileData.PollenHarvestCount;

        if (PhotonNetwork.IsConnected && syncManager != null)
            syncManager.BroadcastPollenHarvested(wx, wy, newCount);

        Debug.Log($"[CropPollenService] Collected pollen '{pollen.itemName}' from ({wx},{wy}). Count: {newCount}.");
        return pollen;
    }

    public Vector3 FindNearbyPollenTile(Vector3 playerPos, float radius)
    {
        if (cropManagerView == null) return Vector3.zero;

        int px = Mathf.RoundToInt(playerPos.x);
        int py = Mathf.RoundToInt(playerPos.y);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector3 tilePos = new Vector3(px + dx, py + dy, 0);
                float dist = Vector2.Distance(new Vector2(playerPos.x, playerPos.y),
                                              new Vector2(tilePos.x,  tilePos.y));
                if (dist > radius) continue;

                int wx = Mathf.FloorToInt(tilePos.x);
                int wy = Mathf.FloorToInt(tilePos.y);

                if (cropManagerView.IsCropAtPollenStage(wx, wy))
                    return tilePos;
            }
        }

        return Vector3.zero;
    }
}
