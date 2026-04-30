using UnityEngine;

/// <summary>
/// Attached to the CookingTable prefab.
/// Inherits all interaction/highlight/input logic from InteractableStructureBase.
/// Only handles cooking-specific UI delegation and badge display.
/// </summary>
public class CookingTableStructure : InteractableStructureBase, IWorldStructure
{
    private CraftingSystemManager craftingSystemManager;
    private int cookingLevel = 0;
    private int worldX, worldY;

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
            craftingSystemManager.OpenCookingUI(cookingLevel, worldX, worldY);
        else if (showDebugLogs)
            Debug.LogWarning("[CookingTableStructure] CraftingSystemManager not found in scene!");
    }

    public override void CloseUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.CloseCookingUI();
    }

    // ── Lifecycle Hooks ──────────────────────────────────────────────────

    protected override void OnStructureEnabled()
    {
        ChunkDataSyncManager.OnStationOpened += HandleStationOpened;
        ChunkDataSyncManager.OnStationClosed += HandleStationClosed;
    }

    protected override void OnStructureDisabled()
    {
        ChunkDataSyncManager.OnStationOpened -= HandleStationOpened;
        ChunkDataSyncManager.OnStationClosed -= HandleStationClosed;
    }

    protected override void OnStructureDestroyed()
    {
        ChunkDataSyncManager.OnStationOpened -= HandleStationOpened;
        ChunkDataSyncManager.OnStationClosed -= HandleStationClosed;
    }

    // ── IWorldStructure ──────────────────────────────────────────────────

    public void InitializeFromWorld(int worldX, int worldY, StructureData structureData)
    {
        this.worldX = worldX;
        this.worldY = worldY;
        cookingLevel = structureData.StructureLevel;

        SetupStructureInteractionBadge(structureData.StructureId);
    }

    // ── Badge (Open/Close Notifications) ─────────────────────────────────

    private void HandleStationOpened(int wx, int wy, int actorNumber)
    {
        if (wx != worldX || wy != worldY) return;

        // Only fade for remote openers — the local player who opened sees their own UI.
        if (actorNumber != Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber)
        {
            ShowStructureInteractionBadge();
            CaptureAndHideStructureSprites();
        }

        if (showDebugLogs)
            Debug.Log($"[CookingTable] Badge ON — player #{actorNumber} opened station at ({wx},{wy})");
    }

    private void HandleStationClosed(int wx, int wy, int actorNumber)
    {
        if (wx != worldX || wy != worldY) return;

        if (actorNumber != Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber)
        {
            HideStructureInteractionBadge();
            RestoreStructureSpritesAlpha();
        }

        if (showDebugLogs)
            Debug.Log($"[CookingTable] Badge OFF — player #{actorNumber} closed station at ({wx},{wy})");
    }
}
