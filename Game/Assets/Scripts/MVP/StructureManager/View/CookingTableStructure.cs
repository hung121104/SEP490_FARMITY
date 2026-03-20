using UnityEngine;

/// <summary>
/// Attached to the CookingTable prefab.
/// Inherits all interaction/highlight/input logic from InteractableStructureBase.
/// Only handles cooking-specific UI delegation.
/// </summary>
public class CookingTableStructure : InteractableStructureBase
{
    private CraftingSystemManager craftingSystemManager;

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
            craftingSystemManager.OpenCookingUI();
        else if (showDebugLogs)
            Debug.LogWarning("[CookingTableStructure] CraftingSystemManager not found in scene!");
    }

    public override void CloseUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.CloseCookingUI();
    }
}
