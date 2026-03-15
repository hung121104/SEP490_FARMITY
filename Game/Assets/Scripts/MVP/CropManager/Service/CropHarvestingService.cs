using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

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

    public bool TryHarvest(Vector3 worldPos, out ItemData harvestedItem)
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
            PlantData plantData = cropManagerView.GetPlantData(tileData.PlantId);
            if (plantData != null && !string.IsNullOrEmpty(plantData.harvestedItemId))
                harvestedItem = ItemCatalogService.Instance?.GetItemData(plantData.harvestedItemId);
            else
                Debug.LogWarning($"[CropHarvestingService] No harvestedItemId for plantId '{tileData.PlantId}'.");
        }

        // Remove crop from world data
        bool removed = worldData.RemoveCropAtWorldPosition(snappedPos);
        if (!removed)
        {
            Debug.LogWarning($"[CropHarvestingService] Failed to remove crop at ({worldX},{worldY}).");
            return false;
        }

        cropManagerView?.UnregisterCrop(worldX, worldY);
        syncManager?.BroadcastCropRemoved(worldX, worldY);

        // If it's raining, re-water the remaining tilled tile
        if (WeatherView.IsRaining && worldData.IsTilledAtWorldPosition(snappedPos))
        {
            worldData.WaterTileAtWorldPosition(snappedPos);

            if (syncManager != null && PhotonNetwork.IsConnected)
                syncManager.BroadcastTileWatered(worldX, worldY);
        }

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
                bool added = inventoryGameView.AddItem(harvestedItem.itemID, 1);
                if (!added)
                    Debug.LogWarning($"[CropHarvestingService] Inventory full — could not add '{harvestedItem.itemName}'.");
                else
                    Debug.Log($"[CropHarvestingService] Added '{harvestedItem.itemName}' from ({worldX},{worldY}).");
            }
        }
        else
        {
            Debug.LogWarning($"[CropHarvestingService] Crop at ({worldX},{worldY}) has no harvestedItemId.");
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

    public bool TryCollectPollen(Vector3 worldPos, out PollenData pollenItem)
    {
        pollenItem = null;
        if (pollenService == null) return false;

        pollenItem = pollenService.TryCollectPollen(worldPos);
        return pollenItem != null;
    }
}

/// <summary>
/// Interface for Resource Harvesting
/// </summary>
public interface IResourceHarvestingService
{
}

/// <summary>
/// Service handling interaction with Resources (trees, rocks, ore).
/// Reduces HP via UseTool events (Axe, Pickaxe). Upon destruction, distributes
/// loot directly to the local player who landed the final hit, ensuring fair loot distribution.
/// </summary>
public class ResourceHarvestingService : IResourceHarvestingService
{
    private readonly WorldDataManager worldData;
    private readonly ChunkDataSyncManager syncManager;
    private readonly InventoryGameView inventoryView;

    public ResourceHarvestingService(
        WorldDataManager worldData,
        ChunkDataSyncManager syncManager,
        InventoryGameView inventoryView)
    {
        this.worldData     = worldData;
        this.syncManager   = syncManager;
        this.inventoryView = inventoryView;

        // Bind Tool Use Events
        UseToolService.OnAxeRequested     += HandleAxeRequested;
        UseToolService.OnPickaxeRequested += HandlePickaxeRequested;
    }

    ~ResourceHarvestingService()
    {
        UseToolService.OnAxeRequested     -= HandleAxeRequested;
        UseToolService.OnPickaxeRequested -= HandlePickaxeRequested;
    }

    private void HandleAxeRequested(ToolData tool, Vector3 pos)
    {
        TryHitResource(tool, pos, "tree");
    }

    private void HandlePickaxeRequested(ToolData tool, Vector3 pos)
    {
        // Pickaxe works on both rocks and ores
        if (!TryHitResource(tool, pos, "rock"))
        {
            TryHitResource(tool, pos, "ore");
        }
    }

    private bool TryHitResource(ToolData tool, Vector3 worldPos, string requiredResourceType)
    {
        if (worldData == null) return false;
        
        int wx = Mathf.FloorToInt(worldPos.x);
        int wy = Mathf.FloorToInt(worldPos.y);
        Vector3 snappedPos = new Vector3(wx, wy, 0);

        if (!worldData.TryGetResourceAtWorldPosition(snappedPos, out UnifiedChunkData.ResourceTileData tileData))
        {
            return false;
        }

        if (string.IsNullOrEmpty(tileData.ResourceId)) return false;

        ResourceConfigData configData = ResourceCatalogManager.Instance?.GetResourceConfig(tileData.ResourceId);
        if (configData == null) return false;

        // Ensure tool matches the required resource type
        if (!string.Equals(configData.resourceType, requiredResourceType, System.StringComparison.OrdinalIgnoreCase))
        {
            if (configData.resourceType == "ore" && requiredResourceType == "rock") 
            {
                // Accept Pickaxe for ore as well
            }
            else
            {
                return false;
            }
        }

        // Apply Damage
        int damage = Mathf.Max(1, tool.toolPower);
        int newHp = tileData.CurrentHp - damage;

        if (newHp > 0)
        {
            // Resource survives
            worldData.UpdateResourceHpAtWorldPosition(snappedPos, newHp);
            if (PhotonNetwork.IsConnected && syncManager != null)
            {
                syncManager.BroadcastResourceHpUpdated(wx, wy, newHp);
            }
            Debug.Log($"[ResourceHarvestingService] Hit {tileData.ResourceId} at ({wx},{wy}). HP: {tileData.CurrentHp} -> {newHp}");
        }
        else
        {
            // Resource Destroyed!
            worldData.RemoveResourceAtWorldPosition(snappedPos);
            
            if (PhotonNetwork.IsConnected && syncManager != null)
            {
                syncManager.BroadcastResourceRemoved(wx, wy);
            }

            Debug.Log($"[ResourceHarvestingService] Destroyed {tileData.ResourceId} at ({wx},{wy}).");
            DistributeLoot(configData.dropTable);
        }

        return true;
    }

    private void DistributeLoot(List<DropEntry> dropTable)
    {
        if (inventoryView == null || dropTable == null || dropTable.Count == 0) return;

        foreach (var drop in dropTable)
        {
            if (string.IsNullOrEmpty(drop.itemId)) continue;

            float chance = Random.Range(0f, 1f);
            if (chance <= drop.dropChance)
            {
                int amount = Random.Range(Mathf.Max(1, drop.minAmount), Mathf.Max(1, drop.maxAmount) + 1);
                
                bool added = inventoryView.AddItem(drop.itemId, amount);
                if (!added)
                {
                    Debug.LogWarning($"[ResourceHarvestingService] Inventory full! Failed to add {amount}x {drop.itemId}");
                }
                else
                {
                    Debug.Log($"[ResourceHarvestingService] Looted {amount}x {drop.itemId} from resource.");
                }
            }
        }
    }
}
