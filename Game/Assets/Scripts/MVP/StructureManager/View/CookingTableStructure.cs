using UnityEngine;

/// <summary>
/// Attached to the CookingTable prefab.
/// Inherits all interaction/highlight/input logic from InteractableStructureBase.
/// Only handles cooking-specific UI delegation.
/// </summary>
public class CookingTableStructure : InteractableStructureBase, IWorldStructure
{
    private CraftingSystemManager craftingSystemManager;
    private int cookingLevel = 0;

    protected override string StructureTag => "CookingTable";

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
        return craftingSystemManager != null && craftingSystemManager.IsCookingUIOpen();
    }

    public override void OpenUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.OpenCookingUI(cookingLevel);
        else if (showDebugLogs)
            Debug.LogWarning("[CookingTableStructure] CraftingSystemManager not found in scene!");
    }

    public override void CloseUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.CloseCookingUI();
    }

    // ── IWorldStructure ──────────────────────────────────────────────────

    public void InitializeFromWorld(int worldX, int worldY, StructureData structureData)
    {
        cookingLevel = structureData.StructureLevel;
    }
}
