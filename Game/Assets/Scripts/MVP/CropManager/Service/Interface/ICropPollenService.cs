using UnityEngine;

/// <summary>
/// Contract for pollen collection business logic.
/// The crop is NOT removed when pollen is collected — only a pollen item is granted.
/// </summary>
public interface ICropPollenService
{
    /// <summary>Returns true if the crop at worldPos is at its pollen stage and can give pollen.</summary>
    bool CanCollectPollen(Vector3 worldPos);

    /// <summary>
    /// Collects pollen from the crop at worldPos — adds the pollen item to the player's inventory.
    /// Does NOT remove the crop. Returns the pollen item on success, null on failure.
    /// </summary>
    PollenDataSO TryCollectPollen(Vector3 worldPos);

    /// <summary>
    /// Scans the 3×3 area around playerPos for any crop that is ready to give pollen.
    /// Returns the tile world position of the first found, or Vector3.zero if none.
    /// </summary>
    Vector3 FindNearbyPollenTile(Vector3 playerPos, float radius);
}
