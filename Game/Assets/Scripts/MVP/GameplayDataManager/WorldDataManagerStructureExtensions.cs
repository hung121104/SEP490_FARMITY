using UnityEngine;

/// <summary>
/// Extension methods for WorldDataManager — Structure operations.
/// </summary>
public static class WorldDataManagerStructureExtensions
{
    /// <summary>Place a structure at a world position with specified HP. Fails if a crop is present there.</summary>
    public static bool PlaceStructureAtWorldPosition(this WorldDataManager manager,
                                                      Vector3 worldPos, string structureId, int initialHp)
        => manager.StructureData?.PlaceStructureAtWorldPosition(worldPos, structureId, initialHp) ?? false;

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
    {
        if (manager.CropData == null) return false;
        int wx = Mathf.FloorToInt(worldPos.x);
        int wy = Mathf.FloorToInt(worldPos.y);
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        var chunkPos = manager.WorldToChunkCoords(worldPos);
        var chunk = manager.CropData.GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;

        return chunk.UpdateStructureHp(wx, wy, newHp);
    }
}
