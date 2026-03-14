using UnityEngine;

public interface IStructureDestructionService
{
    /// <summary>
    /// Reduces the HP of a structure.
    /// Returns true if the structure was successfully damaged (or destroyed).
    /// </summary>
    bool DealDamage(Vector3Int pos, int damage, out bool isDestroyed, out string structureId);

    /// <summary>
    /// Fully regenerates the HP of the structure at the given position.
    /// </summary>
    void RegenerateHP(Vector3Int pos);
}
