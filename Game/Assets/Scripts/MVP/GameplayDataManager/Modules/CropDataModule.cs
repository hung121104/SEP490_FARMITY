using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all crop and tilling data in the world
/// Separated from WorldDataManager for modularity
/// </summary>
public class CropDataModule : IWorldDataModule
{
    public string ModuleName => "Crop Data";
    
    private WorldDataManager manager;
    private bool showDebugLogs = true;
    
    // All crop data: sections[sectionId][chunkPosition] = CropChunkData
    private Dictionary<int, Dictionary<Vector2Int, CropChunkData>> sections = 
        new Dictionary<int, Dictionary<Vector2Int, CropChunkData>>();
    
    public void Initialize(WorldDataManager manager)
    {
        this.manager = manager;
        this.showDebugLogs = manager.showDebugLogs;
        
        // Initialize sections based on world configuration
        foreach (var config in manager.sectionConfigs)
        {
            if (!config.IsActive) continue;
            
            sections[config.SectionId] = new Dictionary<Vector2Int, CropChunkData>();
            
            for (int x = 0; x < config.ChunksWidth; x++)
            {
                for (int y = 0; y < config.ChunksHeight; y++)
                {
                    int worldChunkX = config.ChunkStartX + x;
                    int worldChunkY = config.ChunkStartY + y;
                    Vector2Int chunkPos = new Vector2Int(worldChunkX, worldChunkY);
                    
                    sections[config.SectionId][chunkPos] = new CropChunkData
                    {
                        ChunkX = worldChunkX,
                        ChunkY = worldChunkY,
                        SectionId = config.SectionId,
                        IsLoaded = true
                    };
                }
            }
        }
        
        if (showDebugLogs)
        {
            int totalChunks = 0;
            foreach (var section in sections.Values)
            {
                totalChunks += section.Count;
            }
            Debug.Log($"[CropDataModule] Initialized with {sections.Count} sections, {totalChunks} chunks");
        }
    }
    
