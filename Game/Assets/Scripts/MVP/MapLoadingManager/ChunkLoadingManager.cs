using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Manages dynamic chunk loading/unloading based on player positions
/// Loads 3x3 chunk area around each player
/// Syncs loaded chunks across multiplayer
/// </summary>
public class ChunkLoadingManager : MonoBehaviourPunCallbacks
{
    [Header("Loading Settings")]
    [Tooltip("Load radius in chunks (1 = 3x3, 2 = 5x5, etc.)")]
    public int loadRadius = 1; // 1 = 3x3 chunks around player
    
    [Tooltip("Check player position every X seconds")]
    public float updateInterval = 1f;
    
    [Tooltip("Delay before unloading chunks after player leaves area")]
    public float unloadDelay = 5f;
    
    [Header("Visual Settings")]
    [Tooltip("Show loaded chunks with crops")]
    public bool visualizeCrops = true;
    
    [Header("Plant Data")]
    [Tooltip("Array of all plant data ScriptableObjects indexed by CropTypeID")]
    public PlantDataSO[] plantDatabase;
    
    [Header("Tilled Tile Settings")]
    [Tooltip("TileBase to use for tilled tiles")]
    public TileBase tilledTile;
    
    [Tooltip("Name of the tilemap to place tilled tiles on")]
    public string tilledTilemapName = "TillableTilemap";
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showLoadedChunksGizmos = true;
    
    [Header("Daily Reload")]
    [Tooltip("Enable automatic chunk reload each day")]
    public bool enableDailyReload = true;
    
    // Track all players and their loaded chunks
    private Dictionary<int, Vector2Int> playerChunkPositions = new Dictionary<int, Vector2Int>();
    private HashSet<Vector2Int> currentlyLoadedChunks = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, float> chunksToUnload = new Dictionary<Vector2Int, float>();
    
    // Visual crop objects: chunkPos -> list of GameObjects
    private Dictionary<Vector2Int, List<GameObject>> chunkVisuals = new Dictionary<Vector2Int, List<GameObject>>();
    
    // Track tilled tiles per chunk for cleanup: chunkPos -> list of tile positions
    private Dictionary<Vector2Int, List<Vector3Int>> chunkTilledTiles = new Dictionary<Vector2Int, List<Vector3Int>>();
    
    // Cache tilemap references
    private Dictionary<string, Tilemap> tilemapCache = new Dictionary<string, Tilemap>();
    
