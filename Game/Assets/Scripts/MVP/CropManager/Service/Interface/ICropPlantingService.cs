using UnityEngine;

/// <summary>
/// Service interface for crop planting operations following SOLID principles.
/// Defines the contract for crop planting business logic.
/// </summary>
public interface ICropPlantingService
{
    /// <summary>
    /// Validates if a crop can be planted at the specified world position.
    /// </summary>
    /// <param name="worldPosition">The world position to check</param>
    /// <param name="cropTypeID">The crop type ID to plant</param>
    /// <returns>True if the position is valid for planting</returns>
    bool CanPlantCrop(Vector3 worldPosition, int cropTypeID);

    /// <summary>
    /// Plants a crop at the specified world position.
    /// </summary>
    /// <param name="worldPosition">The world position to plant the crop</param>
    /// <param name="cropTypeID">The crop type ID to plant</param>
    /// <returns>True if the crop was successfully planted</returns>
    bool PlantCrop(Vector3 worldPosition, int cropTypeID);

    /// <summary>
    /// Checks if a position is within the active game section.
    /// </summary>
    /// <param name="worldPosition">The world position to check</param>
    /// <returns>True if the position is in an active section</returns>
    bool IsPositionInActiveSection(Vector3 worldPosition);

    /// <summary>
    /// Checks if a crop already exists at the specified position.
    /// </summary>
    /// <param name="worldPosition">The world position to check</param>
    /// <returns>True if a crop exists at the position</returns>
    bool HasCropAtPosition(Vector3 worldPosition);

    /// <summary>
    /// Converts world position to chunk coordinates.
    /// </summary>
    /// <param name="worldPosition">The world position</param>
    /// <returns>The chunk coordinates</returns>
    Vector2Int WorldToChunkCoords(Vector3 worldPosition);

    /// <summary>
    /// Broadcasts crop planted event to network clients.
    /// </summary>
    /// <param name="worldX">The world X coordinate</param>
    /// <param name="worldY">The world Y coordinate</param>
    /// <param name="cropTypeID">The crop type ID</param>
    void BroadcastCropPlanted(int worldX, int worldY, int cropTypeID);

    /// <summary>
    /// Refreshes the visual representation of a chunk.
    /// </summary>
    /// <param name="chunkPosition">The chunk position to refresh</param>
    void RefreshChunkVisuals(Vector2Int chunkPosition);
}
