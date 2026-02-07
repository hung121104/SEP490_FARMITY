using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// Manages crop growth over time by listening to the TimeManagerView's day change events.
/// Updates crop stages in WorldDataManager and refreshes visual representations.
/// </summary>
public class CropManagerView : MonoBehaviourPunCallbacks
{
    public static CropManagerView Instance { get; private set; }

    [Header("References")]
    [Tooltip("Reference to the TimeManagerView to listen for day changes")]
    public TimeManagerView timeManager;
    
    [Tooltip("Reference to WorldDataManager for crop data")]
    private WorldDataManager worldDataManager;
    
    [Tooltip("Reference to ChunkLoadingManager for visual updates")]
    private ChunkLoadingManager chunkLoadingManager;

    [Header("Plant Data")]
    [Tooltip("Array of all plant data ScriptableObjects indexed by CropTypeID")]
    public PlantDataSO[] plantDatabase;

    [Header("Growth Settings")]
    [Tooltip("Enable/disable automatic crop growth")]
    public bool enableGrowth = true;
    
    [Tooltip("Growth speed multiplier (1.0 = normal speed)")]
    [Range(0.1f, 10f)]
    public float growthSpeedMultiplier = 1f;

    [Header("Visual")]
    [Tooltip("Parent transform for crop visual GameObjects")]
    public Transform cropVisualsParent;

    [Header("Debug")]
    public bool showDebugLogs = true;
    
    // Track when each crop was planted (worldX_worldY -> planting day)
    private Dictionary<string, CropGrowthData> cropGrowthTracker = new Dictionary<string, CropGrowthData>();
    
    // Visual representation (worldX_worldY -> GameObject)
    private Dictionary<string, GameObject> cropVisuals = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[CropManagerView] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeManagers();
        
