using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Presenter for structure placement / removal following the MVP pattern.
/// Mediates between StructureView and StructureService.
/// Mirrors the design of CropPlantingPresenter.
/// </summary>
public class StructurePresenter
{
    private readonly IStructureService structureService;
    private readonly bool showDebugLogs;

    public StructurePresenter(IStructureService structureService, bool showDebugLogs = true)
    {
        this.structureService = structureService;
        this.showDebugLogs = showDebugLogs;
    }

    // ── Placement Validation (called every frame during ghost preview) ────

    /// <summary>
    /// Returns true if the structure can be placed at <paramref name="anchorWorldPos"/>.
    /// The View uses the result to colour the ghost green/red.
    /// </summary>
    public bool CanPlace(Vector3 anchorWorldPos, StructureDataSO data)
    {
        return structureService.CanPlaceStructure(anchorWorldPos, data);
    }

    // ── Placement ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to place the structure. Called by the View when the player clicks.
    /// </summary>
    public bool HandlePlaceStructure(Vector3 anchorWorldPos, StructureDataSO data)
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

    // ── Removal ───────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to remove the structure. Called by the View when demolish criteria are met.
    /// </summary>
    public bool HandleRemoveStructure(Vector3 worldPosition, StructureDataSO data)
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

    // ── Network Receive — TEMPORARILY DISABLED for local testing ─────────

    // /// <summary>
    // /// Called when a remote player places a structure (event from ChunkDataSyncManager).
    // /// Updates local data and refreshes visuals.
    // /// </summary>
    // public void HandleNetworkStructurePlaced(Vector3 worldPosition, string structureId)
    // {
    //     WorldDataManager.Instance.PlaceStructureAtWorldPosition(worldPosition, structureId);
    //
    //     if (showDebugLogs)
    //         Debug.Log($"[Network] Structure '{structureId}' placed at ({worldPosition.x:F0},{worldPosition.y:F0})");
    //
    //     Vector2Int chunkPos = structureService.WorldToChunkCoords(worldPosition);
    //     structureService.RefreshChunkVisuals(chunkPos);
    // }

    // /// <summary>
    // /// Called when a remote player removes a structure (event from ChunkDataSyncManager).
    // /// </summary>
    // public void HandleNetworkStructureRemoved(Vector3 worldPosition)
    // {
    //     WorldDataManager.Instance.RemoveStructureAtWorldPosition(worldPosition);
    //
    //     if (showDebugLogs)
    //         Debug.Log($"[Network] Structure removed at ({worldPosition.x:F0},{worldPosition.y:F0})");
    //
    //     Vector2Int chunkPos = structureService.WorldToChunkCoords(worldPosition);
    //     structureService.RefreshChunkVisuals(chunkPos);
    // }
}
