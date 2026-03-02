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
            ChunkStartX = -8, 
            ChunkStartY = 2, 
            ChunksWidth = 6, 
            ChunksHeight = 4,
            DebugColor = new Color(1f, 0.5f, 0.5f, 0.3f)
        },
        
        
    };
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showSectionBounds = true;
    public bool showChunkGrid = false;
    
    // Data modules
    private Dictionary<string, IWorldDataModule> modules = new Dictionary<string, IWorldDataModule>();
    private CropDataModule cropModule;
    private StructureDataModule structureModule;
    
    // Quick lookup: chunkPosition -> sectionId
    private Dictionary<Vector2Int, int> chunkToSectionMap = new Dictionary<Vector2Int, int>();
    
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    // --- World Meta (populated by WorldDataBootstrapper) ---
    [Header("World Meta")]
    [SerializeField] private string worldName;
    [SerializeField] private int day;
    [SerializeField] private int month;
    [SerializeField] private int year;
    [SerializeField] private int hour;
    [SerializeField] private int minute;
    [SerializeField] private int gold;

    public string WorldName  => worldName;
    public int Day    => day;
    public int Month  => month;
    public int Year   => year;
    public int Hour   => hour;
    public int Minute => minute;
    public int Gold   => gold;

    /// <summary>Called by WorldDataBootstrapper to load world time/economy data.</summary>
    public void PopulateWorldMeta(WorldApiResponse data)
    {
        worldName = data.worldName;
        day       = data.day;
        month     = data.month;
        year      = data.year;
        hour      = data.hour;
        minute    = data.minute;
        gold      = data.gold;
        Debug.Log($"[WorldDataManager] World meta loaded: {worldName} | Day {day} | Gold {gold}");
    }

    // Public access to modules
    public CropDataModule      CropData      => cropModule;
    public StructureDataModule StructureData => structureModule;
    
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
        // Crop Module
        cropModule = new CropDataModule();
        cropModule.Initialize(this);
        modules[cropModule.ModuleName] = cropModule;

        // Structure Module
        structureModule = new StructureDataModule();
        structureModule.Initialize(this);
        modules[structureModule.ModuleName] = structureModule;

        // Future modules:
        // inventoryModule = new InventoryDataModule();
        // inventoryModule.Initialize(this);
        // modules[inventoryModule.ModuleName] = inventoryModule;
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
    
    // NOTE: Crop-related methods moved to WorldDataManagerCropExtensions.cs
    // This keeps WorldDataManager focused on core coordination (SOLID principle)
    // Usage remains identical: WorldDataManager.Instance.PlantCropAtWorldPosition(...)
    
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
        stats.TotalChunks   = chunkToSectionMap.Count;
        stats.MemoryUsageMB = GetMemoryUsageMB();

        var cropStats = cropModule.GetStats();
        stats.LoadedChunks    = (int)cropStats["LoadedChunks"];
        stats.TotalCrops      = (int)cropStats["TotalCrops"];
        stats.TotalTilledTiles = (int)cropStats["TotalTilledTiles"];
        stats.ChunksWithCrops  = (int)cropStats["ChunksWithCrops"];

        var structStats = structureModule.GetStats();
        stats.TotalStructures      = (int)structStats["TotalStructures"];
        stats.ChunksWithStructures = (int)structStats["ChunksWithStructures"];

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
    public int   TotalSections;
    public int   TotalChunks;
    public int   LoadedChunks;
    // Crop
    public int   ChunksWithCrops;
    public int   TotalCrops;
    public int   TotalTilledTiles;
    // Structure
    public int   TotalStructures;
    public int   ChunksWithStructures;
    public float MemoryUsageMB;
}
