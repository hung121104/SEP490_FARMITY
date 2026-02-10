using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core world data manager - coordinates all data modules
/// Provides shared utilities for world/chunk coordinate conversion
/// Modules handle specific data types (crops, inventory, structures, etc.)
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
    
    [Header("World Configuration")]
    [Tooltip("Total world size in chunks (10×10 grid)")]
    public int worldWidthChunks = 10;
    public int worldHeightChunks = 10;
    
    [Tooltip("Size of each chunk in tiles (30×30)")]
    public int chunkSizeTiles = 30;
    
    [Header("Section Definitions")]
    [Tooltip("Define your sections")]
    public List<WorldSectionConfig> sectionConfigs = new List<WorldSectionConfig>()
    {
        // Section 1: 5×4 chunks at (0,6) = World pos (0,180) to (150,300)
        new WorldSectionConfig 
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
        new WorldSectionConfig 
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
        new WorldSectionConfig 
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
        new WorldSectionConfig 
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
    
    // Data modules
    private Dictionary<string, IWorldDataModule> modules = new Dictionary<string, IWorldDataModule>();
    private CropDataModule cropModule;
    
    // Quick lookup: chunkPosition -> sectionId
    private Dictionary<Vector2Int, int> chunkToSectionMap = new Dictionary<Vector2Int, int>();
    
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    
    // Public access to modules
    public CropDataModule CropData => cropModule;
    
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
        
        // Build chunk-to-section lookup map
        int totalUsedChunks = 0;
        foreach (var config in sectionConfigs)
        {
            if (!config.IsActive) continue;
            
            for (int x = 0; x < config.ChunksWidth; x++)
            {
                for (int y = 0; y < config.ChunksHeight; y++)
                {
                    int worldChunkX = config.ChunkStartX + x;
                    int worldChunkY = config.ChunkStartY + y;
                    Vector2Int chunkPos = new Vector2Int(worldChunkX, worldChunkY);
                    
                    chunkToSectionMap[chunkPos] = config.SectionId;
                    totalUsedChunks++;
                }
            }
        }
        
        // Initialize modules
        InitializeModules();
        
        isInitialized = true;
        
        if (showDebugLogs)
        {
            int totalPossibleChunks = worldWidthChunks * worldHeightChunks;
            int unusedChunks = totalPossibleChunks - totalUsedChunks;
            
            Debug.Log($"[WorldDataManager] Initialized\n" +
                      $"Modules: {modules.Count}\n" +
                      $"Sections: {sectionConfigs.Count}\n" +
                      $"Total chunks: {totalUsedChunks}/{totalPossibleChunks} ({unusedChunks} unused)\n" +
                      $"Chunk size: {chunkSizeTiles}×{chunkSizeTiles} tiles\n" +
                      $"World size: {worldWidthChunks * chunkSizeTiles}×{worldHeightChunks * chunkSizeTiles} tiles");
            
            foreach (var config in sectionConfigs)
            {
                if (config.IsActive)
                {
                    var (min, max) = config.GetWorldBounds(chunkSizeTiles);
                    Debug.Log($"  {config.SectionName}: {config.GetTotalChunks()} chunks | " +
                              $"World pos ({min.x},{min.y}) to ({max.x},{max.y})");
                }
            }
        }
    }
    
    private void InitializeModules()
    {
        // Initialize Crop Module
        cropModule = new CropDataModule();
        cropModule.Initialize(this);
        modules[cropModule.ModuleName] = cropModule;
        
        // Future modules can be added here:
        // inventoryModule = new InventoryDataModule();
        // inventoryModule.Initialize(this);
        // modules[inventoryModule.ModuleName] = inventoryModule;
        
        // structureModule = new StructureDataModule();
        // structureModule.Initialize(this);
        // modules[structureModule.ModuleName] = structureModule;
    }
    
    #region Core Coordinate Utilities
    
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
    /// Get section configuration
    /// </summary>
    public WorldSectionConfig GetSectionConfig(int sectionId)
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
    
    #endregion
    
    #region Backward Compatibility - Crop Methods
    
    // These methods provide backward compatibility with existing code
    // They delegate to the CropDataModule
    
    public bool PlantCropAtWorldPosition(Vector3 worldPos, ushort cropTypeID)
        => cropModule.PlantCropAtWorldPosition(worldPos, cropTypeID);
    
    public bool RemoveCropAtWorldPosition(Vector3 worldPos)
        => cropModule.RemoveCropAtWorldPosition(worldPos);
    
    public bool HasCropAtWorldPosition(Vector3 worldPos)
        => cropModule.HasCropAtWorldPosition(worldPos);
    
    public bool TryGetCropAtWorldPosition(Vector3 worldPos, out CropChunkData.TileData crop)
        => cropModule.TryGetCropAtWorldPosition(worldPos, out crop);
    
    public bool UpdateCropStage(Vector3 worldPos, byte newStage)
        => cropModule.UpdateCropStage(worldPos, newStage);
    
    public bool TillTileAtWorldPosition(Vector3 worldPos)
        => cropModule.TillTileAtWorldPosition(worldPos);
    
    public bool UntillTileAtWorldPosition(Vector3 worldPos)
        => cropModule.UntillTileAtWorldPosition(worldPos);
    
    public bool IsTilledAtWorldPosition(Vector3 worldPos)
        => cropModule.IsTilledAtWorldPosition(worldPos);
    
    public CropChunkData GetChunk(int sectionId, Vector2Int chunkPos)
        => cropModule.GetChunk(sectionId, chunkPos);
    
    public Dictionary<Vector2Int, CropChunkData> GetSection(int sectionId)
        => cropModule.GetSection(sectionId);
    
    public CropChunkData GetChunkAtWorldPosition(Vector3 worldPos)
    {
        int sectionId = GetSectionIdFromWorldPosition(worldPos);
        if (sectionId == -1) return null;
        
        Vector2Int chunkPos = WorldToChunkCoords(worldPos);
        return cropModule.GetChunk(sectionId, chunkPos);
    }
    
    #endregion
    
    #region Statistics and Management
    
    /// <summary>
    /// Get memory usage estimate in MB
    /// </summary>
    public float GetMemoryUsageMB()
    {
        float total = 0f;
        foreach (var module in modules.Values)
        {
            total += module.GetMemoryUsageMB();
        }
        return total;
    }
    
    /// <summary>
    /// Get detailed statistics
    /// </summary>
    public WorldDataStats GetStats()
    {
        WorldDataStats stats = new WorldDataStats();
        stats.TotalSections = sectionConfigs.Count;
        stats.TotalChunks = chunkToSectionMap.Count;
        stats.MemoryUsageMB = GetMemoryUsageMB();
        
        // Get crop-specific stats
        var cropStats = cropModule.GetStats();
        stats.LoadedChunks = (int)cropStats["LoadedChunks"];
        stats.TotalCrops = (int)cropStats["TotalCrops"];
        stats.TotalTilledTiles = (int)cropStats["TotalTilledTiles"];
        stats.ChunksWithCrops = (int)cropStats["ChunksWithCrops"];
        
        return stats;
    }
    
    public void LogStats()
    {
        WorldDataStats stats = GetStats();
        
        string log = $"=== World Data Statistics ===\n" +
                     $"Sections: {stats.TotalSections}\n" +
                     $"Total Chunks: {stats.TotalChunks}\n" +
                     $"Loaded Chunks: {stats.LoadedChunks}\n" +
                     $"Memory Usage: {stats.MemoryUsageMB:F2} MB\n\n";
        
        foreach (var kvp in modules)
        {
            log += $"--- {kvp.Key} ---\n";
            var moduleStats = kvp.Value.GetStats();
            foreach (var statKvp in moduleStats)
            {
                log += $"  {statKvp.Key}: {statKvp.Value}\n";
            }
            log += "\n";
        }
        
        Debug.Log(log);
    }
    
    public void ClearAllData()
    {
        foreach (var module in modules.Values)
        {
            module.ClearAll();
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[WorldDataManager] All data cleared");
        }
    }
    
    #endregion
    
    #region Debug Visualization
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (showSectionBounds)
        {
            foreach (var config in sectionConfigs)
            {
                if (!config.IsActive) continue;
                
                var (min, max) = config.GetWorldBounds(chunkSizeTiles);
                Vector3 minV3 = new Vector3(min.x, min.y, 0);
                Vector3 size = new Vector3(max.x - min.x, max.y - min.y, 1);
                
                Gizmos.color = config.DebugColor;
                Gizmos.DrawWireCube(minV3 + size / 2, size);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(minV3 + size / 2, 
                    $"{config.SectionName}\n({min.x},{min.y}) to ({max.x},{max.y})");
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
    
    #endregion
}

[System.Serializable]
public struct WorldDataStats
{
    public int TotalSections;
    public int TotalChunks;
    public int LoadedChunks;
    public int ChunksWithCrops;
    public int TotalCrops;
    public int TotalTilledTiles;
    public float MemoryUsageMB;
}