    /// <summary>
    /// Plant crop at ABSOLUTE WORLD POSITION (X, Y)
    /// </summary>
    public bool PlantCropAtWorldPosition(Vector3 worldPos, string plantId)
    {
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        
        if (sectionId == -1)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"Cannot plant at world pos ({worldPos.x:F1}, {worldPos.y:F1}): " +
                           $"Chunk ({chunkPos.x}, {chunkPos.y}) is not in any active section.");
            }
            return false;
        }
        
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null)
        {
            Debug.LogWarning($"Cannot plant: chunk ({chunkPos.x}, {chunkPos.y}) not found in section {sectionId}.");
            return false;
        }
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        bool success = chunk.PlantCrop(plantId, worldX, worldY);
        
        if (success && showDebugLogs)
        {
            var config = manager.GetSectionConfig(sectionId);
            Debug.Log($"✓ Planted plant '{plantId}' at world pos ({worldX}, {worldY}) " +
                 $"[Chunk: ({chunkPos.x}, {chunkPos.y}), Section: {config?.SectionName}]");
        }
        
        return success;
    }
    
    /// <summary>
    /// Remove crop at world position
    /// </summary>
    public bool RemoveCropAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.RemoveCrop(worldX, worldY);
    }
    
    /// <summary>
    /// Mark a tile as tilled at ABSOLUTE WORLD POSITION (X, Y)
    /// </summary>
    public bool TillTileAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        
        if (sectionId == -1)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"Cannot till at world pos ({worldPos.x:F1}, {worldPos.y:F1}): " +
                           $"Chunk ({chunkPos.x}, {chunkPos.y}) is not in any active section.");
            }
            return false;
        }
        
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null)
        {
            Debug.LogWarning($"Cannot till: chunk ({chunkPos.x}, {chunkPos.y}) not found in section {sectionId}.");
            return false;
        }
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        bool success = chunk.TillTile(worldX, worldY);
        
        if (success && showDebugLogs)
        {
            var config = manager.GetSectionConfig(sectionId);
            Debug.Log($"✓ Tilled tile at world pos ({worldX}, {worldY}) " +
                 $"[Chunk: ({chunkPos.x}, {chunkPos.y}), Section: {config?.SectionName}]");
        }
        
        return success;
    }
    
    /// <summary>
    /// Remove tilled status from a tile at world position
    /// </summary>
    public bool UntillTileAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.UntillTile(worldX, worldY);
    }
    
    /// <summary>
    /// Check if tile is tilled at world position
    /// </summary>
    public bool IsTilledAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.IsTilled(worldX, worldY);
    }
    
    /// <summary>
    /// Check if crop exists at world position
    /// </summary>
    public bool HasCropAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.HasCrop(worldX, worldY);
    }
    
    /// <summary>
    /// Get crop at world position
    /// </summary>
    public bool TryGetCropAtWorldPosition(Vector3 worldPos, out CropChunkData.TileData crop)
    {
        crop = default;
        
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.TryGetCrop(worldX, worldY, out crop);
    }
    
    /// <summary>
    /// Update crop stage at world position
    /// </summary>
    public bool UpdateCropStage(Vector3 worldPos, byte newStage)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.UpdateCropStage(worldX, worldY, newStage);
    }
    
    /// <summary>
    /// Update crop age at world position
    /// </summary>
    public bool UpdateCropAge(Vector3 worldPos, int newAge)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.UpdateCropAge(worldX, worldY, newAge);
    }
    
    /// <summary>
    /// Increment crop age at world position by 1 day
    /// </summary>
    public bool IncrementCropAge(Vector3 worldPos)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.IncrementCropAge(worldX, worldY);
    }
    
    /// <summary>Increments the pollen harvest count for the crop at world position by 1.</summary>
    public bool IncrementPollenHarvestCount(Vector3 worldPos)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;

        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;

        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        return chunk.IncrementPollenHarvestCount(worldX, worldY);
    }
    
    /// <summary>
    /// Get a specific chunk
    /// </summary>
    public CropChunkData GetChunk(int sectionId, Vector2Int chunkPos)
    {
        if (!sections.ContainsKey(sectionId)) return null;
        if (!sections[sectionId].ContainsKey(chunkPos)) return null;
        
        return sections[sectionId][chunkPos];
    }
    
    /// <summary>
    /// Get all chunks in a section
    /// </summary>
    public Dictionary<Vector2Int, CropChunkData> GetSection(int sectionId)
    {
        return sections.ContainsKey(sectionId) ? sections[sectionId] : null;
    }
    
    public void ClearAll()
    {
        foreach (var section in sections.Values)
        {
            foreach (var chunk in section.Values)
            {
                chunk.Clear();
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[CropDataModule] All data cleared");
        }
    }
    
    public float GetMemoryUsageMB()
    {
        int totalCrops = 0;
        foreach (var section in sections.Values)
        {
            foreach (var chunk in section.Values)
            {
                totalCrops += chunk.GetCropCount();
            }
        }
        
        float bytes = totalCrops * 25f;
        bytes += sections.Count * 1000;
        
        return bytes / (1024f * 1024f);
    }
    
    public Dictionary<string, object> GetStats()
    {
        var stats = new Dictionary<string, object>();
        
        int totalChunks = 0;
        int loadedChunks = 0;
        int totalCrops = 0;
        int totalTilledTiles = 0;
        int chunksWithCrops = 0;
        
        foreach (var section in sections.Values)
        {
            totalChunks += section.Count;
            
            foreach (var chunk in section.Values)
            {
                if (chunk.IsLoaded) loadedChunks++;
                
                int cropCount = chunk.GetCropCount();
                totalCrops += cropCount;
                totalTilledTiles += chunk.GetTilledCount();
                
                if (cropCount > 0) chunksWithCrops++;
            }
        }
        
        stats["TotalChunks"] = totalChunks;
        stats["LoadedChunks"] = loadedChunks;
        stats["TotalCrops"] = totalCrops;
        stats["TotalTilledTiles"] = totalTilledTiles;
        stats["ChunksWithCrops"] = chunksWithCrops;
        stats["MemoryUsageMB"] = GetMemoryUsageMB();
        
        return stats;
    }

    // ── Crossbreeding helpers ──────────────────────────────────────────────

    /// <summary>
    /// Morphs an existing crop's PlantId to a hybrid (crossbreeding).
    /// Resets CropStage to <paramref name="startStage"/> and marks IsPollinated.
    /// </summary>
    public bool SetCropPlantId(Vector3 worldPos, string newPlantId, byte startStage)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        Vector2Int chunkPos = manager.WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        int wx = Mathf.FloorToInt(worldPos.x);
        int wy = Mathf.FloorToInt(worldPos.y);
        bool ok = chunk.SetCropPlantId(wx, wy, newPlantId, startStage);
        if (ok && showDebugLogs)
            Debug.Log($"[CropDataModule] ✓ Crossbred crop at ({wx},{wy}) → '{newPlantId}' stage {startStage}");
        return ok;
    }

    public bool SetPollinatedAtWorldPosition(Vector3 worldPos, bool value)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        CropChunkData chunk = GetChunk(sectionId, manager.WorldToChunkCoords(worldPos));
        if (chunk == null) return false;
        return chunk.SetPollinated(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), value);
    }

    public bool IsPollinatedAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = manager.GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        CropChunkData chunk = GetChunk(sectionId, manager.WorldToChunkCoords(worldPos));
        if (chunk == null) return false;
        return chunk.IsPollinatedAt(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }
}

