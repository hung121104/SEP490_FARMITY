using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

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
    private readonly float interactionRange;
    private Transform localPlayerTransform;

    public ResourceHarvestingService(
        WorldDataManager worldData,
        ChunkDataSyncManager syncManager,
        InventoryGameView inventoryView,
        float interactionRange)
    {
        this.worldData = worldData;
        this.syncManager = syncManager;
        this.inventoryView = inventoryView;
        this.interactionRange = Mathf.Max(0.1f, interactionRange);

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

        if (!TryGetSnappedTargetTile(worldPos, out Vector3 snappedPos))
            return false;

        int wx = Mathf.FloorToInt(snappedPos.x);
        int wy = Mathf.FloorToInt(snappedPos.y);

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

    private bool TryGetSnappedTargetTile(Vector3 mouseWorldPos, out Vector3 snappedTile)
    {
        snappedTile = Vector3.zero;

        if (!TryGetLocalPlayerTransform(out Transform playerTransform))
            return false;

        Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
        snappedTile = CropTileSelector.GetDirectionalTile(
            playerTransform.position,
            mouseWorldPos,
            interactionRange,
            ref dummy);

        return snappedTile != Vector3.zero;
    }

    private bool TryGetLocalPlayerTransform(out Transform playerTransform)
    {
        playerTransform = null;

        if (localPlayerTransform != null && localPlayerTransform.gameObject.activeInHierarchy)
        {
            playerTransform = localPlayerTransform;
            return true;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv == null || !pv.IsMine) continue;

            Transform center = player.transform.Find("CenterPoint");
            localPlayerTransform = center != null ? center : player.transform;
            playerTransform = localPlayerTransform;
            return true;
        }

        return false;
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
                Quality qualityOverride = Quality.Normal;

                bool added = inventoryView.AddItem(drop.itemId, amount, qualityOverride);
                if (!added)
                {
                    Debug.LogWarning(
                        $"[ResourceHarvestingService] Inventory reached capacity while adding {amount}x {drop.itemId}. " +
                        "Overflow was handled automatically by dropping remaining items.");
                }
                else
                {
                    Debug.Log($"[ResourceHarvestingService] Looted {amount}x {drop.itemId} from resource (Quality: {qualityOverride}).");
                }
            }
        }
    }
}
