/// <summary>
/// Contract for the crop-watering action.
/// Applies the IsWatered flag to a tilled crop tile so it grows faster.
/// </summary>
public interface ICropWateringService
{
    /// <summary>Initializes the service with the watered overlay tile reference.</summary>
    void Initialize(UnityEngine.Tilemaps.TileBase wateredTile);

    /// <summary>Returns true if the position is inside the currently active section.</summary>
    bool IsPositionInActiveSection(UnityEngine.Vector3 worldPosition);

    /// <summary>
    /// Returns true if the tile at <paramref name="worldPosition"/> can be watered
    /// (tilled, has a crop, and is not already watered).
    /// </summary>
    bool IsWaterable(UnityEngine.Vector3 worldPosition);

    /// <summary>
    /// Waters the crop at <paramref name="worldPosition"/>, sets the IsWatered flag,
    /// places the watered overlay tile, syncs to other players, and marks the chunk dirty.
    /// Returns true on success.
    /// </summary>
    bool WaterTile(UnityEngine.Vector3 worldPosition);
}
