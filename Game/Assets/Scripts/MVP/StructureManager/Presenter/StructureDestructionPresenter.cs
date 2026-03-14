using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class StructureDestructionPresenter
{
    private readonly IStructureDestructionService destructionService;
    private readonly IStructureService structureService;
    private readonly DroppedItemManagerView droppedItemManager;
    private readonly StructureDestructionView view;
    private readonly bool showDebugLogs;

    public StructureDestructionPresenter(
        StructureDestructionView view,
        IStructureDestructionService destructionService,
        IStructureService structureService,
        bool showDebugLogs = true)
    {
        this.view = view;
        this.destructionService = destructionService;
        this.structureService = structureService;
        this.showDebugLogs = showDebugLogs;
    }

    public void HandleToolUse(Vector3 targetWorldPos, ToolData tool)
    {
        Vector3Int tilePos = new Vector3Int(Mathf.FloorToInt(targetWorldPos.x), Mathf.FloorToInt(targetWorldPos.y), 0);

        bool success = destructionService.DealDamage(tilePos, tool.toolPower, out bool isDestroyed, out string structureId);

        if (success)
        {
            // Trigger visual feedback
            view.PlayHitEffect(tilePos);
            view.StartRegenTimer(tilePos);

            if (isDestroyed)
            {
                if (showDebugLogs)
                    Debug.Log($"[StructureDestructionPresenter] Destroying structure {structureId} at {tilePos}");

                // Get StructureDataSO for removal
                var pool = Object.FindAnyObjectByType<StructurePool>();
                if (pool != null)
                {
                    StructureDataSO structureData = pool.GetStructureData(structureId);
                    if (structureData != null)
                    {
                        // Remove from world
                        structureService.RemoveStructure(tilePos, structureData);

                        // Drop item
                        DropStructureItem(structureId, tilePos);
                    }
                }
            }
        }
    }

    private void DropStructureItem(string structureId, Vector3Int tilePos)
    {
        // Offset slightly to center of tile
        Vector3 dropPos = new Vector3(tilePos.x + 0.5f, tilePos.y + 0.5f, 0f);

        // Fetch ItemModel for dropping
        if (ItemCatalogService.Instance != null)
        {
            ItemData itemData = ItemCatalogService.Instance.GetItemData(structureId);
            if (itemData != null)
            {
                ItemModel dropModel = new ItemModel(itemData);
                var sync = Object.FindAnyObjectByType<DroppedItemSyncManager>();
                if (sync != null)
                {
                    DroppedItemData dropData = DroppedItemData.FromItemModel(dropModel, dropPos.x, dropPos.y);
                    sync.SendDropRequest(dropData);
                }
            }
        }
    }

    public void HandleRegenTimerComplete(Vector3Int tilePos)
    {
        destructionService.RegenerateHP(tilePos);
    }
}
