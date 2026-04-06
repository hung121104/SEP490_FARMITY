using UnityEngine;
using UnityEngine.Tilemaps;

public interface ICropPlowingService
{
    /// <summary>
    /// Checks if a tile at the given position is tillable
    /// </summary>
    bool IsTillable(Vector3Int tilePosition, Vector3 worldPosition);
    
    /// <summary>
    /// Checks if a tile at the given position already has data (is plowed or has something placed)
    /// </summary>
    bool HasTileData(Vector3Int tilePosition);
    
    /// <summary>
    /// Adds a tilled tile at the given position
    /// </summary>
    bool PlowTile(Vector3Int tilePosition, Vector3 worldPosition);
    
    /// <summary>
    /// Initializes the service with the tilled tile reference
    /// </summary>
    void Initialize(TileBase tilledTile);

    /// <summary>
    /// Removes the crop on a tilled tile (hoe on occupied tile). Returns true if a crop was removed.
    /// </summary>
    bool RemoveCropOnTile(Vector3 worldPosition);

    /// <summary>
    /// Untills a tilled tile with no crop (hoe on empty tilled tile).
    /// Removes the tilled state from chunk data and from the tilemap. Returns true on success.
    /// </summary>
    bool UntillTile(Vector3 worldPosition);
}
