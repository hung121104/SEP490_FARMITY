using UnityEngine;

/// <summary>
/// Extension methods for WorldDataManager — Structure operations.
/// </summary>
public static class WorldDataManagerStructureExtensions
{
    /// <summary>Place a structure at a world position with specified HP and level. Fails if a crop is present there.</summary>
    public static bool PlaceStructureAtWorldPosition(this WorldDataManager manager,
                                                      Vector3 worldPos, string structureId, int initialHp, byte structureLevel = 1)
        => manager.StructureData?.PlaceStructureAtWorldPosition(worldPos, structureId, initialHp, structureLevel) ?? false;

    public static bool RemoveStructureAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.StructureData?.RemoveStructureAtWorldPosition(worldPos) ?? false;

    public static bool HasStructureAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.StructureData?.HasStructureAtWorldPosition(worldPos) ?? false;

    public static bool TryGetStructureAtWorldPosition(this WorldDataManager manager, Vector3 worldPos,
                                                       out UnifiedChunkData.StructureTileData structure)
    {
        structure = default;
        if (manager.StructureData == null) return false;
        return manager.StructureData.TryGetStructureAtWorldPosition(worldPos, out structure);
    }

    public static bool UpdateStructureHpAtWorldPosition(this WorldDataManager manager, Vector3 worldPos, int newHp)
        => manager.StructureData?.UpdateStructureHpAtWorldPosition(worldPos, newHp) ?? false;
}
