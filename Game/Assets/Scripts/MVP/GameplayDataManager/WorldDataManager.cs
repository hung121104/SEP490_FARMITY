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
    private InventoryDataModule inventoryModule;
    
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

    /// <summary>
    /// Rebuilds all saved chunks into RAM from the API response.
    /// Called by WorldDataBootstrapper once the GET /player-data/world response arrives.
    ///
    /// For each chunk the method:
    ///   1. Derives the world-space origin of the chunk (chunkX * 30, chunkY * 30).
    ///   2. Converts each local tile index (0–899) back to world XY using:
    ///         localX = index % 30,  worldX = chunkX * 30 + localX
    ///         localY = index / 30,  worldY = chunkY * 30 + localY
    ///   3. Calls the appropriate WorldDataManager methods to recreate the tile state.
    /// </summary>
    public void PopulateChunks(System.Collections.Generic.List<ChunkResponseData> loadedChunks)
    {
        if (loadedChunks == null || loadedChunks.Count == 0) return;

        int tilesApplied = 0;

        foreach (var chunk in loadedChunks)
        {
            if (chunk.tiles == null || chunk.tiles.Count == 0) continue;

            int originX = chunk.chunkX * chunkSizeTiles;   // world X of tile (0,0) in this chunk
            int originY = chunk.chunkY * chunkSizeTiles;   // world Y of tile (0,0) in this chunk

            foreach (var kvp in chunk.tiles)
            {
                // Parse the string key back to integer tile index
                if (!int.TryParse(kvp.Key, out int localIndex)) continue;
                TileResponseData td = kvp.Value;
                if (td == null) continue;

                int localX = localIndex % chunkSizeTiles;
                int localY = localIndex / chunkSizeTiles;
                int worldX = originX + localX;
                int worldY = originY + localY;

                var worldPos = new UnityEngine.Vector3(worldX, worldY, 0);

                // ── Restore tilled ground ──
                if (td.type == "tilled" || td.type == "crop")
                {
                    this.TillTileAtWorldPosition(worldPos);
                }

                // ── Restore crop ──
                if (td.type == "crop" && !string.IsNullOrEmpty(td.plantId))
                {
                    this.PlantCropAtWorldPosition(worldPos, td.plantId);

                    // Restore all crop sub-fields
                    if (td.cropStage > 0)
                        this.UpdateCropStage(worldPos, (byte)td.cropStage);

                    if (td.growthTimer > 0f)
                        this.UpdateGrowthTimer(worldPos, td.growthTimer);

                    // Watered / Fertilized / Pollinated — use the CropData module directly
                    if (CropData != null)
                    {
                        var chunkPos  = WorldToChunkCoords(worldPos);
                        int sectionId = GetSectionIdFromWorldPosition(worldPos);
                        var chunkData = CropData.GetChunk(sectionId, chunkPos);

                        if (chunkData != null)
                        {
                            if (td.isWatered)    chunkData.WaterTile(worldX, worldY);
                            if (td.isFertilized)  chunkData.FertilizeTile(worldX, worldY);
                            if (td.isPollinated)  chunkData.SetPollinated(worldX, worldY, true);

                            if (td.pollenHarvestCount > 0)
                            {
                                for (int i = 0; i < td.pollenHarvestCount; i++)
                                    chunkData.IncrementPollenHarvestCount(worldX, worldY);
                            }
                        }
                    }
                }

                // —— Restore structure ——
                tilesApplied++;
            }
        }

        Debug.Log($"[WorldDataManager] PopulateChunks done: {loadedChunks.Count} chunk(s), {tilesApplied} tile(s) restored.");
    }



    // Public access to modules
    public CropDataModule      CropData      => cropModule;
    public StructureDataModule StructureData => structureModule;
    public InventoryDataModule InventoryData => inventoryModule;
    
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

        // Inventory Module
        inventoryModule = new InventoryDataModule();
        inventoryModule.Initialize(this);
        modules[inventoryModule.ModuleName] = inventoryModule;
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

    /// <summary>
    /// Get all character IDs with cached inventory data
    /// </summary>
    public List<string> GetAllCharacterIds()
    {
        return inventoryModule != null
            ? new List<string>(inventoryModule.GetAllCharacterIds())
            : new List<string>();
    }

    /// <summary>
    /// Get debug information about a character's inventory
    /// </summary>
    public CharacterInventoryDebugInfo GetCharacterInventoryDebugInfo(string characterId)
    {
        var info = new CharacterInventoryDebugInfo();

        if (inventoryModule == null)
        {
            info.IsValid = false;
            return info;
        }

        var inventory = inventoryModule.GetInventory(characterId);
        if (inventory == null)
        {
            info.IsValid = false;
            return info;
        }

        info.IsValid = true;
        info.CharacterId = characterId;
        info.OccupiedSlots = inventory.OccupiedSlotCount;
        info.TotalItems = 0;
        info.Items = new List<CharacterInventoryDebugInfo.ItemInfo>();

        foreach (var slot in inventory.GetAllSlots())
        {
            info.TotalItems += slot.Quantity;
            info.Items.Add(new CharacterInventoryDebugInfo.ItemInfo
            {
                SlotIndex = slot.SlotIndex,
                ItemId = slot.ItemId,
                Quantity = slot.Quantity
            });
        }

        return info;
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

        var invStats = inventoryModule.GetStats();
        stats.InventoryCharacters = (int)invStats["Characters"];
        stats.InventoryOccupiedSlots = (int)invStats["OccupiedSlots"];
        stats.InventoryTotalItems = (int)invStats["TotalItems"];

        return stats;
    }
    
    public void LogStats()
    {
        WorldDataStats stats = GetStats();
        
        string log = $"========== World Data Statistics ==========\n" +
                     $"Sections: {stats.TotalSections}\n" +
                     $"Total Chunks: {stats.TotalChunks}\n" +
                     $"Loaded Chunks: {stats.LoadedChunks}\n\n" +
                     
                     $"--- Crops ---\n" +
                     $"  Chunks with Crops: {stats.ChunksWithCrops}\n" +
                     $"  Total Crops: {stats.TotalCrops}\n" +
                     $"  Total Tilled Tiles: {stats.TotalTilledTiles}\n\n" +
                     
                     $"--- Structures ---\n" +
                     $"  Total Structures: {stats.TotalStructures}\n" +
                     $"  Chunks with Structures: {stats.ChunksWithStructures}\n\n" +
                     
                     $"--- Inventory ---\n" +
                     $"  Cached Characters: {stats.InventoryCharacters}\n" +
                     $"  Occupied Slots: {stats.InventoryOccupiedSlots}\n" +
                     $"  Total Items: {stats.InventoryTotalItems}\n\n" +
                     
                     $"Memory Usage: {stats.MemoryUsageMB:F3} MB\n" +
                     $"==========================================";
        
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
    // Inventory
    public int   InventoryCharacters;
    public int   InventoryOccupiedSlots;
    public int   InventoryTotalItems;
    public float MemoryUsageMB;
}

/// <summary>
/// Debug information structure for character inventory display in editor
/// </summary>
[System.Serializable]
public struct CharacterInventoryDebugInfo
{
    public bool IsValid;
    public string CharacterId;
    public int OccupiedSlots;
    public int TotalItems;
    public List<ItemInfo> Items;
    
    [System.Serializable]
    public struct ItemInfo
    {
        public byte SlotIndex;
        public string ItemId;
        public ushort Quantity;
    }
}