        if (cropVisualsParent == null)
        {
            GameObject parent = new GameObject("CropVisuals");
            cropVisualsParent = parent.transform;
        }
    }

    private void InitializeManagers()
    {
        // Find TimeManagerView if not assigned
        if (timeManager == null)
        {
            timeManager = FindAnyObjectByType<TimeManagerView>();
            if (timeManager == null)
            {
                Debug.LogError("[CropManagerView] TimeManagerView not found in scene!");
                return;
            }
        }

        // Subscribe to day change event
        timeManager.OnDayChanged += OnDayChanged;
        
        // Get WorldDataManager
        worldDataManager = WorldDataManager.Instance;
        if (worldDataManager == null)
        {
            Debug.LogError("[CropManagerView] WorldDataManager not found!");
        }
        
        // Get ChunkLoadingManager
        chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
        if (chunkLoadingManager == null)
        {
            Debug.LogWarning("[CropManagerView] ChunkLoadingManager not found!");
        }

        if (showDebugLogs)
        {
            Debug.Log("[CropManagerView] Initialized successfully");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // Unsubscribe from events
        if (timeManager != null)
        {
            timeManager.OnDayChanged -= OnDayChanged;
        }
    }

    /// <summary>
    /// Called when a new day begins in the game
    /// </summary>
    private void OnDayChanged()
    {
        if (!enableGrowth) return;

        if (showDebugLogs)
        {
            Debug.Log($"[CropManagerView] Day changed! Growing all crops...");
        }

        GrowAllCrops();
    }

    /// <summary>
    /// Grow all crops in the world by one day
    /// </summary>
    private void GrowAllCrops()
    {
        if (worldDataManager == null) return;

        int cropsGrown = 0;
        int cropsHarvested = 0;

        // Get all sections and iterate through chunks
        var stats = worldDataManager.GetStats();
        
        for (int s = 0; s < worldDataManager.sectionConfigs.Count; s++)
        {
            var sectionConfig = worldDataManager.sectionConfigs[s];
            if (!sectionConfig.IsActive) continue;

            var section = worldDataManager.GetSection(sectionConfig.SectionId);
            if (section == null) continue;

            foreach (var chunkPair in section)
            {
                CropChunkData chunk = chunkPair.Value;
                bool chunkModified = false;

                // Get all crops in this chunk
                var allCrops = chunk.GetAllCrops();

                foreach (var tile in allCrops)
                {
                    if (!tile.HasCrop) continue;

                    string key = GetCropKey(tile.WorldX, tile.WorldY);
                    
                    // Initialize growth data if not tracked yet
                    if (!cropGrowthTracker.ContainsKey(key))
                    {
                        cropGrowthTracker[key] = new CropGrowthData
                        {
                            cropTypeID = tile.CropTypeID,
                            currentStage = tile.CropStage,
                            totalAge = 0, // Total days since planting
                            worldX = tile.WorldX,
                            worldY = tile.WorldY
                        };
                    }

                    CropGrowthData growthData = cropGrowthTracker[key];
                    growthData.totalAge++; // Increment total age each day

                    // Check if crop should advance to next stage based on total age
                    PlantDataSO plantData = GetPlantData(tile.CropTypeID);
                    if (plantData != null && growthData.currentStage < plantData.GrowthStages.Count - 1)
                    {
                        // Check next stage to see if we should advance
                        int nextStageIndex = growthData.currentStage + 1;
                        GrowthStage nextStageData = plantData.GrowthStages[nextStageIndex];
                        
                        // Apply growth speed multiplier
                        int ageRequired = Mathf.RoundToInt(nextStageData.age / growthSpeedMultiplier);
                        
                        if (growthData.totalAge >= ageRequired)
                        {
                            // Advance to next stage
                            growthData.currentStage = nextStageIndex;
                            
                            // Update in WorldDataManager
                            worldDataManager.UpdateCropStage(
                                new Vector3(tile.WorldX, tile.WorldY, 0), 
                                (byte)growthData.currentStage
                            );
                            
                            chunkModified = true;
                            cropsGrown++;

                            if (showDebugLogs)
                            {
                                Debug.Log($"[CropManagerView] Crop {tile.CropTypeID} at ({tile.WorldX}, {tile.WorldY}) " +
                                         $"advanced to stage {growthData.currentStage} (age: {growthData.totalAge} days)");
                            }

                            // Update visual
                            UpdateCropVisual(tile.WorldX, tile.WorldY, plantData, growthData.currentStage);

                            // Check if fully grown (ready to harvest)
                            if (growthData.currentStage >= plantData.GrowthStages.Count - 1)
                            {
                                cropsHarvested++;
                                if (showDebugLogs)
                                {
                                    Debug.Log($"[CropManagerView] Crop {tile.CropTypeID} at ({tile.WorldX}, {tile.WorldY}) " +
                                             $"is ready to harvest! (Total age: {growthData.totalAge} days)");
                                }
                            }
                        }
                    }

                    cropGrowthTracker[key] = growthData;
                }

                // Refresh chunk visuals if modified
                if (chunkModified && chunkLoadingManager != null)
                {
                    Vector2Int chunkPos = chunkPair.Key;
                    chunkLoadingManager.RefreshChunkVisuals(chunkPos);
                }
            }
        }

        if (showDebugLogs && (cropsGrown > 0 || cropsHarvested > 0))
        {
            Debug.Log($"[CropManagerView] Growth complete: {cropsGrown} crops advanced, {cropsHarvested} ready to harvest");
        }
    }

    /// <summary>
    /// Register a newly planted crop for growth tracking
    /// </summary>
    public void RegisterPlantedCrop(int worldX, int worldY, ushort cropTypeID)
    {
        string key = GetCropKey(worldX, worldY);
        
        cropGrowthTracker[key] = new CropGrowthData
        {
            cropTypeID = cropTypeID,
            currentStage = 0,
            totalAge = 0, // Start from day 0
            worldX = worldX,
            worldY = worldY
        };

        // Create visual
        PlantDataSO plantData = GetPlantData(cropTypeID);
        if (plantData != null)
        {
            // If ChunkLoadingManager is available, refresh that chunk's visuals
            // Otherwise, create our own visual
            if (chunkLoadingManager != null)
            {
                Vector2Int chunkPos = worldDataManager.WorldToChunkCoords(new Vector3(worldX, worldY, 0));
                chunkLoadingManager.RefreshChunkVisuals(chunkPos);
            }
            else
            {
                CreateCropVisual(worldX, worldY, plantData, 0);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CropManagerView] Registered new crop {cropTypeID} at ({worldX}, {worldY})");
        }
    }

    /// <summary>
    /// Unregister a crop (when harvested or removed)
    /// </summary>
    public void UnregisterCrop(int worldX, int worldY)
    {
        string key = GetCropKey(worldX, worldY);
        
        cropGrowthTracker.Remove(key);
        
        // Remove visual or refresh chunk
        if (chunkLoadingManager != null)
        {
            Vector2Int chunkPos = worldDataManager.WorldToChunkCoords(new Vector3(worldX, worldY, 0));
            chunkLoadingManager.RefreshChunkVisuals(chunkPos);
        }
        else if (cropVisuals.ContainsKey(key))
        {
            if (cropVisuals[key] != null)
            {
                Destroy(cropVisuals[key]);
            }
            cropVisuals.Remove(key);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[CropManagerView] Unregistered crop at ({worldX}, {worldY})");
        }
    }

    /// <summary>
    /// Create visual representation of a crop
    /// </summary>
    private void CreateCropVisual(int worldX, int worldY, PlantDataSO plantData, int stage)
    {
        if (plantData == null || plantData.GrowthStages.Count == 0) return;

        string key = GetCropKey(worldX, worldY);
        
        // Remove old visual if exists
        if (cropVisuals.ContainsKey(key) && cropVisuals[key] != null)
        {
            Destroy(cropVisuals[key]);
        }

        // Create new visual
        GameObject cropVisual = new GameObject($"Crop_{plantData.PlantName}_{worldX}_{worldY}");
        cropVisual.transform.position = new Vector3(worldX + 0.5f, worldY + 0.5f, 0);
        cropVisual.transform.SetParent(cropVisualsParent);

        SpriteRenderer sr = cropVisual.AddComponent<SpriteRenderer>();
        sr.sprite = plantData.GrowthStages[stage].stageSprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 1;

        cropVisuals[key] = cropVisual;
    }

    /// <summary>
    /// Update visual representation of a crop
    /// </summary>
    private void UpdateCropVisual(int worldX, int worldY, PlantDataSO plantData, int stage)
    {
        // If ChunkLoadingManager is available, it will handle visuals via RefreshChunkVisuals
        // This method is only used as a fallback when ChunkLoadingManager is not present
        if (chunkLoadingManager != null)
        {
            // ChunkLoadingManager handles visuals, do nothing here
            return;
        }
        
        if (plantData == null || stage >= plantData.GrowthStages.Count) return;

        string key = GetCropKey(worldX, worldY);

        if (cropVisuals.ContainsKey(key) && cropVisuals[key] != null)
        {
            SpriteRenderer sr = cropVisuals[key].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = plantData.GrowthStages[stage].stageSprite;
            }
        }
        else
        {
            // Create visual if it doesn't exist
            CreateCropVisual(worldX, worldY, plantData, stage);
        }
    }

    /// <summary>
    /// Get plant data for a specific crop type ID
    /// </summary>
    private PlantDataSO GetPlantData(ushort cropTypeID)
    {
        if (plantDatabase == null || cropTypeID >= plantDatabase.Length)
        {
            Debug.LogWarning($"[CropManagerView] No plant data for crop type {cropTypeID}");
            return null;
        }
        return plantDatabase[cropTypeID];
    }

    /// <summary>
    /// Generate unique key for crop position
    /// </summary>
    private string GetCropKey(int worldX, int worldY)
    {
        return $"{worldX}_{worldY}";
    }

    /// <summary>
    /// Check if a crop is ready to harvest
    /// </summary>
    public bool IsCropReadyToHarvest(int worldX, int worldY)
    {
        string key = GetCropKey(worldX, worldY);
        
        if (!cropGrowthTracker.ContainsKey(key)) return false;

        CropGrowthData growthData = cropGrowthTracker[key];
        PlantDataSO plantData = GetPlantData(growthData.cropTypeID);
        
        if (plantData == null) return false;

        return growthData.currentStage >= plantData.GrowthStages.Count - 1;
    }

    /// <summary>
    /// Get growth progress of a crop (0-1)
    /// </summary>
    public float GetCropGrowthProgress(int worldX, int worldY)
    {
        string key = GetCropKey(worldX, worldY);
        
        if (!cropGrowthTracker.ContainsKey(key)) return 0f;

        CropGrowthData growthData = cropGrowthTracker[key];
        PlantDataSO plantData = GetPlantData(growthData.cropTypeID);
        
        if (plantData == null || plantData.GrowthStages.Count == 0) return 0f;

        return (float)growthData.currentStage / (plantData.GrowthStages.Count - 1);
    }

    /// <summary>
    /// Force grow a specific crop (for testing/debugging)
    /// </summary>
    public void ForceGrowCrop(int worldX, int worldY)
    {
        string key = GetCropKey(worldX, worldY);
        
        if (!cropGrowthTracker.ContainsKey(key))
        {
            Debug.LogWarning($"[CropManagerView] No crop at ({worldX}, {worldY}) to force grow");
            return;
        }

        CropGrowthData growthData = cropGrowthTracker[key];
        PlantDataSO plantData = GetPlantData(growthData.cropTypeID);
        
        if (plantData == null) return;

        if (growthData.currentStage < plantData.GrowthStages.Count - 1)
        {
            growthData.currentStage++;
            
            // Set age to the requirement for this stage
            if (growthData.currentStage < plantData.GrowthStages.Count)
            {
                growthData.totalAge = plantData.GrowthStages[growthData.currentStage].age;
            }
            
            cropGrowthTracker[key] = growthData;

            worldDataManager.UpdateCropStage(
                new Vector3(worldX, worldY, 0), 
                (byte)growthData.currentStage
            );

            UpdateCropVisual(worldX, worldY, plantData, growthData.currentStage);

            if (showDebugLogs)
            {
                Debug.Log($"[CropManagerView] Force grew crop at ({worldX}, {worldY}) to stage {growthData.currentStage} (age: {growthData.totalAge})");
            }
        }
    }

    /// <summary>
    /// Clear all growth data (useful when loading a save or starting new game)
    /// </summary>
    public void ClearAllGrowthData()
    {
        cropGrowthTracker.Clear();
        
        foreach (var visual in cropVisuals.Values)
        {
            if (visual != null) Destroy(visual);
        }
        cropVisuals.Clear();

        if (showDebugLogs)
        {
            Debug.Log("[CropManagerView] Cleared all growth data");
        }
    }
}

/// <summary>
/// Data structure to track individual crop growth
/// </summary>
[System.Serializable]
public struct CropGrowthData
{
    public ushort cropTypeID;
    public int currentStage;
    public int totalAge; // Total days since planting
    public int worldX;
    public int worldY;
}
