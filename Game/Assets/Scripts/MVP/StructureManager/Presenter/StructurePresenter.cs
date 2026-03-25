using UnityEngine;
using System;
using Photon.Pun;

/// <summary>
/// Unified presenter for structure placement, removal, and destruction.
/// Mediates between StructureView / StructureDestructionView and StructureService.
/// </summary>
public class StructurePresenter
{
    private readonly IStructureService structureService;
    private readonly bool showDebugLogs;

    // View callback for destruction visual effects (optional — only wired by DestructionView)
    private readonly StructureDestructionView destructionView;

    public StructurePresenter(IStructureService structureService, bool showDebugLogs = true)
    {
        this.structureService = structureService;
        this.showDebugLogs = showDebugLogs;
    }

    /// <summary>
    /// Constructor that also wires destruction event subscriptions (used by DestructionView).
    /// </summary>
    public StructurePresenter(IStructureService structureService,
                              StructureDestructionView destructionView,
                              bool showDebugLogs = true)
        : this(structureService, showDebugLogs)
    {
        this.destructionView = destructionView;

        // Subscribe to HP update events for visual feedback
        ChunkDataSyncManager.OnStructureHpUpdated += OnStructureHpUpdated;

        // Master: Subscribe to hit requests from clients
        if (PhotonNetwork.IsMasterClient)
            ChunkDataSyncManager.OnStructureHitRequest += OnStructureHitRequest;
    }

    ~StructurePresenter()
    {
        ChunkDataSyncManager.OnStructureHpUpdated -= OnStructureHpUpdated;

        if (PhotonNetwork.IsMasterClient)
            ChunkDataSyncManager.OnStructureHitRequest -= OnStructureHitRequest;
    }

    // ── Data Building (Placement) ─────────────────────────────────────────

    public StructureData BuildStructureData(StructureItemData itemData,
                                            Func<StructureInteractionType, GameObject> getPrefab)
    {
        if (itemData == null) return null;

        StructureInteractionType interactionType =
            (StructureInteractionType)itemData.structureInteractionType;

        GameObject prefab = getPrefab(interactionType);
        if (prefab == null)
        {
            Debug.LogWarning($"[StructurePresenter] No prefab for '{itemData.itemID}'");
            return null;
        }

        return new StructureData(itemData, prefab);
    }

    public StructureData GetStructureData(string itemID, Func<StructureInteractionType, GameObject> getPrefab)
    {
        var itemData = ItemCatalogService.Instance?.GetItemData(itemID) as StructureItemData;
        return BuildStructureData(itemData, getPrefab);
    }

    // ── Placement ─────────────────────────────────────────────────────────

    public bool CanPlace(Vector3 anchorWorldPos, StructureData data)
    {
        return structureService.CanPlaceStructure(anchorWorldPos, data);
    }

    public bool HandlePlaceStructure(Vector3 anchorWorldPos, StructureData data)
    {
        if (data == null) return false;

        bool success = structureService.PlaceStructure(anchorWorldPos, data);

        if (showDebugLogs)
        {
            if (success)
                Debug.Log($"[StructurePresenter] Placed '{data.StructureId}' at ({anchorWorldPos.x:F0},{anchorWorldPos.y:F0})");
            else
                Debug.LogWarning($"[StructurePresenter] Failed to place '{data.StructureId}' at ({anchorWorldPos.x:F0},{anchorWorldPos.y:F0})");
        }

        return success;
    }

    public bool HandleRemoveStructure(Vector3 worldPosition, StructureData data)
    {
        if (data == null) return false;

        bool success = structureService.RemoveStructure(worldPosition, data);

        if (showDebugLogs)
        {
            if (success)
                Debug.Log($"[StructurePresenter] Removed '{data.StructureId}' at ({worldPosition.x:F0},{worldPosition.y:F0})");
        }

        return success;
    }

    // ── Destruction ───────────────────────────────────────────────────────

    public void HandleToolUse(Vector3 targetWorldPos, ToolData tool)
    {
        Vector3Int tilePos = new Vector3Int(
            Mathf.FloorToInt(targetWorldPos.x),
            Mathf.FloorToInt(targetWorldPos.y), 0);

        if (structureService.IsStructureAlreadyDestroyed(tilePos))
        {
            if (showDebugLogs)
                Debug.Log($"[StructurePresenter] Structure at {tilePos} already destroyed, ignoring hit");
            return;
        }

        structureService.DealDamage(tilePos, tool.toolPower, out bool isRemoved, out string structureId);
    }

    public void HandleRegenTimerComplete(Vector3Int tilePos)
    {
        structureService.RegenerateHP(tilePos);
    }

    // ── Network Event Handlers (Destruction) ──────────────────────────────

    private void OnStructureHitRequest(int worldX, int worldY, int damage, string playerActorId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Vector3Int tilePos = new Vector3Int(worldX, worldY, 0);
        structureService.ProcessHitRequest(tilePos, damage, playerActorId);
    }

    private void OnStructureHpUpdated(int worldX, int worldY, int newHp)
    {
        if (destructionView == null) return;

        Vector3Int tilePos = new Vector3Int(worldX, worldY, 0);

        if (newHp == -1)
        {
            destructionView.PlayHitEffect(tilePos);
            destructionView.StartRegenTimer(tilePos);
            return;
        }

        if (structureService.IsStructureFullHp(tilePos, newHp))
            return;

        if (newHp <= 0)
            return;

        destructionView.PlayHitEffect(tilePos);
        destructionView.StartRegenTimer(tilePos);
    }
}
