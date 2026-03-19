using UnityEngine;
using System;

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

    // ── Data Building (moved from View) ───────────────────────────────────

    /// <summary>
    /// Builds a StructureData from raw item data.
    /// The View passes a <paramref name="getPrefab"/> delegate because prefab selection
    /// is a visual/Inspector concern that belongs to the View layer.
    /// </summary>
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

        return new StructureData
        {
            StructureId     = itemData.itemID,
            DisplayName     = itemData.itemName,
            InteractionType = interactionType,
            Prefab          = prefab
        };
    }

    /// <summary>
    /// Resolves StructureData for an item ID using the catalog service.
    /// The View provides the prefab resolver callback.
    /// </summary>
    public StructureData GetStructureData(string itemID, Func<StructureInteractionType, GameObject> getPrefab)
    {
        var itemData = ItemCatalogService.Instance?.GetItemData(itemID) as StructureItemData;
        return BuildStructureData(itemData, getPrefab);
    }

    // ── Placement Validation (called every frame during ghost preview) ────

    /// <summary>
    /// Returns true if the structure can be placed at <paramref name="anchorWorldPos"/>.
    /// The View uses the result to colour the ghost green/red.
    /// </summary>
    public bool CanPlace(Vector3 anchorWorldPos, StructureData data)
    {
        return structureService.CanPlaceStructure(anchorWorldPos, data);
    }

    // ── Placement ─────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to place the structure. Called by the View when the player clicks.
    /// </summary>
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

    // ── Removal ───────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to remove the structure. Called by the View when demolish criteria are met.
    /// </summary>
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
}
