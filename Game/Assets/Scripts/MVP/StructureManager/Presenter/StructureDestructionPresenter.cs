using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class StructureDestructionPresenter
{
    private readonly IStructureDestructionService destructionService;
    private readonly IStructureService structureService;
    private readonly DroppedItemManagerView droppedItemManager;
    private readonly StructureDestructionView view;
    private readonly IInventoryService inventoryService;
    private readonly bool showDebugLogs;

    public StructureDestructionPresenter(
        StructureDestructionView view,
        IStructureDestructionService destructionService,
        IStructureService structureService,
        IInventoryService inventoryService,
        bool showDebugLogs = true)
    {
        this.view = view;
        this.destructionService = destructionService;
        this.structureService = structureService;
        this.inventoryService = inventoryService;
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

                        // AddItem auto-handles dropping when inventory is full
                        inventoryService?.AddItem(structureId);
                    }
                }
            }
        }
    }

    public void HandleRegenTimerComplete(Vector3Int tilePos)
    {
        destructionService.RegenerateHP(tilePos);
    }
}
