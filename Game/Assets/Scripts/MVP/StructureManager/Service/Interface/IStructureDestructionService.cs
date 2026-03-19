using UnityEngine;

public interface IStructureDestructionService
{
    /// <summary>
    /// Reduces the HP of a structure.
    /// Returns true if the structure was successfully damaged (or removed when HP reaches 0).
    /// </summary>
    bool DealDamage(Vector3Int pos, int damage, out bool isRemoved, out string structureId);

    /// <summary>
    /// Fully regenerates the HP of the structure at the given position.
    /// </summary>
    void RegenerateHP(Vector3Int pos);

    /// <summary>
    /// Master only: Process a hit request from a client.
    /// Called by Presenter when receiving network hit request events.
    /// </summary>
    bool ProcessHitRequest(Vector3Int pos, int damage, string playerActorId);

    /// <summary>
    /// Checks if a structure at the given position is already destroyed (HP <= 0 but still exists in data).
    /// Used by Presenter to skip redundant hits.
    /// </summary>
    bool IsStructureAlreadyDestroyed(Vector3Int pos);
}
