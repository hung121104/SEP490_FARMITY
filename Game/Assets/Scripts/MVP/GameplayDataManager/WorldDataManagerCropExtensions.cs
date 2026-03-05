using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Extension methods for WorldDataManager - Crop operations.
/// Keeps WorldDataManager focused on core coordination (SOLID - Single Responsibility).
/// </summary>
public static class WorldDataManagerCropExtensions
{
    public static bool PlantCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos, string plantId)
        => manager.CropData?.PlantCropAtWorldPosition(worldPos, plantId) ?? false;

    public static bool RemoveCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.RemoveCropAtWorldPosition(worldPos) ?? false;

    public static bool HasCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.HasCropAtWorldPosition(worldPos) ?? false;

    public static bool TryGetCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos,
                                                  out UnifiedChunkData.CropTileData crop)
    {
        crop = default;
        if (manager.CropData == null) return false;
        return manager.CropData.TryGetCropAtWorldPosition(worldPos, out crop);
    }

    public static bool UpdateCropStage(this WorldDataManager manager, Vector3 worldPos, byte newStage)
        => manager.CropData?.UpdateCropStage(worldPos, newStage) ?? false;

    public static bool UpdateCropAge(this WorldDataManager manager, Vector3 worldPos, int newAge)
        => manager.CropData?.UpdateCropAge(worldPos, newAge) ?? false;

    public static bool IncrementCropAge(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.IncrementCropAge(worldPos) ?? false;

    public static bool IncrementPollenHarvestCount(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.IncrementPollenHarvestCount(worldPos) ?? false;

    public static bool TillTileAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.TillTileAtWorldPosition(worldPos) ?? false;

    public static bool UntillTileAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.UntillTileAtWorldPosition(worldPos) ?? false;

    public static bool IsTilledAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.IsTilledAtWorldPosition(worldPos) ?? false;

    /// <summary>Get crop chunk at a specific section/chunk coordinate.</summary>
    public static UnifiedChunkData GetChunk(this WorldDataManager manager, int sectionId, Vector2Int chunkPos)
        => manager.CropData?.GetChunk(sectionId, chunkPos);

    /// <summary>Get all crop chunks in a section.</summary>
    public static Dictionary<Vector2Int, UnifiedChunkData> GetSection(this WorldDataManager manager, int sectionId)
        => manager.CropData?.GetSection(sectionId);

    /// <summary>Get the crop chunk at a world position.</summary>
    public static UnifiedChunkData GetChunkAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
    {
        if (manager.CropData == null) return null;
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return null;
        return manager.CropData.GetChunk(sectionId, manager.WorldToChunkCoords(worldPos));
    }

    /// <summary>Morph a crop's PlantId to a hybrid (crossbreeding).</summary>
    public static bool SetCropPlantId(this WorldDataManager manager, Vector3 worldPos,
                                       string newPlantId, byte startStage)
        => manager.CropData?.SetCropPlantId(worldPos, newPlantId, startStage) ?? false;

    public static bool SetPollinatedAtWorldPosition(this WorldDataManager manager,
                                                     Vector3 worldPos, bool value)
        => manager.CropData?.SetPollinatedAtWorldPosition(worldPos, value) ?? false;

    public static bool IsPollinatedAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
        => manager.CropData?.IsPollinatedAtWorldPosition(worldPos) ?? false;
}
