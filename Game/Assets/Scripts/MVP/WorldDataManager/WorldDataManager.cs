using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent world data manager - uses absolute world positions
/// Much simpler: stores crops at their exact world X,Y coordinates
/// No complex chunk-relative calculations needed
/// </summary>
public class WorldDataManager : MonoBehaviour
{
    private static WorldDataManager _instance;
    public static WorldDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("WorldDataManager");
                _instance = go.AddComponent<WorldDataManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Section configuration with custom chunk ranges
    /// </summary>
    [System.Serializable]
    public class SectionConfig
    {
        [Tooltip("Section ID (0-3)")]
        public int SectionId;
        
        [Tooltip("Section name")]
        public string SectionName = "Section";
        
        [Tooltip("Starting chunk X coordinate (0-9)")]
        public int ChunkStartX;
        
        [Tooltip("Starting chunk Y coordinate (0-9)")]
        public int ChunkStartY;
        
        [Tooltip("Number of chunks wide")]
        public int ChunksWidth;
        
        [Tooltip("Number of chunks tall")]
        public int ChunksHeight;
        
        [Tooltip("Is this section active?")]
        public bool IsActive = true;
        
        [Tooltip("Background color for visualization")]
        public Color DebugColor = Color.green;
        
        /// <summary>
        /// Check if a chunk position belongs to this section
        /// </summary>
        public bool ContainsChunk(Vector2Int chunkPos)
        {
            return chunkPos.x >= ChunkStartX && 
                   chunkPos.x < ChunkStartX + ChunksWidth &&
                   chunkPos.y >= ChunkStartY && 
                   chunkPos.y < ChunkStartY + ChunksHeight;
        }
        
        /// <summary>
        /// Check if a world position belongs to this section
        /// </summary>
        public bool ContainsWorldPosition(Vector3 worldPos, int chunkSize)
        {
            int worldMinX = ChunkStartX * chunkSize;
            int worldMinY = ChunkStartY * chunkSize;
            int worldMaxX = (ChunkStartX + ChunksWidth) * chunkSize;
            int worldMaxY = (ChunkStartY + ChunksHeight) * chunkSize;
            
            return worldPos.x >= worldMinX && worldPos.x < worldMaxX &&
                   worldPos.y >= worldMinY && worldPos.y < worldMaxY;
        }
        
        public int GetTotalChunks() => ChunksWidth * ChunksHeight;
    }
    
    [Header("World Configuration")]
    [Tooltip("Total world size in chunks (10×10 grid)")]
    public int worldWidthChunks = 10;
    public int worldHeightChunks = 10;
    
    [Tooltip("Size of each chunk in tiles (30×30)")]
    public int chunkSizeTiles = 30;
    
    [Header("Section Definitions")]
    [Tooltip("Define your 4 sections")]
    public List<SectionConfig> sectionConfigs = new List<SectionConfig>()
    {
        // Section 1: 5×4 chunks at (0,6) = World pos (0,180) to (150,300)
        new SectionConfig 
        { 
            SectionId = 0, 
            SectionName = "Section 1",
            ChunkStartX = 0, 
            ChunkStartY = 6, 
            ChunksWidth = 5, 
            ChunksHeight = 4,
            DebugColor = new Color(1f, 0.5f, 0.5f, 0.3f)
        },
        
        // Section 2: 5×5 chunks at (0,0) = World pos (0,0) to (150,150)
        new SectionConfig 
        { 
            SectionId = 1, 
            SectionName = "Section 2",
            ChunkStartX = 0, 
            ChunkStartY = 0, 
            ChunksWidth = 5, 
            ChunksHeight = 5,
            DebugColor = new Color(0.5f, 1f, 0.5f, 0.3f)
        },
        
        // Section 3: 4×5 chunks at (5,0) = World pos (150,0) to (270,150)
        new SectionConfig 
        { 
            SectionId = 2, 
            SectionName = "Section 3",
            ChunkStartX = 5, 
            ChunkStartY = 0, 
            ChunksWidth = 4, 
            ChunksHeight = 5,
            DebugColor = new Color(0.5f, 0.5f, 1f, 0.3f)
        },
        
        // Section 4: 4×3 chunks at (5,7) = World pos (150,210) to (270,300)
        new SectionConfig 
        { 
            SectionId = 3, 
            SectionName = "Section 4",
            ChunkStartX = 5, 
            ChunkStartY = 7, 
            ChunksWidth = 4, 
            ChunksHeight = 3,
            DebugColor = new Color(1f, 1f, 0.5f, 0.3f)
        }
    };
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showSectionBounds = true;
    public bool showChunkGrid = false;
    
