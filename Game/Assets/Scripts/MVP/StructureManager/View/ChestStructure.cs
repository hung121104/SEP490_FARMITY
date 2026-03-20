using UnityEngine;

/// <summary>
/// Attached to the Chest prefab.
/// Inherits all interaction/highlight/input logic from InteractableStructureBase.
/// Only handles chest-specific UI, data registration, and badge display.
/// </summary>
public class ChestStructure : InteractableStructureBase, IWorldStructure
{
    [Header("Chest Settings")]
    [Tooltip("Badge shown when another player is using this chest.")]
    [SerializeField] private GameObject inUseBadge;

    private ChestGameView chestGameView;
    private ChestData chestData;

    protected override string StructureTag => "ChestStructure";

    // ── Base Overrides ───────────────────────────────────────────────────

    protected override void FindUI()
    {
        chestGameView = FindFirstObjectByType<ChestGameView>(FindObjectsInactive.Include);
        if (chestGameView == null)
            Debug.LogError("[ChestStructure] ChestGameView not found in scene! Chest interaction will not work.", this);
    }

    protected override bool CanInteract()
    {
        return chestGameView != null && chestData != null;
    }

    public override bool IsUIOpen()
    {
        return chestGameView != null && chestGameView.IsChestOpen()
            && chestGameView.ActiveChestId == chestData?.ChestId;
    }

    public override void OpenUI()
    {
        if (chestGameView != null && chestData != null)
            chestGameView.OpenChest(chestData);
        else if (showDebugLogs)
            Debug.LogWarning("[ChestStructure] ChestGameView not found in scene!");
    }

    public override void CloseUI()
    {
        if (chestGameView != null)
            chestGameView.CloseChest();
    }

    // ── Lifecycle Hooks ──────────────────────────────────────────────────

    protected override void OnStructureEnabled()
    {
        ChestSyncManager.OnChestOpened += HandleChestOpened;
        ChestSyncManager.OnChestClosed += HandleChestClosed;
    }

    protected override void OnStructureDisabled()
    {
        ChestSyncManager.OnChestOpened -= HandleChestOpened;
        ChestSyncManager.OnChestClosed -= HandleChestClosed;
    }

    protected override void OnStructureDestroyed()
    {
        ChestSyncManager.OnChestOpened -= HandleChestOpened;
        ChestSyncManager.OnChestClosed -= HandleChestClosed;
    }

    // ── IWorldStructure ──────────────────────────────────────────────────

    /// <summary>
    /// Called by ChunkLoadingManager after spawn. Builds ChestData from world info.
    /// </summary>
    public void InitializeFromWorld(int worldX, int worldY, StructureData structureData)
    {
        chestData = new ChestData(worldX, worldY, structureData.StructureLevel);

        // Self-register in ChestDataModule (Master or offline only).
        // Idempotent — safe to call even if already registered.
        if (Photon.Pun.PhotonNetwork.IsMasterClient || !Photon.Pun.PhotonNetwork.IsConnected)
        {
            WorldDataManager.Instance?.RegisterChest(
                (short)worldX,
                (short)worldY,
                (byte)structureData.StorageSlots,
                (byte)structureData.StructureLevel);
        }
    }

    /// <summary>Direct init — used by tests or code that already has a ChestData.</summary>
    public void Initialize(ChestData data)
    {
        chestData = data;
    }

    // ── Badge (Open/Close Notifications) ─────────────────────────────────

    private void HandleChestOpened(string chestId, int actorNumber)
    {
        if (chestData == null || chestData.ChestId != chestId) return;
        if (inUseBadge != null)
            inUseBadge.SetActive(true);

        if (showDebugLogs)
            Debug.Log($"[ChestStructure] Badge ON — player #{actorNumber} opened '{chestId}'");
    }

    private void HandleChestClosed(string chestId, int actorNumber)
    {
        if (chestData == null || chestData.ChestId != chestId) return;
        if (inUseBadge != null)
            inUseBadge.SetActive(false);

        if (showDebugLogs)
            Debug.Log($"[ChestStructure] Badge OFF — player #{actorNumber} closed '{chestId}'");
    }
}
