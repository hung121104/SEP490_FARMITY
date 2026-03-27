/// <summary>
/// Implemented by any structure View that needs world-position and StructureData
/// injected by ChunkLoadingManager when the structure is spawned or loaded.
///
/// Decouples ChunkLoadingManager from concrete structure types:
/// adding a new structure (Furnace, Anvil, …) only requires implementing
/// this interface — ChunkLoadingManager never needs to change.
///
/// MVP layer: View (structure MonoBehaviour is a View component).
/// Responsibility: one-time setup from world data (separate from IInteractable).
/// </summary>
public interface IWorldStructure
{
    /// <summary>
    /// Called once by ChunkLoadingManager after the structure GameObject is spawned.
    /// Each concrete View extracts whatever it needs from <paramref name="structureData"/>.
    /// </summary>
    /// <param name="worldX">Tile X coordinate in world space.</param>
    /// <param name="worldY">Tile Y coordinate in world space.</param>
    /// <param name="structureData">Full StructureData from the pool — contains StructureLevel, StructureId, etc.</param>
    void InitializeFromWorld(int worldX, int worldY, StructureData structureData);
}