    // All world data: sections[sectionId][chunkPosition] = CropChunkData
    private Dictionary<int, Dictionary<Vector2Int, CropChunkData>> sections = 
        new Dictionary<int, Dictionary<Vector2Int, CropChunkData>>();
    
    // Quick lookup: chunkPosition -> sectionId
    private Dictionary<Vector2Int, int> chunkToSectionMap = new Dictionary<Vector2Int, int>();
    
    // Track initialization
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }
    
    private void Initialize()
    {
        if (isInitialized) return;
        
        int totalUsedChunks = 0;
        
        foreach (var config in sectionConfigs)
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
                    
                    // REMOVE OR COMMENT OUT THIS CHECK to allow negative chunks:
                    // if (worldChunkX < 0 || worldChunkX >= worldWidthChunks ||
                    //     worldChunkY < 0 || worldChunkY >= worldHeightChunks)
                    // {
                    //     Debug.LogWarning($"Chunk ({worldChunkX}, {worldChunkY}) is out of world bounds!");
                    //     continue;
                    // }
                    
                    sections[config.SectionId][chunkPos] = new CropChunkData
                    {
                        ChunkX = worldChunkX,
                        ChunkY = worldChunkY,
                        SectionId = config.SectionId,
                        IsLoaded = true
                    };
                    
                    chunkToSectionMap[chunkPos] = config.SectionId;
                    totalUsedChunks++;
                }
            }
        }
        
        isInitialized = true;
        
        if (showDebugLogs)
        {
            int totalPossibleChunks = worldWidthChunks * worldHeightChunks;
            int unusedChunks = totalPossibleChunks - totalUsedChunks;
            
            Debug.Log($"[WorldData] Initialized {sections.Count} sections\n" +
                      $"Total chunks: {totalUsedChunks}/{totalPossibleChunks} ({unusedChunks} unused)\n" +
                      $"Chunk size: {chunkSizeTiles}×{chunkSizeTiles} tiles\n" +
                      $"World size: {worldWidthChunks * chunkSizeTiles}×{worldHeightChunks * chunkSizeTiles} tiles");
            
            foreach (var config in sectionConfigs)
            {
                if (config.IsActive)
                {
                    int worldMinX = config.ChunkStartX * chunkSizeTiles;
                    int worldMinY = config.ChunkStartY * chunkSizeTiles;
                    int worldMaxX = (config.ChunkStartX + config.ChunksWidth) * chunkSizeTiles;
                    int worldMaxY = (config.ChunkStartY + config.ChunksHeight) * chunkSizeTiles;
                    
                    Debug.Log($"  {config.SectionName}: {config.GetTotalChunks()} chunks | " +
                              $"World pos ({worldMinX},{worldMinY}) to ({worldMaxX},{worldMaxY})");
                }
            }
        }
    }
    
    /// <summary>
    /// Get section ID from world position
    /// </summary>
    public int GetSectionIdFromWorldPosition(Vector3 worldPos)
    {
        foreach (var config in sectionConfigs)
        {
            if (!config.IsActive) continue;
            
            if (config.ContainsWorldPosition(worldPos, chunkSizeTiles))
            {
                return config.SectionId;
            }
        }
        
        return -1; // Not in any section
    }
    
    /// <summary>
    /// Convert world position to chunk coordinates
    /// </summary>
    public Vector2Int WorldToChunkCoords(Vector3 worldPos)
    {
        // Handle negative coordinates properly
        int chunkX = Mathf.FloorToInt(worldPos.x / chunkSizeTiles);
        int chunkY = Mathf.FloorToInt(worldPos.y / chunkSizeTiles);
        
        // Don't clamp - let it return actual chunk position for better debugging
        return new Vector2Int(chunkX, chunkY);
    }
    
    /// <summary>
    /// Convert chunk coordinates to world position (bottom-left corner)
    /// </summary>
    public Vector3 ChunkToWorldPosition(Vector2Int chunkPos)
    {
        return new Vector3(
            chunkPos.x * chunkSizeTiles, 
            chunkPos.y * chunkSizeTiles, 
            0
        );
    }
    
    /// <summary>
    /// Check if a world position is in any active section
    /// </summary>
    public bool IsPositionInActiveSection(Vector3 worldPos)
    {
        return GetSectionIdFromWorldPosition(worldPos) != -1;
    }
    
    /// <summary>
    /// Get chunk at world position
    /// </summary>
    public CropChunkData GetChunkAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return null;
        
        Vector2Int chunkPos = WorldToChunkCoords(worldPos);
        return GetChunk(sectionId, chunkPos);
    }
    
    /// <summary    >
    /// Get a specific chunk
    /// </summary>
    public CropChunkData GetChunk(int sectionId, Vector2Int chunkPos)
    {
        if (!sections.ContainsKey(sectionId))
        {
            return null;
        }
        
        if (!sections[sectionId].ContainsKey(chunkPos))
        {
            return null;
        }
        
        return sections[sectionId][chunkPos];
    }
    
    /// <summary>
    /// Plant crop at ABSOLUTE WORLD POSITION (X, Y)
    /// </summary>
    public bool PlantCropAtWorldPosition(Vector3 worldPos, ushort cropTypeID)
    {
        // Declare chunkPos ONCE at the top
        Vector2Int chunkPos = WorldToChunkCoords(worldPos);
        
        // Get section ID
        int sectionId = GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1)
        {
            if (showDebugLogs)
            {
                // Now just use it, don't redeclare
                Debug.LogWarning($"Cannot plant at world pos ({worldPos.x:F1}, {worldPos.y:F1}): " +
                           $"Chunk ({chunkPos.x}, {chunkPos.y}) is not in any active section.\n" +
                           $"Valid sections:\n" +
                           GetValidSectionsInfo());
            }
            return false;
        }
        
        // Remove this line since we already declared it above
        // Vector2Int chunkPos = WorldToChunkCoords(worldPos);
        
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        
        if (chunk == null)
        {
            Debug.LogWarning($"Cannot plant: chunk ({chunkPos.x}, {chunkPos.y}) not found in section {sectionId}. " +
                    $"This shouldn't happen - please report this bug.");
            return false;
        }
        
        // Store crop at absolute world position
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        bool success = chunk.PlantCrop(cropTypeID, worldX, worldY);
        
        if (success && showDebugLogs)
        {
            Debug.Log($"✓ Planted crop type {cropTypeID} at world pos ({worldX}, {worldY}) " +
                 $"[Chunk: ({chunkPos.x}, {chunkPos.y}), Section: {sectionConfigs[sectionId].SectionName}]");
        }
        
        return success;
    }
    
    /// <summary>
    /// Remove crop at world position
    /// </summary>
    public bool RemoveCropAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.RemoveCrop(worldX, worldY);
    }
    
    /// <summary>
    /// Check if crop exists at world position
    /// </summary>
    public bool HasCropAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.HasCrop(worldX, worldY);
    }
    
    /// <summary>
    /// Get crop at world position
    /// </summary>
    public bool TryGetCropAtWorldPosition(Vector3 worldPos, out CropChunkData.CompactCrop crop)
    {
        crop = default;
        
        int sectionId = GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = WorldToChunkCoords(worldPos);
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
        int sectionId = GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return false;
        
        Vector2Int chunkPos = WorldToChunkCoords(worldPos);
        CropChunkData chunk = GetChunk(sectionId, chunkPos);
        if (chunk == null) return false;
        
        int worldX = Mathf.FloorToInt(worldPos.x);
        int worldY = Mathf.FloorToInt(worldPos.y);
        
        return chunk.UpdateCropStage(worldX, worldY, newStage);
    }
    
    /// <summary>
    /// Get all chunks in a section
    /// </summary>
    public Dictionary<Vector2Int, CropChunkData> GetSection(int sectionId)
    {
        return sections.ContainsKey(sectionId) ? sections[sectionId] : null;
    }
    
    /// <summary>
    /// Get section configuration
    /// </summary>
    public SectionConfig GetSectionConfig(int sectionId)
    {
        foreach (var config in sectionConfigs)
        {
            if (config.SectionId == sectionId)
            {
                return config;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get memory usage estimate in MB
    /// </summary>
    public float GetMemoryUsageMB()
    {
        int totalCrops = 0;
        foreach (var section in sections.Values)
        {
            foreach (var chunk in section.Values)
            {
                totalCrops += chunk.plantedCrops.Count;
            }
        }
        
        float bytes = totalCrops * 25f;
        bytes += sections.Count * 1000;
        
        return bytes / (1024f * 1024f);
    }
    
    /// <summary>
    /// Get detailed statistics
    /// </summary>
    public WorldDataStats GetStats()
    {
        WorldDataStats stats = new WorldDataStats();
        stats.TotalSections = sections.Count;
        stats.TotalChunks = 0;
        stats.LoadedChunks = 0;
        stats.TotalCrops = 0;
        
        foreach (var section in sections.Values)
        {
            stats.TotalChunks += section.Count;
            
            foreach (var chunk in section.Values)
            {
                if (chunk.IsLoaded)
                {
                    stats.LoadedChunks++;
                }
                
                int cropCount = chunk.GetCropCount();
                stats.TotalCrops += cropCount;
                
                if (cropCount > 0)
                {
                    stats.ChunksWithCrops++;
                }
            }
        }
        
        stats.MemoryUsageMB = GetMemoryUsageMB();
        
        return stats;
    }
    
    public void LogStats()
    {
        WorldDataStats stats = GetStats();
        
        Debug.Log($"=== World Data Statistics ===\n" +
                  $"Sections: {stats.TotalSections}\n" +
                  $"Total Chunks: {stats.TotalChunks}\n" +
                  $"Loaded Chunks: {stats.LoadedChunks}\n" +
                  $"Chunks with Crops: {stats.ChunksWithCrops}\n" +
                  $"Total Crops: {stats.TotalCrops}\n" +
                  $"Memory Usage: {stats.MemoryUsageMB:F2} MB");
    }
    
    public void ClearAllData()
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
            Debug.Log("[WorldData] All data cleared");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (showSectionBounds)
        {
            foreach (var config in sectionConfigs)
            {
                if (!config.IsActive) continue;
                
                Vector3 min = new Vector3(
                    config.ChunkStartX * chunkSizeTiles, 
                    config.ChunkStartY * chunkSizeTiles, 
                    0
                );
                Vector3 size = new Vector3(
                    config.ChunksWidth * chunkSizeTiles, 
                    config.ChunksHeight * chunkSizeTiles, 
                    1
                );
                
                Gizmos.color = config.DebugColor;
                Gizmos.DrawWireCube(min + size / 2, size);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(min + size / 2, 
                    $"{config.SectionName}\n({min.x:F0},{min.y:F0}) to ({min.x + size.x:F0},{min.y + size.y:F0})");
                #endif
            }
        }
        
        if (showChunkGrid && isInitialized)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            foreach (var kvp in chunkToSectionMap)
            {
                Vector2Int chunkPos = kvp.Key;
                Vector3 worldPos = ChunkToWorldPosition(chunkPos);
                Vector3 chunkSize = new Vector3(chunkSizeTiles, chunkSizeTiles, 1);
                
                Gizmos.DrawWireCube(worldPos + chunkSize / 2, chunkSize);
            }
        }
    }
    /// <summary>
/// Get info about valid sections for debugging
/// </summary>
private string GetValidSectionsInfo()
{
    string info = "";
    foreach (var config in sectionConfigs)
    {
        if (!config.IsActive) continue;
        
        int worldMinX = config.ChunkStartX * chunkSizeTiles;
        int worldMinY = config.ChunkStartY * chunkSizeTiles;
        int worldMaxX = (config.ChunkStartX + config.ChunksWidth) * chunkSizeTiles;
        int worldMaxY = (config.ChunkStartY + config.ChunksHeight) * chunkSizeTiles;
        
        info += $"  • {config.SectionName}: World ({worldMinX},{worldMinY}) to ({worldMaxX},{worldMaxY}) " +
                $"[Chunks {config.ChunkStartX}-{config.ChunkStartX + config.ChunksWidth - 1}, " +
                $"{config.ChunkStartY}-{config.ChunkStartY + config.ChunksHeight - 1}]\n";
    }
    return info;
}
}


[System.Serializable]
public struct WorldDataStats
{
    public int TotalSections;
    public int TotalChunks;
    public int LoadedChunks;
    public int ChunksWithCrops;
    public int TotalCrops;
    public float MemoryUsageMB;
}


