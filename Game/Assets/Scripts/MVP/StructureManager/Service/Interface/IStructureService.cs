using UnityEngine;

/// <summary>
/// Service interface for structure placement / removal operations.
/// Follows the same SOLID pattern as ICropPlantingService.
/// </summary>
public interface IStructureService
{
    /// <summary>
    /// Validates whether a structure can be placed at the given anchor position.
    /// Checks: active section, TillableTilemap (buildable area), occupancy for every tile in NxM footprint.
    /// </summary>
    bool CanPlaceStructure(Vector3 worldPosition, StructureDataSO data);

    /// <summary>
    /// Places a structure at the given anchor position. Updates WorldDataManager,
    /// refreshes chunk visuals, and broadcasts placement to other clients.
    /// </summary>
    bool PlaceStructure(Vector3 worldPosition, StructureDataSO data);

    /// <summary>
    /// Removes the structure at the given position. Updates WorldDataManager,
    /// refreshes chunk visuals, and broadcasts removal to other clients.
    /// </summary>
    bool RemoveStructure(Vector3 worldPosition, StructureDataSO data);

    /// <summary>Checks if a position is within an active world section.</summary>
    bool IsPositionInActiveSection(Vector3 worldPosition);

    /// <summary>Converts world position to chunk coordinates.</summary>
    Vector2Int WorldToChunkCoords(Vector3 worldPosition);

    /// <summary>Broadcasts structure placed event over Photon.</summary>
    //void BroadcastStructurePlaced(int worldX, int worldY, string structureId);

    ///// <summary>Broadcasts structure removed event over Photon.</summary>
    //void BroadcastStructureRemoved(int worldX, int worldY);

    /// <summary>Refreshes chunk visuals after data changes.</summary>
    void RefreshChunkVisuals(Vector2Int chunkPosition);
}
