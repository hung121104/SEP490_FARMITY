using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Service handling interaction with resources (trees, rocks, ore).
/// Reduces HP via delayed tool impact events (Axe, Pickaxe). Upon destruction,
/// distributes loot directly to the local player who landed the final hit.
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
        this.worldData = worldData;
        this.syncManager = syncManager;
        this.inventoryView = inventoryView;

        // Bind to delayed impact events so gameplay timing matches chop animation timing.
        UseToolService.OnAxeImpactRequested += HandleAxeRequested;
        UseToolService.OnPickaxeImpactRequested += HandlePickaxeRequested;
    }

    ~ResourceHarvestingService()
    {
        UseToolService.OnAxeImpactRequested -= HandleAxeRequested;
        UseToolService.OnPickaxeImpactRequested -= HandlePickaxeRequested;
    }

    private void HandleAxeRequested(ToolData tool, Vector3 pos)
    {
        TryHitResource(tool, pos);
    }

    private void HandlePickaxeRequested(ToolData tool, Vector3 pos)
    {
        TryHitResource(tool, pos);
    }

    private bool TryHitResource(ToolData tool, Vector3 worldPos)
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

        // Ensure tool matches the required tool type and has sufficient power.
        if (tool.toolType != configData.requiredToolType || tool.toolPower < configData.minToolPower)
        {
            return false;
        }

        // Damage formula: tool power + (tool power - minimum tool power).
        int calculatedDamage = tool.toolPower + (tool.toolPower - configData.minToolPower);
        if (calculatedDamage <= 0) calculatedDamage = 1;

        int newHp = tileData.CurrentHp - calculatedDamage;

        if (newHp > 0)
        {
            worldData.UpdateResourceHpAtWorldPosition(snappedPos, newHp);
            if (syncManager != null)
            {
                syncManager.BroadcastResourceHpUpdated(wx, wy, newHp);
            }
            Debug.Log($"[ResourceHarvestingService] Hit {tileData.ResourceId} at ({wx},{wy}). HP: {tileData.CurrentHp} -> {newHp}");
        }
        else
        {
            worldData.RemoveResourceAtWorldPosition(snappedPos);

            if (syncManager != null)
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

        foreach (DropEntry drop in dropTable)
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
