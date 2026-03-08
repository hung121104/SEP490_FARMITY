using UnityEngine;

/// <summary>
/// Contract for pollen collection business logic.
/// The crop is NOT removed when pollen is collected — only a pollen item is granted.
/// </summary>
public interface ICropPollenService
{
    bool CanCollectPollen(Vector3 worldPos);

    /// <summary>
    /// Collects pollen from the crop at worldPos — adds the pollen item to the player's inventory.
    /// Does NOT remove the crop. Returns the pollen item on success, null on failure.
    /// </summary>
    PollenData TryCollectPollen(Vector3 worldPos);

    Vector3 FindNearbyPollenTile(Vector3 playerPos, float radius);
}

