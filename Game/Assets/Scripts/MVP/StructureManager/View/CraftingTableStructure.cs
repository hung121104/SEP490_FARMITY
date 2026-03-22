using UnityEngine;

/// <summary>
/// Attached to the CraftingTable prefab.
/// Inherits all interaction/highlight/input logic from InteractableStructureBase.
/// Only handles crafting-specific UI delegation.
/// </summary>
public class CraftingTableStructure : InteractableStructureBase, IWorldStructure
{
    private CraftingSystemManager craftingSystemManager;
    private int craftingLevel = 0;

    protected override string StructureTag => "CraftingTable";

    // ── Base Overrides ───────────────────────────────────────────────────

    protected override void FindUI()
    {
        craftingSystemManager = FindFirstObjectByType<CraftingSystemManager>();
    }

    protected override bool CanInteract()
    {
        return craftingSystemManager != null;
    }

    public override bool IsUIOpen()
    {
        return craftingSystemManager != null && craftingSystemManager.IsCraftingUIOpen();
    }

    public override void OpenUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.OpenCraftingUI(craftingLevel);
        else if (showDebugLogs)
            Debug.LogWarning("[CraftingTableStructure] CraftingSystemManager not found in scene!");
    }

    public override void CloseUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.CloseCraftingUI();
    }

    // ── IWorldStructure ──────────────────────────────────────────────────

    /// <summary>
    /// Called by ChunkLoadingManager after spawn.
    /// Reserved for future use — e.g., filter recipes by table level.
    /// </summary>
    public void InitializeFromWorld(int worldX, int worldY, StructureData structureData)
    {
        craftingLevel = structureData.StructureLevel;
    }
}
