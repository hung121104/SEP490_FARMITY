using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using System;
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

    [Tooltip("Show loaded chunks with resources")]
    public bool visualizeResources = true;

    [Tooltip("Prefab used to render a crop. Assign a prefab with a SpriteRenderer (can be on a child object) so you can control the local offset in the editor.")]
    public GameObject cropVisualPrefab;
    
    // Plant data is sourced from PlantCatalogService at runtime — no Inspector array needed.
    
    [Header("Tilled Tile Settings")]
    [Tooltip("TileBase to use for tilled tiles")]
    public TileBase tilledTile;
    
    [Tooltip("Name of the tilemap to place tilled tiles on")]
    public string tilledTilemapName = "TilledOverlayTilemap";

    [Header("Watered Tile Settings")]
    [Tooltip("TileBase to use for watered tiles")]
    public TileBase wateredTile;

    [Tooltip("Name of the tilemap to place watered tiles on")]
    public string wateredTilemapName = "WateredOverlayTilemap";
    
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
    
    // Structure visuals tracked separately so they can be returned to the pool
    private Dictionary<Vector2Int, List<(string structureId, GameObject go)>> chunkStructureVisuals
        = new Dictionary<Vector2Int, List<(string, GameObject)>>();
    
    // Cached pool reference (avoids FindAnyObjectByType every tile)
    private StructurePool cachedStructurePool;
    
    // Track tilled tiles per chunk for cleanup: chunkPos -> list of tile positions
    private Dictionary<Vector2Int, List<Vector3Int>> chunkTilledTiles = new Dictionary<Vector2Int, List<Vector3Int>>();

    // Track watered tiles per chunk for cleanup: chunkPos -> list of tile positions
    private Dictionary<Vector2Int, List<Vector3Int>> chunkWateredTiles = new Dictionary<Vector2Int, List<Vector3Int>>();
    
    // Cache tilemap references
    private Dictionary<string, Tilemap> tilemapCache = new Dictionary<string, Tilemap>();
    
    private float nextUpdateTime;
    private Transform localPlayerTransform;
    private TimeManagerView timeManager;

    // ── Events for other systems (e.g. DroppedItemManager) ──────────────────

    /// <summary>Fired after a chunk has been loaded and its visuals spawned.</summary>
    public event Action<Vector2Int> OnChunkLoaded;

    /// <summary>Fired after a chunk has been unloaded and its visuals destroyed.</summary>
    public event Action<Vector2Int> OnChunkUnloaded;

    // ── Public Query ─────────────────────────────────────────────────────────
  
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
        // Wait for PlantCatalogService to finish loading sprites
        while (PlantCatalogService.Instance == null || !PlantCatalogService.Instance.IsReady)
        {
            if (showDebugLogs)
                Debug.Log("[ChunkLoading] Waiting for PlantCatalogService to be ready...");
            yield return new WaitForSeconds(0.5f);
        }

        // Wait for WorldDataBootstrapper to finish fetching and populating chunk data
        // (master client only — non-masters skip this automatically via IsReady staying false)
        float bootTimeout = 30f; // generous cap in case of slow network
        float bootElapsed = 0f;
        while (WorldDataBootstrapper.Instance != null && !WorldDataBootstrapper.Instance.IsReady)
        {
            if (showDebugLogs)
                Debug.Log("[ChunkLoading] Waiting for WorldDataBootstrapper to be ready...");
            yield return new WaitForSeconds(0.5f);
            bootElapsed += 0.5f;
            if (bootElapsed >= bootTimeout)
            {
                Debug.LogWarning("[ChunkLoading] Timed out waiting for WorldDataBootstrapper — proceeding without loaded chunk data.");
                break;
            }
        }

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
        
        // Initial load — chunk data in RAM is now fully populated from DB
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
            Debug.Log($"[ChunkLoading] Loaded chunk ({chunkPos.x}, {chunkPos.y}) - {chunk.GetCropCount()} crops, {chunk.GetResourceCount()} resources");
        
        // Spawn visuals for crops/resources in this chunk
        if (visualizeCrops || visualizeResources)
        {
            SpawnChunkVisuals(chunkPos, chunk);
        }

        // Notify subscribers (e.g. DroppedItemManager)
        OnChunkLoaded?.Invoke(chunkPos);
    }
    
    /// <summary>
    /// Unload a chunk and destroy visuals
    /// </summary>
    private void UnloadChunk(Vector2Int chunkPos)
    {
        if (!currentlyLoadedChunks.Contains(chunkPos))
            return;
        
        // Notify subscribers BEFORE removing (they may need to query state)
        OnChunkUnloaded?.Invoke(chunkPos);

        currentlyLoadedChunks.Remove(chunkPos);
        
        // Note: We don't actually clear the chunk data, just mark as unloaded
        // Data stays in memory (Strategy 1 from our design)
        
        if (showDebugLogs)
            Debug.Log($"[ChunkLoading] Unloaded chunk ({chunkPos.x}, {chunkPos.y})");
        
        // Destroy crop visuals
        if (chunkVisuals.ContainsKey(chunkPos))
        {
            foreach (GameObject visual in chunkVisuals[chunkPos])
            {
                if (visual != null)
                    Destroy(visual);
            }
            chunkVisuals.Remove(chunkPos);
        }
        
        // Return structure visuals to pool
        ReleaseChunkStructures(chunkPos);

        // Remove resource visuals for this chunk.
        ReleaseChunkResources(chunkPos);
        
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

        // Clear watered tiles from tilemap
        if (chunkWateredTiles.ContainsKey(chunkPos))
        {
            Tilemap wateredTilemap = FindTilemap(wateredTilemapName);
            if (wateredTilemap != null)
            {
                foreach (Vector3Int tilePos in chunkWateredTiles[chunkPos])
                    wateredTilemap.SetTile(tilePos, null);
            }
            chunkWateredTiles.Remove(chunkPos);
        }
    }
    
    /// <summary>
    /// Spawn visual GameObjects for all crops and tilled tiles in a chunk
    /// </summary>
    private void SpawnChunkVisuals(Vector2Int chunkPos, UnifiedChunkData chunk)
    {
        if (PlantCatalogService.Instance == null || !PlantCatalogService.Instance.IsReady)
        {
            if (showDebugLogs)
                Debug.LogWarning("[ChunkLoading] PlantCatalogService is not ready yet — skipping visual spawn for chunk.");
            return;
        }
        
        List<GameObject> visuals = new List<GameObject>();
        List<Vector3Int> tilledTilePositions = new List<Vector3Int>();
        List<Vector3Int> wateredTilePositions = new List<Vector3Int>();
        int resourceVisualsSpawned = 0;
        ResourceSpawnerManager resourceSpawner = null;

        if (visualizeResources)
        {
            resourceSpawner = ResourceSpawnerManager.Instance ?? FindAnyObjectByType<ResourceSpawnerManager>();
            if (resourceSpawner == null && showDebugLogs)
            {
                Debug.LogWarning("[ChunkLoading] ResourceSpawnerManager not found - skipping resource visuals for this chunk.");
            }
        }
        
        // Find the tilemap for tilled tiles
        Tilemap tilledTilemap = FindTilemap(tilledTilemapName);

        // Find the tilemap for watered tiles
        Tilemap wateredTilemap = FindTilemap(wateredTilemapName);
        
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

            // If tile is tilled and watered, place the watered overlay tile
            if (tile.IsTilled && tile.Crop.IsWatered)
            {
                if (wateredTile == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning("[ChunkLoading] WateredTile (TileBase) not assigned!");
                }
                else if (wateredTilemap == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] Tilemap '{wateredTilemapName}' not found!");
                }
                else
                {
                    Vector3 worldPos = new Vector3(tile.WorldX, tile.WorldY, 0);
                    Vector3Int tilePos = wateredTilemap.WorldToCell(worldPos);
                    wateredTilemap.SetTile(tilePos, wateredTile);
                    wateredTilePositions.Add(tilePos);
                }
            }
            
            // If tile has a crop, spawn the crop visual on top of the tilled tile
            if (tile.HasCrop)
            {
                // Get plant data for this crop type
                PlantData plantData = GetPlantData(tile.Crop.PlantId);
                if (plantData == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] No plant data found for plant id '{tile.Crop.PlantId}' at ({tile.WorldX}, {tile.WorldY})");
                    continue;
                }
                
                // Validate stage is within bounds (for normal plants)
                if (!plantData.isHybrid && tile.Crop.CropStage >= plantData.growthStages.Count)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] Invalid crop stage {tile.Crop.CropStage} for {plantData.plantName} at ({tile.WorldX}, {tile.WorldY})");
                    continue;
                }
                
                // Get sprite from catalog (handles hybrid delegation internally)
                Sprite stageSprite = PlantCatalogService.Instance?.GetStageSprite(tile.Crop.PlantId, tile.Crop.CropStage);

                if (stageSprite == null)
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[ChunkLoading] '{plantData.plantName}' stage {tile.Crop.CropStage} has a null sprite in PlantCatalogService.");
                    continue;
                }

                // Instantiate from prefab if assigned, otherwise fall back to a plain GameObject
                GameObject visual;
                if (cropVisualPrefab != null)
                {
                    visual = Instantiate(cropVisualPrefab, new Vector3(tile.WorldX, tile.WorldY, 0f), Quaternion.identity);
                }
                else
                {
                    visual = new GameObject();
                    visual.transform.position = new Vector3(tile.WorldX, tile.WorldY, 0f);
                }

                visual.name = $"Crop_{plantData.plantName}_{tile.WorldX}_{tile.WorldY}";

                // SpriteRenderer may be on the root or a child object (allowing offset control in prefab).
                // Skip any shadow child renderers created by SpriteShadowShader during Awake so
                // the stage sprite is always assigned to the real source renderer, not the shadow.
                SpriteRenderer sr = null;
                foreach (var candidate in visual.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (candidate.gameObject.name != "_SpriteShadowShader" &&
                        candidate.gameObject.name != "_SpriteShadow")
                    {
                        sr = candidate;
                        break;
                    }
                }
                if (sr == null)
                {
                    // No renderer in the prefab — add one to the root and apply defaults.
                    sr = visual.AddComponent<SpriteRenderer>();
                    sr.sortingLayerName = "WalkInfront";
                }
                // When a prefab is used its SpriteRenderer (and any SpriteShadowShader on it)
                // already carry the correct sorting layer and shadow settings — don't override them.

                sr.sprite = stageSprite;

                visuals.Add(visual);

            }

            // If tile has a structure, spawn the structure visual via pool
            if (tile.HasStructure)
            {
                // Skip spawning if structure is destroyed (HP <= 0)
                if (tile.Structure.CurrentHp <= 0)
                {
                    if (showDebugLogs)
                        Debug.Log($"[ChunkLoading] Skipping destroyed structure '{tile.Structure.StructureId}' at ({tile.WorldX},{tile.WorldY}) - HP is {tile.Structure.CurrentHp}");
                    continue;
                }
                
                if (cachedStructurePool == null)
                    cachedStructurePool = FindAnyObjectByType<StructurePool>();

                if (cachedStructurePool != null)
                {
                    string structId = tile.Structure.StructureId;
                    StructureData structData = cachedStructurePool.GetStructureData(structId);
                    if (structData != null)
                    {
                        Vector3Int structPosInt = new Vector3Int(tile.WorldX, tile.WorldY, 0);
                        GameObject structObj = cachedStructurePool.Get(structId, structPosInt);
                        
                        if (showDebugLogs)
                            Debug.Log($"[ChunkLoading] Spawned structure '{structId}' at ({tile.WorldX},{tile.WorldY}) from pool, obj={structObj?.GetInstanceID()}");
                        
                        Vector3 structPos = new Vector3(tile.WorldX + 0.5f, tile.WorldY + 0.5f, 0f);
                        structObj.transform.position = structPos;

                        SpriteRenderer sr = structObj.GetComponentInChildren<SpriteRenderer>(true)
                            ?? structObj.AddComponent<SpriteRenderer>();
                        Sprite itemSprite = ItemCatalogService.Instance?.GetCachedSprite(structId);
                        if (itemSprite != null)
                            sr.sprite = itemSprite;
                        sr.sortingLayerName = "WalkInfront";

                        structObj.SetActive(true);
                        foreach (var col in structObj.GetComponentsInChildren<Collider2D>())
                            col.enabled = true;

                        var worldStructure = structObj.GetComponentInChildren<IWorldStructure>(true);
                        if (worldStructure != null)
                            worldStructure.InitializeFromWorld(tile.WorldX, tile.WorldY, structData);

                        if (!chunkStructureVisuals.ContainsKey(chunkPos))
                            chunkStructureVisuals[chunkPos] = new List<(string, GameObject)>();
                        chunkStructureVisuals[chunkPos].Add((structId, structObj));
                        
                        // Store position for keyed release
                        structObj.name = $"Structure_{structId}_{tile.WorldX}_{tile.WorldY}";
                        
                        if (showDebugLogs)
                            Debug.Log($"[ChunkLoading] chunkStructureVisuals[{chunkPos}] now has {chunkStructureVisuals[chunkPos].Count} structures");
                    }
                    else if (showDebugLogs)
                    {
                        Debug.LogWarning($"[ChunkLoading] No structure data found for '{tile.Structure.StructureId}' at ({tile.WorldX}, {tile.WorldY})");
                    }
                }
            }

            if (visualizeResources && tile.HasResource && resourceSpawner != null && !string.IsNullOrEmpty(tile.Resource.ResourceId))
            {
                int tileIndex = WorldTileToTileIndex(chunk.ChunkX, chunk.ChunkY, tile.WorldX, tile.WorldY);
                if (tileIndex >= 0)
                {
                    resourceSpawner.SpawnResourceVisualLocally(
                        chunk.ChunkX,
                        chunk.ChunkY,
                        tileIndex,
                        tile.Resource.ResourceId);
                    resourceVisualsSpawned++;
                }
            }
        }
        
        chunkVisuals[chunkPos] = visuals;
        chunkTilledTiles[chunkPos] = tilledTilePositions;
        chunkWateredTiles[chunkPos] = wateredTilePositions;
        
        if (showDebugLogs && (visuals.Count > 0 || tilledTilePositions.Count > 0 || resourceVisualsSpawned > 0))
            Debug.Log($"[ChunkLoading] Spawned {visuals.Count} crop visuals + {resourceVisualsSpawned} resource visuals + {tilledTilePositions.Count} tilled tiles for chunk ({chunkPos.x}, {chunkPos.y})");
    }

    private int WorldTileToTileIndex(int chunkX, int chunkY, int worldX, int worldY)
    {
        int chunkSize = Mathf.Max(1, WorldDataManager.Instance != null
            ? WorldDataManager.Instance.chunkSizeTiles
            : 30);

        int localX = worldX - (chunkX * chunkSize);
        int localY = worldY - (chunkY * chunkSize);

        if (localX < 0 || localY < 0 || localX >= chunkSize || localY >= chunkSize)
            return -1;

        return (localY * chunkSize) + localX;
    }

    private bool TryGetChunkData(Vector2Int chunkPos, out UnifiedChunkData chunk)
    {
        chunk = null;

        if (WorldDataManager.Instance == null)
            return false;

        foreach (var config in WorldDataManager.Instance.sectionConfigs)
        {
            if (!config.IsActive) continue;
            if (!config.ContainsChunk(chunkPos)) continue;

            chunk = WorldDataManager.Instance.GetChunk(config.SectionId, chunkPos);
            return chunk != null;
        }

        return false;
    }

    private void ReleaseChunkResources(Vector2Int chunkPos)
    {
        if (!visualizeResources)
            return;

        ResourceSpawnerManager resourceSpawner = ResourceSpawnerManager.Instance ?? FindAnyObjectByType<ResourceSpawnerManager>();
        if (resourceSpawner == null)
            return;

        if (!TryGetChunkData(chunkPos, out UnifiedChunkData chunk) || chunk == null)
            return;

        int removed = 0;
        foreach (var tile in chunk.GetAllTiles())
        {
            if (!tile.HasResource)
                continue;

            int tileIndex = WorldTileToTileIndex(chunk.ChunkX, chunk.ChunkY, tile.WorldX, tile.WorldY);
            if (tileIndex < 0)
                continue;

            resourceSpawner.RemoveResourceVisual(chunk.ChunkX, chunk.ChunkY, tileIndex);
            removed++;
        }

        if (showDebugLogs && removed > 0)
            Debug.Log($"[ChunkLoading] Removed {removed} resource visuals for chunk ({chunkPos.x}, {chunkPos.y})");
    }
    
    /// <summary>
    /// Get plant data by PlantId string
    /// </summary>
    private PlantData GetPlantData(string plantId)
    {
        PlantData plant = PlantCatalogService.Instance?.GetPlantData(plantId);
        if (plant == null && showDebugLogs)
            Debug.LogWarning($"[ChunkLoading] PlantId '{plantId}' not found in PlantCatalogService.");
        return plant;
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
    /// Close the room when the master client leaves instead of transferring mastership.
    /// </summary>
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log("[ChunkLoading] Master client left — closing room.");
        PhotonNetwork.LeaveRoom();
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
        
        // Destroy old crop visuals
        if (chunkVisuals.ContainsKey(chunkPos))
        {
            foreach (GameObject visual in chunkVisuals[chunkPos])
            {
                if (visual != null)
                    Destroy(visual);
            }
            chunkVisuals.Remove(chunkPos);
        }

        // Clear tilled tile cells from the Tilemap before re-spawning.
        // Without this, untilled tiles remain painted on the Tilemap because
        // SpawnChunkVisuals only adds currently-tilled positions — it never
        // removes ones that were tilled on the previous draw call.
        if (chunkTilledTiles.ContainsKey(chunkPos))
        {
            Tilemap tilledTilemap = FindTilemap(tilledTilemapName);
            if (tilledTilemap != null)
            {
                foreach (Vector3Int tilePos in chunkTilledTiles[chunkPos])
                    tilledTilemap.SetTile(tilePos, null);
            }
            chunkTilledTiles.Remove(chunkPos);
        }

        // Clear watered tile cells from the Tilemap before re-spawning.
        if (chunkWateredTiles.ContainsKey(chunkPos))
        {
            Tilemap wateredTilemap = FindTilemap(wateredTilemapName);
            if (wateredTilemap != null)
            {
                foreach (Vector3Int tilePos in chunkWateredTiles[chunkPos])
                    wateredTilemap.SetTile(tilePos, null);
            }
            chunkWateredTiles.Remove(chunkPos);
        }
        
        // Return old structure visuals to pool
        ReleaseChunkStructures(chunkPos);

        // Remove resource visuals for this chunk before re-spawning.
        ReleaseChunkResources(chunkPos);
        
        // Find chunk data and re-spawn
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
    /// Returns all structure GameObjects for the given chunk back to the pool.
    /// Only processes active objects to avoid double-releasing.
    /// </summary>
    private void ReleaseChunkStructures(Vector2Int chunkPos)
    {
        if (!chunkStructureVisuals.ContainsKey(chunkPos))
            return;

        if (cachedStructurePool == null)
            cachedStructurePool = FindAnyObjectByType<StructurePool>();

        foreach (var (structureId, go) in chunkStructureVisuals[chunkPos])
        {
            if (go == null) continue;
            // Only release if object is still active - prevents double-release if already pooled
            if (!go.activeInHierarchy) continue;

            // Parse position from name for keyed release
            Vector3Int? position = null;
            var parts = go.name.Split('_');
            if (parts.Length >= 4 && int.TryParse(parts[parts.Length - 2], out int px) && int.TryParse(parts[parts.Length - 1], out int py))
            {
                position = new Vector3Int(px, py, 0);
            }

            cachedStructurePool.Release(structureId, go, position);
        }
        chunkStructureVisuals.Remove(chunkPos);
    }

    /// <summary>
    /// Returns the visual GameObject for a structure at a specific world position, if loaded.
    /// Accounts for the +0.5f offset used when spawning structures.
    /// </summary>
    public GameObject GetStructureVisualAt(Vector3Int worldPos)
    {
        Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(new Vector3(worldPos.x, worldPos.y, 0f));
        if (chunkStructureVisuals.TryGetValue(chunkPos, out var list))
        {
            foreach (var tuple in list)
            {
                if (tuple.go == null) continue;
                
                // Structure positions have +0.5f offset, so check both integer and offset positions
                float visualX = tuple.go.transform.position.x;
                float visualY = tuple.go.transform.position.y;
                
                // Check if matches integer position (worldPos)
                bool matchesIntegerPos = Mathf.Approximately(visualX, worldPos.x) && 
                                         Mathf.Approximately(visualY, worldPos.y);
                
                // Check if matches offset position (worldPos + 0.5f)
                bool matchesOffsetPos = Mathf.Approximately(visualX, worldPos.x + 0.5f) && 
                                      Mathf.Approximately(visualY, worldPos.y + 0.5f);
                
                if (matchesIntegerPos || matchesOffsetPos)
                {
                    return tuple.go;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Removes the watered overlay tile at a single world position.
    /// Called directly by CropGrowthService on water decay — avoids a full chunk re-render.
    /// </summary>
    public void ClearWateredTileAt(Vector3 worldPos)
    {
        Tilemap wateredTilemap = FindTilemap(wateredTilemapName);
        if (wateredTilemap == null) return;

        Vector3Int cellPos = wateredTilemap.WorldToCell(worldPos);
        wateredTilemap.SetTile(cellPos, null);

        // Remove from tracking so the next RefreshChunkVisuals doesn't try to null it again
        Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
        if (chunkWateredTiles.TryGetValue(chunkPos, out var list))
            list.Remove(cellPos);
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
    [ContextMenu("Simulate Day Change")]
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
