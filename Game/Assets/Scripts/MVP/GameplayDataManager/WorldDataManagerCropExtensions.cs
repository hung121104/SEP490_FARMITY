using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Extension methods for WorldDataManager - Crop operations
/// Provides backward compatibility and clean API for crop-related operations
/// Keeps WorldDataManager focused on core coordination (SOLID - Single Responsibility)
/// </summary>
public static class WorldDataManagerCropExtensions
{
    /// <summary>
    /// Plant crop at world position
    /// </summary>
    public static bool PlantCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos, string plantId)
    {
        return manager.CropData?.PlantCropAtWorldPosition(worldPos, plantId) ?? false;
    }
    
    /// <summary>
    /// Remove crop at world position
    /// </summary>
    public static bool RemoveCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
    {
        return manager.CropData?.RemoveCropAtWorldPosition(worldPos) ?? false;
    }
    
    /// <summary>
    /// Check if crop exists at world position
    /// </summary>
    public static bool HasCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
    {
        return manager.CropData?.HasCropAtWorldPosition(worldPos) ?? false;
    }
    
    /// <summary>
    /// Try to get crop data at world position
    /// </summary>
    public static bool TryGetCropAtWorldPosition(this WorldDataManager manager, Vector3 worldPos, out CropChunkData.TileData crop)
    {
        crop = default;
        if (manager.CropData == null) return false;
        return manager.CropData.TryGetCropAtWorldPosition(worldPos, out crop);
    }
    
    /// <summary>
    /// Update crop growth stage
    /// </summary>
    public static bool UpdateCropStage(this WorldDataManager manager, Vector3 worldPos, byte newStage)
    {
        return manager.CropData?.UpdateCropStage(worldPos, newStage) ?? false;
    }
    
    /// <summary>
    /// Update crop age (days since planting)
    /// </summary>
    public static bool UpdateCropAge(this WorldDataManager manager, Vector3 worldPos, int newAge)
    {
        return manager.CropData?.UpdateCropAge(worldPos, newAge) ?? false;
    }
    
    /// <summary>
    /// Increment crop age by 1 day
    /// </summary>
    public static bool IncrementCropAge(this WorldDataManager manager, Vector3 worldPos)
    {
        return manager.CropData?.IncrementCropAge(worldPos) ?? false;
    }
    
    /// <summary>
    /// Till a tile at world position
    /// </summary>
    public static bool TillTileAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
    {
        return manager.CropData?.TillTileAtWorldPosition(worldPos) ?? false;
    }
    
    /// <summary>
    /// Remove tilled status from tile
    /// </summary>
    public static bool UntillTileAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
    {
        return manager.CropData?.UntillTileAtWorldPosition(worldPos) ?? false;
    }
    
    /// <summary>
    /// Check if tile is tilled
    /// </summary>
    public static bool IsTilledAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
    {
        return manager.CropData?.IsTilledAtWorldPosition(worldPos) ?? false;
    }
    
    /// <summary>
    /// Get crop chunk data by section and chunk position
    /// </summary>
    public static CropChunkData GetChunk(this WorldDataManager manager, int sectionId, Vector2Int chunkPos)
    {
        return manager.CropData?.GetChunk(sectionId, chunkPos);
    }
    
    /// <summary>
    /// Get all crop chunks in a section
    /// </summary>
    public static Dictionary<Vector2Int, CropChunkData> GetSection(this WorldDataManager manager, int sectionId)
    {
        return manager.CropData?.GetSection(sectionId);
    }
    
    /// <summary>
    /// Get crop chunk at world position
    /// </summary>
    public static CropChunkData GetChunkAtWorldPosition(this WorldDataManager manager, Vector3 worldPos)
    {
        if (manager.CropData == null) return null;
        
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return null;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        return manager.CropData.GetChunk(sectionId, chunkPos);
    }
}
