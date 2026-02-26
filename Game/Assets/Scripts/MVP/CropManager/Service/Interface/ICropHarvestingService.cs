using UnityEngine;

/// <summary>
/// Contract for the crop harvesting business logic layer.
/// </summary>
public interface ICropHarvestingService
{
    /// <summary>
    /// Returns true if a mature crop exists at the given world position.
    /// </summary>
    bool IsReadyToHarvest(Vector3 worldPos);

    /// <summary>
    /// Attempts to harvest the crop at worldPos.
    /// Removes the crop from world data, broadcasts the removal, refreshes visuals,
    /// and returns the harvested ItemDataSO (or null if none matched).
    /// Returns false if the crop could not be removed.
    /// </summary>
    bool TryHarvest(Vector3 worldPos, out ItemDataSO harvestedItem);

    /// <summary>
    /// Scans the 3×3 area around playerPos for any crop ready to harvest.
    /// Returns the tile world position of the first found, or Vector3.zero if none.
    /// </summary>
    Vector3 FindNearbyHarvestableTile(Vector3 playerPos, float radius);

    // ── Pollen harvesting ─────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the crop at worldPos is at its pollen/flowering stage
    /// and has canProducePollen enabled in its PlantDataSO.
    /// The crop is NOT removed when pollen is collected.
    /// </summary>
    bool IsReadyToCollectPollen(Vector3 worldPos);

    /// <summary>
    /// Collects pollen from the crop at worldPos and adds it to the player's inventory.
    /// The crop itself is NOT removed — only pollen is taken.
    /// Returns true on success; pollenItem will contain the PollenDataSO given.
    /// </summary>
    bool TryCollectPollen(Vector3 worldPos, out PollenDataSO pollenItem);
}