    private float nextUpdateTime;
    private Transform localPlayerTransform;
    private TimeManagerView timeManager;
    
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[ChunkLoading] Not connected to Photon network");
            return;
        }
        
        // Find TimeManagerView and subscribe to day change event
        if (enableDailyReload)
        {
            timeManager = FindAnyObjectByType<TimeManagerView>();
            if (timeManager != null)
            {
                timeManager.OnDayChanged += OnDayChanged;
                if (showDebugLogs)
                    Debug.Log("[ChunkLoading] Subscribed to OnDayChanged event");
            }
            else
            {
                Debug.LogWarning("[ChunkLoading] TimeManagerView not found in scene!");
            }
        }
        
        // Find local player
        StartCoroutine(FindLocalPlayer());
        
        nextUpdateTime = Time.time + updateInterval;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (timeManager != null)
        {
            timeManager.OnDayChanged -= OnDayChanged;
        }
    }
    
    private IEnumerator FindLocalPlayer()
    {
        // Wait for local player to spawn
        while (localPlayerTransform == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
            foreach (GameObject player in players)
            {
                PhotonView pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    localPlayerTransform = player.transform;
                    if (showDebugLogs)
                        Debug.Log($"[ChunkLoading] Found local player: {player.name}");
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
        
        // Initial load
        UpdatePlayerChunkPosition();
    }
    
    private void Update()
    {
        if (localPlayerTransform == null || !WorldDataManager.Instance.IsInitialized)
            return;
        
        // Periodic position check
        if (Time.time >= nextUpdateTime)
        {
            UpdatePlayerChunkPosition();
            nextUpdateTime = Time.time + updateInterval;
        }
        
        // Process chunk unloading
        if (chunksToUnload.Count > 0)
        {
            List<Vector2Int> toUnload = new List<Vector2Int>();
            foreach (var kvp in chunksToUnload)
            {
                if (Time.time >= kvp.Value)
                {
                    toUnload.Add(kvp.Key);
                }
            }
            
            foreach (var chunkPos in toUnload)
            {
                UnloadChunk(chunkPos);
                chunksToUnload.Remove(chunkPos);
            }
        }
    }
    
    /// <summary>
    /// Update local player's chunk position
    /// </summary>
    private void UpdatePlayerChunkPosition()
    {
        if (localPlayerTransform == null) return;
        
        Vector3 worldPos = localPlayerTransform.position;
        Vector2Int currentChunk = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
        
        // Check if player moved to a different chunk
        int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
        if (!playerChunkPositions.ContainsKey(localActorNumber) || 
            playerChunkPositions[localActorNumber] != currentChunk)
        {
            playerChunkPositions[localActorNumber] = currentChunk;
            
            if (showDebugLogs)
                Debug.Log($"[ChunkLoading] Player moved to chunk ({currentChunk.x}, {currentChunk.y})");
            
            // Update loaded chunks
            UpdateLoadedChunks(currentChunk);
            
            // Notify other players
            BroadcastPlayerChunkPosition(currentChunk);
        }
    }
    
    /// <summary>
    /// Load chunks in radius around LOCAL player only
    /// </summary>
    private void UpdateLoadedChunks(Vector2Int centerChunk)
    {
        HashSet<Vector2Int> chunksToLoad = new HashSet<Vector2Int>();
        
        // CHANGED: Only load chunks around LOCAL player, not all players
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int y = -loadRadius; y <= loadRadius; y++)
            {
                Vector2Int chunkPos = new Vector2Int(centerChunk.x + x, centerChunk.y + y);
                chunksToLoad.Add(chunkPos);
            }
        }
        
        // Load new chunks
        foreach (var chunkPos in chunksToLoad)
        {
            if (!currentlyLoadedChunks.Contains(chunkPos))
            {
                LoadChunk(chunkPos);
            }
            
            // Cancel unload if chunk is needed again
            if (chunksToUnload.ContainsKey(chunkPos))
            {
                chunksToUnload.Remove(chunkPos);
            }
        }
        
        // Mark chunks for unloading if no player needs them
        foreach (var loadedChunk in new List<Vector2Int>(currentlyLoadedChunks))
        {
            if (!chunksToLoad.Contains(loadedChunk) && !chunksToUnload.ContainsKey(loadedChunk))
            {
                chunksToUnload[loadedChunk] = Time.time + unloadDelay;
                
                if (showDebugLogs)
                    Debug.Log($"[ChunkLoading] Marking chunk ({loadedChunk.x}, {loadedChunk.y}) for unload");
            }
        }
    }
    
    /// <summary>
    /// Load a chunk and spawn visuals
    /// </summary>
    private void LoadChunk(Vector2Int chunkPos)
    {
        // Check if chunk exists in any section
        int sectionId = -1;
        UnifiedChunkData chunk = null;
        
        foreach (var config in WorldDataManager.Instance.sectionConfigs)
        {
            if (!config.IsActive) continue;
            
            if (config.ContainsChunk(chunkPos))
            {
                sectionId = config.SectionId;
                chunk = WorldDataManager.Instance.GetChunk(sectionId, chunkPos);
                break;
            }
        }
        
        if (chunk == null)
        {
            // Chunk not in any active section
            return;
        }
        
        currentlyLoadedChunks.Add(chunkPos);
        chunk.IsLoaded = true;
        
        if (showDebugLogs)
            Debug.Log($"[ChunkLoading] Loaded chunk ({chunkPos.x}, {chunkPos.y}) - {chunk.GetCropCount()} crops");
        
        // Spawn visuals for crops in this chunk
        if (visualizeCrops)
        {
            SpawnChunkVisuals(chunkPos, chunk);
        }
    }
    
    /// <summary>
    /// Unload a chunk and destroy visuals
    /// </summary>
    private void UnloadChunk(Vector2Int chunkPos)
    {
        if (!currentlyLoadedChunks.Contains(chunkPos))
            return;
        
        currentlyLoadedChunks.Remove(chunkPos);
        
        // Note: We don't actually clear the chunk data, just mark as unloaded
        // Data stays in memory (Strategy 1 from our design)
        
        if (showDebugLogs)
            Debug.Log($"[ChunkLoading] Unloaded chunk ({chunkPos.x}, {chunkPos.y})");
        
        // Destroy visuals
        if (chunkVisuals.ContainsKey(chunkPos))
        {
            foreach (GameObject visual in chunkVisuals[chunkPos])
            {
                if (visual != null)
                    Destroy(visual);
            }
            chunkVisuals.Remove(chunkPos);
        }
        
        // Clear tilled tiles from tilemap
        if (chunkTilledTiles.ContainsKey(chunkPos))
        {
            Tilemap tilledTilemap = FindTilemap(tilledTilemapName);
            if (tilledTilemap != null)
            {
                foreach (Vector3Int tilePos in chunkTilledTiles[chunkPos])
                {
                    tilledTilemap.SetTile(tilePos, null);
                }
            }
            chunkTilledTiles.Remove(chunkPos);
        }
    }
    
    /// <summary>
    /// Spawn visual GameObjects for all crops and tilled tiles in a chunk
    /// </summary>
    private void SpawnChunkVisuals(Vector2Int chunkPos, UnifiedChunkData chunk)
    {
        if (plantDatabase == null || plantDatabase.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("[ChunkLoading] PlantDatabase not assigned or empty!");
            return;
        }
        
        List<GameObject> visuals = new List<GameObject>();
        List<Vector3Int> tilledTilePositions = new List<Vector3Int>();
        
        // Find the tilemap for tilled tiles
        Tilemap tilledTilemap = FindTilemap(tilledTilemapName);
        
        // Get all tiles (both tilled and with crops)
        foreach (var tile in chunk.GetAllTiles())
        {
            // If tile is tilled, place it on the tilemap (regardless of whether it has a crop)
            if (tile.IsTilled)
            {
                if (tilledTile == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning("[ChunkLoading] TilledTile (TileBase) not assigned!");
                    continue;
                }
                
                if (tilledTilemap == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] Tilemap '{tilledTilemapName}' not found!");
                    continue;
                }
                
                // Set tilled tile on tilemap
                Vector3 worldPos = new Vector3(tile.WorldX, tile.WorldY, 0);
                Vector3Int tilePos = tilledTilemap.WorldToCell(worldPos);
                tilledTilemap.SetTile(tilePos, tilledTile);
                tilledTilePositions.Add(tilePos);
            }
            
            // If tile has a crop, spawn the crop visual on top of the tilled tile
            if (tile.HasCrop)
            {
                // Get plant data for this crop type
                PlantDataSO plantData = GetPlantData(tile.Crop.PlantId);
                if (plantData == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] No plant data found for plant id '{tile.Crop.PlantId}' at ({tile.WorldX}, {tile.WorldY})");
                    continue;
                }
                
                // Validate stage is within bounds (for normal plants)
                if (plantData is not HybridPlantDataSO && tile.Crop.CropStage >= plantData.GrowthStages.Count)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] Invalid crop stage {tile.Crop.CropStage} for {plantData.PlantName} at ({tile.WorldX}, {tile.WorldY})");
                    continue;
                }
                
                // Create crop visual GameObject
                GameObject visual = new GameObject($"Crop_{plantData.PlantName}_{tile.WorldX}_{tile.WorldY}");
                visual.transform.position = new Vector3(tile.WorldX, tile.WorldY+0.062f, 0);
                
                // Add sprite renderer with correct stage sprite
                Sprite stageSprite = null;
                if (plantData is HybridPlantDataSO hybridData)
                {
                    stageSprite = hybridData.GetHybridStageSprite(tile.Crop.CropStage);
                }
                else if (tile.Crop.CropStage < plantData.GrowthStages.Count)
                {
                    stageSprite = plantData.GrowthStages[tile.Crop.CropStage].stageSprite;
                }

                if (stageSprite == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] '{plantData.PlantName}' stage {tile.Crop.CropStage} has a null stageSprite — " +
                                         "assign a sprite in PlantDataSO.GrowthStages (or the Hybrid receiver).");
                    Destroy(visual);
                    continue;
                }

                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = stageSprite;
                sr.sortingLayerName = "WalkInfront";
                sr.sortingOrder = 1;
                
                visuals.Add(visual);

            }
        }
        
        chunkVisuals[chunkPos] = visuals;
        chunkTilledTiles[chunkPos] = tilledTilePositions;
        
        if (showDebugLogs && (visuals.Count > 0 || tilledTilePositions.Count > 0))
            Debug.Log($"[ChunkLoading] Spawned {visuals.Count} crop visuals + {tilledTilePositions.Count} tilled tiles for chunk ({chunkPos.x}, {chunkPos.y})");
    }
    
    /// <summary>
    /// Get plant data by PlantId string
    /// </summary>
    private PlantDataSO GetPlantData(string plantId)
    {
        if (plantDatabase == null || string.IsNullOrEmpty(plantId)) return null;
        foreach (var plant in plantDatabase)
        {
            if (plant != null && plant.PlantId == plantId) return plant;
        }
        // Plant not found — log what IS registered so the user can spot the mismatch
        if (showDebugLogs)
        {
            var registered = new System.Text.StringBuilder();
            if (plantDatabase != null)
                foreach (var p in plantDatabase)
                    if (p != null) registered.Append($"'{p.PlantId}' ");
            Debug.LogWarning($"[ChunkLoading] PlantId '{plantId}' not found in plantDatabase. " +
                             $"Registered ids: [{registered}]\n" +
                             "→ Fix: add the PlantDataSO to ChunkLoadingManager.plantDatabase in the Inspector.");
        }
        return null;
    }
    
    /// <summary>
    /// Find a tilemap by name, with caching
    /// </summary>
    private Tilemap FindTilemap(string tilemapName)
    {
        // Check cache first
        if (tilemapCache.TryGetValue(tilemapName, out Tilemap cached) && cached != null)
        {
            return cached;
        }
        
        // Find all tilemaps
        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap.gameObject.name == tilemapName)
            {
                tilemapCache[tilemapName] = tilemap;
                return tilemap;
            }
        }
        
        if (showDebugLogs)
            Debug.LogWarning($"[ChunkLoading] Tilemap '{tilemapName}' not found in scene!");
        
        return null;
    }
    
    /// <summary>
    /// Broadcast player's chunk position to other clients
    /// </summary>
    private void BroadcastPlayerChunkPosition(Vector2Int chunkPos)
    {
        // You can delete this entire method if you want, or keep it empty for future use
        // Other players no longer need to know your chunk position
    }
    
    /// <summary>
    /// Handle player leaving room
    /// </summary>
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (playerChunkPositions.ContainsKey(otherPlayer.ActorNumber))
        {
            playerChunkPositions.Remove(otherPlayer.ActorNumber);
            
            if (showDebugLogs)
                Debug.Log($"[ChunkLoading] Player {otherPlayer.NickName} left, updating loaded chunks");
            
            // Update loaded chunks since a player left
            if (localPlayerTransform != null)
            {
                Vector2Int localChunk = WorldDataManager.Instance.WorldToChunkCoords(localPlayerTransform.position);
                UpdateLoadedChunks(localChunk);
            }
        }
    }
    
    /// <summary>
    /// Refresh visuals for a specific chunk (call when crop planted/removed)
    /// </summary>
    public void RefreshChunkVisuals(Vector2Int chunkPos)
    {
        if (!currentlyLoadedChunks.Contains(chunkPos))
            return;
        
        // Destroy old visuals
        if (chunkVisuals.ContainsKey(chunkPos))
        {
            foreach (GameObject visual in chunkVisuals[chunkPos])
            {
                if (visual != null)
                    Destroy(visual);
            }
            chunkVisuals.Remove(chunkPos);
        }
        
        // Find chunk data
        foreach (var config in WorldDataManager.Instance.sectionConfigs)
        {
            if (!config.IsActive) continue;
            
            if (config.ContainsChunk(chunkPos))
            {
                UnifiedChunkData chunk = WorldDataManager.Instance.GetChunk(config.SectionId, chunkPos);
                if (chunk != null)
                {
                    SpawnChunkVisuals(chunkPos, chunk);
                }
                break;
            }
        }
    }

    /// <summary>
    /// Ensure a chunk is loaded and visuals are spawned. Safe to call from other managers.
    /// </summary>
    public void EnsureChunkLoaded(Vector2Int chunkPos)
    {
        // If already loaded, refresh visuals to apply any new data
        if (currentlyLoadedChunks.Contains(chunkPos))
        {
            RefreshChunkVisuals(chunkPos);
            return;
        }

        // Try to find chunk data and load it
        foreach (var config in WorldDataManager.Instance.sectionConfigs)
        {
            if (!config.IsActive) continue;

            if (config.ContainsChunk(chunkPos))
            {
                UnifiedChunkData chunk = WorldDataManager.Instance.GetChunk(config.SectionId, chunkPos);
                if (chunk != null)
                {
                    LoadChunk(chunkPos);
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// Get list of currently loaded chunks
    /// </summary>
    public List<Vector2Int> GetLoadedChunks()
    {
        return new List<Vector2Int>(currentlyLoadedChunks);
    }
    
    /// <summary>
    /// Check if a chunk is currently loaded
    /// </summary>
    public bool IsChunkLoaded(Vector2Int chunkPos)
    {
        return currentlyLoadedChunks.Contains(chunkPos);
    }
    
    /// <summary>
    /// Called when a new day begins in the game
    /// </summary>
    private void OnDayChanged()
    {
        if (!enableDailyReload) return;
        
        if (showDebugLogs)
            Debug.Log("[ChunkLoading] New day detected! Reloading all chunks");
        
        ReloadAllChunks();
    }
    
    /// <summary>
    /// Reload all currently loaded chunks
    /// </summary>
    private void ReloadAllChunks()
    {
        if (showDebugLogs)
            Debug.Log($"[ChunkLoading] Reloading {currentlyLoadedChunks.Count} chunks");
        
        // Store current loaded chunks
        List<Vector2Int> chunksToReload = new List<Vector2Int>(currentlyLoadedChunks);
        
        // Unload all chunks
        foreach (var chunkPos in chunksToReload)
        {
            UnloadChunk(chunkPos);
        }
        
        // Clear unload queue
        chunksToUnload.Clear();
        
        // Reload chunks around local player
        if (localPlayerTransform != null)
        {
            Vector2Int currentChunk = WorldDataManager.Instance.WorldToChunkCoords(localPlayerTransform.position);
            UpdateLoadedChunks(currentChunk);
        }
    }
    
    /// <summary>
    /// Manually trigger a chunk reload (useful for testing or forced refresh)
    /// </summary>
    public void ForceReloadAllChunks()
    {
        if (showDebugLogs)
            Debug.Log("[ChunkLoading] Force reloading all chunks");
        
        ReloadAllChunks();
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showLoadedChunksGizmos) return;
        
        WorldDataManager manager = WorldDataManager.Instance;
        if (manager == null) return;
        
        // Draw loaded chunks in green
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        foreach (var chunkPos in currentlyLoadedChunks)
        {
            Vector3 worldPos = manager.ChunkToWorldPosition(chunkPos);
            Vector3 size = new Vector3(manager.chunkSizeTiles, manager.chunkSizeTiles, 1);
            Gizmos.DrawWireCube(worldPos + size / 2, size);
        }
        
        // Draw chunks pending unload in yellow
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        foreach (var chunkPos in chunksToUnload.Keys)
        {
            Vector3 worldPos = manager.ChunkToWorldPosition(chunkPos);
            Vector3 size = new Vector3(manager.chunkSizeTiles, manager.chunkSizeTiles, 1);
            Gizmos.DrawWireCube(worldPos + size / 2, size);
        }
        
        // Draw player positions
        Gizmos.color = Color.cyan;
        foreach (var playerChunk in playerChunkPositions.Values)
        {
            Vector3 worldPos = manager.ChunkToWorldPosition(playerChunk);
            Vector3 centerPos = worldPos + new Vector3(manager.chunkSizeTiles / 2f, manager.chunkSizeTiles / 2f, 0);
            Gizmos.DrawSphere(centerPos, 2f);
        }
    }
}
