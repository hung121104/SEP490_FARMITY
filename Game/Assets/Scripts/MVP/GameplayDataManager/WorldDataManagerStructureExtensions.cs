using UnityEngine;

/// <summary>
/// Extension methods for WorldDataManager — Structure operations.
/// Mirrors WorldDataManagerCropExtensions for structure placement/removal.
/// </summary>
public static class WorldDataManagerStructureExtensions
{
    /// <summary>Place a structure at a world position. Fails if a crop is present there.</summary>
    public static bool PlaceStructureAtWorldPosition(this WorldDataManager manager,
                                                      Vector3 worldPos, string structureId)
        => manager.StructureData?.PlaceStructureAtWorldPosition(worldPos, structureId) ?? false;

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
}
