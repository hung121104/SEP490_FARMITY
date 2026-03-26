using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

/// <summary>
/// One entry in the spawn configuration — maps a resource type string to the tilemap(s)
/// it is allowed to spawn on and the maximum number of that type that may exist in the world.
/// </summary>
[System.Serializable]
public class ResourceTypeMapping
{
    [Tooltip("Must match ResourceConfigData.resourceType from the server catalog (case-insensitive). E.g. 'tree', 'rock', 'ore'.")]
    public string resourceType;

    [Tooltip("Tilemaps this resource type is allowed to spawn on. A tile valid on ANY of these passes.")]
    public List<Tilemap> allowedTilemaps = new List<Tilemap>();

    [Tooltip("Maximum total count of this resource type across the entire world. 0 = unlimited.")]
    public int maxOnMap = 200;
}

/// <summary>
/// Host-authoritative resource spawner.
/// Spawns resource state in RAM and broadcasts visual-only prefab instantiation via RPC.
/// </summary>
public class ResourceSpawnerManager : MonoBehaviourPun, IInRoomCallbacks
{
    public static ResourceSpawnerManager Instance { get; private set; }

    [Header("Spawn Config")]
    [Tooltip("Maps each resource type to its allowed tilemap(s) and global world cap.")]
    public List<ResourceTypeMapping> resourceTypeMappings = new List<ResourceTypeMapping>();

    [Header("Spawn Rules")]
    public int maxResourcesPerChunk = 40;
    public int dailySpawnRate = 5;

    [Header("Harvest Settings")]
    [Min(0.1f)]
    [Tooltip("Max distance from local player to target tile when harvesting resources.")]
    [SerializeField] private float interactionRange = 2f;

    [Header("Noise Spawn System")]
    [Tooltip("Size/Frequency of the noise map. Lower is larger clusters.")]
    public float noiseScale = 0.1f;
    [Tooltip("Minimum noise value (0.0 to 1.0) required to spawn a resource.")]
    public float noiseThreshold = 0.5f;
    private float _dailyNoiseOffsetX;
    private float _dailyNoiseOffsetY;

    [Header("Prefabs based on Resource Type")]
    public GameObject treePrefab;
    public GameObject rockPrefab;
    public GameObject orePrefab;

    private readonly Dictionary<string, GameObject> _spawnedVisuals =
        new Dictionary<string, GameObject>();

    private readonly Dictionary<string, Sprite> _spriteCache =
        new Dictionary<string, Sprite>();

    private readonly Dictionary<string, Vector3> _baseVisualScales =
        new Dictionary<string, Vector3>();

    private readonly Dictionary<string, Coroutine> _activeHitFlashCoroutines =
        new Dictionary<string, Coroutine>();

    // Per-type world-wide resource counts (keyed on resourceType e.g. "tree").
    // Tracked on MasterClient only; rebuilt from world data after bootstrap completes.
    private readonly Dictionary<string, int> _worldResourceCounts = new Dictionary<string, int>();

    // Maps visual key → resourceType so counts can be decremented when a resource is removed.
    private readonly Dictionary<string, string> _visualKeyToResourceType = new Dictionary<string, string>();

    // Fast lookup built from resourceTypeMappings: lowercase resourceType → ResourceTypeMapping.
    private Dictionary<string, ResourceTypeMapping> _spawnMappingLookup;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private TimeManagerView _timeManager;
    private Coroutine _bindTimeManagerRoutine;

    private struct TileScanStats
    {
        public int TotalTilesChecked;
        public int InvalidSpawnMask;
        public int BlockedByTilled;
        public int BlockedByCrop;
        public int BlockedByStructure;
        public int BlockedByResource;
    }

    private ResourceHarvestingService _resourceHarvestingService;
    private ChunkLoadingManager _chunkLoadingManager;

    private Dictionary<string, ResourceTypeMapping> BuildMappingLookup()
    {
        var dict = new Dictionary<string, ResourceTypeMapping>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in resourceTypeMappings)
        {
            if (string.IsNullOrEmpty(mapping.resourceType)) continue;
            dict[mapping.resourceType.ToLower()] = mapping;
        }
        return dict;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        _spawnMappingLookup = BuildMappingLookup();

        _chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        _resourceHarvestingService = new ResourceHarvestingService(
            WorldDataManager.Instance,
            FindAnyObjectByType<ChunkDataSyncManager>(),
            FindAnyObjectByType<InventoryGameView>(),
            interactionRange
        );
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);

        TryBindTimeManager();
        if (_timeManager == null)
        {
            _bindTimeManagerRoutine = StartCoroutine(BindTimeManagerWhenReady());
        }

        ChunkDataSyncManager.OnResourceHpUpdated += HandleResourceHpUpdated;
        ChunkDataSyncManager.OnResourceRemoved   += HandleResourceRemoved;
        ChunkDataSyncManager.OnResourceSpawned   += HandleResourceSpawned;
        WorldDataBootstrapper.OnWorldDataReady   += HandleWorldDataReady;
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);

        if (_bindTimeManagerRoutine != null)
        {
            StopCoroutine(_bindTimeManagerRoutine);
            _bindTimeManagerRoutine = null;
        }

        UnbindTimeManager();

        ChunkDataSyncManager.OnResourceHpUpdated -= HandleResourceHpUpdated;
        ChunkDataSyncManager.OnResourceRemoved   -= HandleResourceRemoved;
        ChunkDataSyncManager.OnResourceSpawned   -= HandleResourceSpawned;
        WorldDataBootstrapper.OnWorldDataReady   -= HandleWorldDataReady;

        StopAllHitFlashCoroutines();
    }

    private void TryBindTimeManager()
    {
        TimeManagerView found = FindAnyObjectByType<TimeManagerView>();
        if (found == null)
            return;

        BindTimeManager(found);
    }

    private IEnumerator BindTimeManagerWhenReady()
    {
        int attempts = 0;

        while (_timeManager == null)
        {
            attempts++;
            TryBindTimeManager();
            if (_timeManager == null)
            {
                if (attempts == 1 || attempts % 10 == 0)
                {
                    Debug.LogWarning(
                        "[ResourceSpawnerManager] Waiting for TimeManagerView in scene to subscribe OnDayChanged.");
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        _bindTimeManagerRoutine = null;
    }

    private void BindTimeManager(TimeManagerView manager)
    {
        if (manager == null)
            return;

        if (_timeManager == manager)
        {
            // Defensive de-duplication in case lifecycle methods re-enter.
            _timeManager.OnDayChanged -= TriggerNewDaySpawning;
            _timeManager.OnDayChanged += TriggerNewDaySpawning;
            return;
        }

        UnbindTimeManager();
        _timeManager = manager;
        _timeManager.OnDayChanged += TriggerNewDaySpawning;
        LogDebug("Subscribed to TimeManagerView.OnDayChanged.");
    }

    private void UnbindTimeManager()
    {
        if (_timeManager == null)
            return;

        _timeManager.OnDayChanged -= TriggerNewDaySpawning;
        LogDebug("Unsubscribed from TimeManagerView.OnDayChanged.");
        _timeManager = null;
    }

    private void LogDebug(string message)
    {
        if (!showDebugLogs)
            return;

        Debug.Log($"[ResourceSpawnerManager] {message}");
    }

    /// <summary>Returns true if the tile is valid for spawning ANY configured resource type (union of all tilemap sets).</summary>
    public bool IsValidSpawnTile(int chunkX, int chunkY, int tileIndex)
    {
        if (resourceTypeMappings == null || resourceTypeMappings.Count == 0) return false;
        foreach (var mapping in resourceTypeMappings)
        {
            if (IsValidSpawnTileForType(chunkX, chunkY, tileIndex, mapping.allowedTilemaps))
                return true;
        }
        return false;
    }

    /// <summary>Returns true if the tile falls within ANY of the provided tilemaps.</summary>
    public bool IsValidSpawnTileForType(int chunkX, int chunkY, int tileIndex, List<Tilemap> tilemaps)
    {
        if (tilemaps == null || tilemaps.Count == 0) return false;
        Vector3 worldPos = TileIndexToWorldPosition(chunkX, chunkY, tileIndex);
        foreach (var tilemap in tilemaps)
        {
            if (tilemap == null) continue;
            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            if (tilemap.HasTile(cellPos)) return true;
        }
        return false;
    }

    public void TriggerNewDaySpawning()
    {
        LogDebug($"OnDayChanged received. IsMasterClient={PhotonNetwork.IsMasterClient}, InRoom={PhotonNetwork.InRoom}");

        if (!PhotonNetwork.IsMasterClient) return;

        var worldData = WorldDataManager.Instance;
        var catalog = ResourceCatalogManager.Instance;
        int totalSpawned = 0;
        int activeSections = 0;
        int nullSections = 0;
        int chunksChecked = 0;
        int chunksLoaded = 0;
        int chunksAtCapacity = 0;
        int chunksNoSpawnBudget = 0;
        int totalValidTilesFound = 0;

        if (worldData == null || !worldData.IsInitialized)
        {
            Debug.LogWarning("[ResourceSpawnerManager] WorldDataManager is not ready.");
            return;
        }

        if (catalog == null || !catalog.IsReady || catalog.resourceConfigs.Count == 0)
        {
            Debug.LogWarning("[ResourceSpawnerManager] Resource catalog is not ready.");
            return;
        }

        if (resourceTypeMappings == null || resourceTypeMappings.Count == 0)
        {
            Debug.LogWarning("[ResourceSpawnerManager] resourceTypeMappings is empty.");
            return;
        }

        // Cache sync manager once — avoids FindAnyObjectByType per spawned resource.
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        var resourceIdsByType = BuildResourceIdsByType(catalog);
        int chunkSize = GetChunkSize();

        // Evaluate noise from random coordinate offsets each time to randomize shapes per day.
        // We split X and Y to prevent diagonal drifting bias in the noise map.
        // We add 1,000,000 to prevent Unity's Mathf.PerlinNoise from mirroring across negative world coordinate axes.
        _dailyNoiseOffsetX = Random.Range(100000f, 200000f) + 1000000f;
        _dailyNoiseOffsetY = Random.Range(300000f, 400000f) + 1000000f;

        foreach (var sectionConfig in worldData.sectionConfigs)
        {
            if (!sectionConfig.IsActive) continue;
            activeSections++;

            for (int cx = sectionConfig.ChunkStartX; cx < sectionConfig.ChunkStartX + sectionConfig.ChunksWidth; cx++)
            {
            for (int cy = sectionConfig.ChunkStartY; cy < sectionConfig.ChunkStartY + sectionConfig.ChunksHeight; cy++)
            {
                chunksChecked++;
                var chunkPos = new Vector2Int(cx, cy);
                UnifiedChunkData chunk = worldData.GetChunk(sectionConfig.SectionId, chunkPos);
                if (chunk == null) continue;
                chunksLoaded++;

                int currentResources = chunk.GetResourceCount();
                if (currentResources >= maxResourcesPerChunk)
                {
                    chunksAtCapacity++;
                    continue;
                }

                // Shared budget for this chunk across all types this day.
                int chunkBudget = Mathf.Min(dailySpawnRate, maxResourcesPerChunk - currentResources);
                if (chunkBudget <= 0)
                {
                    chunksNoSpawnBudget++;
                    continue;
                }

                // Per-type spawn: each type uses its own tilemap set and respects the global cap.
                foreach (var mapping in resourceTypeMappings)
                {
                    if (chunkBudget <= 0) break;

                    string typeKey = mapping.resourceType?.ToLower() ?? "";
                    if (string.IsNullOrEmpty(typeKey)) continue;

                    // Skip if this type has reached its world cap.
                    if (mapping.maxOnMap > 0)
                    {
                        _worldResourceCounts.TryGetValue(typeKey, out int currentTypeCount);
                        if (currentTypeCount >= mapping.maxOnMap)
                        {
                            LogDebug($"  Type '{typeKey}' at world cap ({currentTypeCount}/{mapping.maxOnMap}), skipping.");
                            continue;
                        }
                    }

                    if (!resourceIdsByType.TryGetValue(typeKey, out var idsForType) || idsForType.Count == 0)
                    {
                        LogDebug($"  Type '{typeKey}' has no matching resourceIds in catalog. Catalog types: [{string.Join(", ", resourceIdsByType.Keys)}]");
                        continue;
                    }

                    TileScanStats scanStats;
                    List<int> validTiles = FindValidTilesForType(chunk, chunkSize, mapping.allowedTilemaps, out scanStats);
                    if (validTiles.Count == 0)
                    {
                        LogDebug($"  Type '{typeKey}' chunk({chunk.ChunkX},{chunk.ChunkY}): 0 valid tiles. " +
                            $"Checked={scanStats.TotalTilesChecked}, InvalidTilemap={scanStats.InvalidSpawnMask}, " +
                            $"Tilled={scanStats.BlockedByTilled}, Crop={scanStats.BlockedByCrop}, " +
                            $"Structure={scanStats.BlockedByStructure}, Resource={scanStats.BlockedByResource}, " +
                            $"Tilemaps={mapping.allowedTilemaps?.Count ?? 0}");
                        continue;
                    }

                    totalValidTilesFound += validTiles.Count;

                    var noisePassedTiles = new List<int>();
                    foreach (int tileIndex in validTiles)
                    {
                        Vector2Int worldTile = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
                        float sampleX = (worldTile.x + 0.5f) * noiseScale + _dailyNoiseOffsetX;
                        float sampleY = (worldTile.y + 0.5f) * noiseScale + _dailyNoiseOffsetY;
                        if (Mathf.PerlinNoise(sampleX, sampleY) >= noiseThreshold)
                            noisePassedTiles.Add(tileIndex);
                    }
                    Shuffle(noisePassedTiles);

                    int typeGlobalBudget = mapping.maxOnMap > 0
                        ? mapping.maxOnMap - (_worldResourceCounts.TryGetValue(typeKey, out int tc) ? tc : 0)
                        : int.MaxValue;

                    int spawnCount = Mathf.Min(chunkBudget, Mathf.Min(noisePassedTiles.Count, typeGlobalBudget));

                    for (int i = 0; i < spawnCount; i++)
                    {
                        int tileIndex = noisePassedTiles[i];
                        string pickedId = PickWeightedResource(idsForType, catalog);
                        if (pickedId == null) continue;

                        ResourceConfigData configData = catalog.GetResourceConfig(pickedId);
                        if (configData == null) continue;

                        Vector2Int worldTile = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
                        bool placed = chunk.PlaceResource(pickedId, Mathf.Max(1, configData.maxHp), worldTile.x, worldTile.y);
                        if (!placed) continue;

                        totalSpawned++;
                        chunkBudget--;

                        _worldResourceCounts.TryGetValue(typeKey, out int typeCount);
                        _worldResourceCounts[typeKey] = typeCount + 1;

                        chunk.IsDirty = true;
                        WorldSaveManager.TryMarkChunkDirty(chunk.ChunkX, chunk.ChunkY, chunk.SectionId);
                        syncManager?.BroadcastResourceSpawned(worldTile.x, worldTile.y, pickedId, Mathf.Max(1, configData.maxHp));
                        SpawnResourceVisualLocally(chunk.ChunkX, chunk.ChunkY, tileIndex, pickedId);
                    }
                }
            }
            }
        }

        LogDebug($"Daily spawn pass complete. Spawned={totalSpawned}, Sections={activeSections}, NullSections={nullSections}, " +
            $"ChunksChecked={chunksChecked}, ChunksLoaded={chunksLoaded}, AtCapacity={chunksAtCapacity}, " +
            $"NoBudget={chunksNoSpawnBudget}, ValidTiles={totalValidTilesFound}.");
    }

    public void SpawnResourceVisualLocally(int chunkX, int chunkY, int tileIndex, string resourceId)
    {
        // Only render visuals for chunks currently loaded on this client.
        // When the player later approaches, ChunkLoadingManager will call this method
        // again via SpawnChunkVisualsAsync for all tiles in the chunk.
        if (_chunkLoadingManager != null && !_chunkLoadingManager.IsChunkLoaded(new Vector2Int(chunkX, chunkY)))
            return;

        string visualKey = MakeVisualKey(chunkX, chunkY, tileIndex);
        if (_spawnedVisuals.TryGetValue(visualKey, out GameObject existing))
        {
            if (existing != null) return;
            _spawnedVisuals.Remove(visualKey);
            ClearVisualTracking(visualKey);
        }

        ResourceConfigData configData = ResourceCatalogManager.Instance?.GetResourceConfig(resourceId);
        if (configData == null)
        {
            // No catalog data — spawn placeholder if available (orphaned data from late-join)
            var fallbackSprite = FallbackConfig.Instance?.PlaceholderSprite;
            if (fallbackSprite != null)
            {
                Vector3 worldPosPlaceholder = TileIndexToWorldPosition(chunkX, chunkY, tileIndex);
                var placeholderObj = new GameObject($"Resource_Fallback_{resourceId}_{chunkX}_{chunkY}_{tileIndex}");
                placeholderObj.transform.position = worldPosPlaceholder;
                var sr = placeholderObj.AddComponent<SpriteRenderer>();
                sr.sprite = fallbackSprite;
                sr.sortingLayerName = "WalkInfront";
                _spawnedVisuals[visualKey] = placeholderObj;
                _baseVisualScales[visualKey] = placeholderObj.transform.localScale;
            }
            else
            {
                Debug.LogWarning($"[ResourceSpawnerManager] Missing config data for resource '{resourceId}'.");
            }
            return;
        }

        GameObject prefabToUse = treePrefab; // Default fallback
        if (!string.IsNullOrEmpty(configData.resourceType))
        {
            switch (configData.resourceType.ToLower())
            {
                case "tree": prefabToUse = treePrefab; break;
                case "rock": prefabToUse = rockPrefab; break;
                case "ore": prefabToUse = orePrefab; break;
                default: prefabToUse = treePrefab; break; // safe fallback
            }
        }

        if (prefabToUse == null)
        {
            Debug.LogWarning($"[ResourceSpawnerManager] Prefab for resourceType '{configData.resourceType}' is not assigned.");
            return;
        }

        Vector3 worldPos = TileIndexToWorldPosition(chunkX, chunkY, tileIndex);
        GameObject visual = Instantiate(prefabToUse, worldPos, Quaternion.identity);
        visual.name = $"Resource_{resourceId}_{chunkX}_{chunkY}_{tileIndex}";
        _spawnedVisuals[visualKey] = visual;
        _baseVisualScales[visualKey] = visual.transform.localScale;
        _visualKeyToResourceType[visualKey] = (configData.resourceType ?? "tree").ToLower();

        if (string.IsNullOrEmpty(configData.spriteUrl))
        {
            Debug.LogWarning($"[ResourceSpawnerManager] Missing spriteUrl for resource '{resourceId}'.");
            return;
        }

        SpriteRenderer spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
            spriteRenderer = visual.AddComponent<SpriteRenderer>();

        if (_spriteCache.TryGetValue(configData.spriteUrl, out Sprite cachedSprite))
        {
            spriteRenderer.sprite = cachedSprite;
        }
        else
        {
            StartCoroutine(LoadAndApplySprite(spriteRenderer, configData.spriteUrl, resourceId));
        }
    }

    private IEnumerator LoadAndApplySprite(SpriteRenderer spriteRenderer, string url, string resourceId)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        using var request = UnityWebRequestTexture.GetTexture(url);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[ResourceSpawnerManager] Failed to download sprite for resource '{resourceId}' from '{url}': {request.error}");
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(request);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.065f), // Bottom-Center pivot
                16f,
                0,
                SpriteMeshType.FullRect);
            
            sprite.name = $"Resource_{resourceId}";
            _spriteCache[url] = sprite;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }

    public bool TryGetResourceVisual(int chunkX, int chunkY, int tileIndex, out GameObject visual)
    {
        string key = MakeVisualKey(chunkX, chunkY, tileIndex);
        if (_spawnedVisuals.TryGetValue(key, out visual) && visual != null)
            return true;

        if (_spawnedVisuals.ContainsKey(key))
        {
            _spawnedVisuals.Remove(key);
            ClearVisualTracking(key);
        }

        visual = null;
        return false;
    }

    public void RemoveResourceVisual(int chunkX, int chunkY, int tileIndex)
    {
        string key = MakeVisualKey(chunkX, chunkY, tileIndex);

        // Decrement world-type count — called on MasterClient only (ResourceInteractionManager).
        if (_visualKeyToResourceType.TryGetValue(key, out string removedType))
        {
            if (_worldResourceCounts.TryGetValue(removedType, out int typeCount) && typeCount > 0)
                _worldResourceCounts[removedType] = typeCount - 1;
            _visualKeyToResourceType.Remove(key);
        }

        if (_spawnedVisuals.TryGetValue(key, out GameObject visual))
        {
            ClearVisualTracking(key);
            if (visual != null)
                Destroy(visual);
            _spawnedVisuals.Remove(key);
        }
    }

    private List<int> FindValidTilesForType(
        UnifiedChunkData chunk, int chunkSize,
        List<Tilemap> tilemaps, out TileScanStats stats)
    {
        stats = new TileScanStats();
        var valid = new List<int>();
        int totalTiles = chunkSize * chunkSize;

        for (int tileIndex = 0; tileIndex < totalTiles; tileIndex++)
        {
            stats.TotalTilesChecked++;
            if (!IsValidSpawnTileForType(chunk.ChunkX, chunk.ChunkY, tileIndex, tilemaps))
            {
                stats.InvalidSpawnMask++;
                continue;
            }

            Vector2Int worldTile = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
            if (chunk.IsTilled(worldTile.x, worldTile.y))    { stats.BlockedByTilled++;    continue; }
            if (chunk.HasCrop(worldTile.x, worldTile.y))      { stats.BlockedByCrop++;      continue; }
            if (chunk.HasStructure(worldTile.x, worldTile.y)) { stats.BlockedByStructure++; continue; }
            if (chunk.HasResource(worldTile.x, worldTile.y))  { stats.BlockedByResource++;  continue; }

            valid.Add(tileIndex);
        }

        return valid;
    }

    private Vector2Int TileIndexToWorldTile(int chunkX, int chunkY, int tileIndex)
    {
        int chunkSize = GetChunkSize();
        int localX = tileIndex % chunkSize;
        int localY = tileIndex / chunkSize;

        int worldX = (chunkX * chunkSize) + localX;
        int worldY = (chunkY * chunkSize) + localY;
        return new Vector2Int(worldX, worldY);
    }

    private Vector3 TileIndexToWorldPosition(int chunkX, int chunkY, int tileIndex)
    {
        Vector2Int worldTile = TileIndexToWorldTile(chunkX, chunkY, tileIndex);
        return new Vector3(worldTile.x, worldTile.y, 0f); // Render exactly at integer grid intersection
    }

    private int GetChunkSize()
    {
        return Mathf.Max(1, WorldDataManager.Instance != null
            ? WorldDataManager.Instance.chunkSizeTiles
            : 30);
    }

    private static void Shuffle(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static string MakeVisualKey(int chunkX, int chunkY, int tileIndex)
    {
        return $"{chunkX}:{chunkY}:{tileIndex}";
    }

    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }

    private void HandleResourceHpUpdated(int worldX, int worldY, int newHp)
    {
        if (!WorldTileToVisualKey(worldX, worldY, out string key))
            return;

        if (!_spawnedVisuals.TryGetValue(key, out GameObject visual) || visual == null)
        {
            ClearVisualTracking(key);
            return;
        }

        StartOrRestartHitFlash(key, visual);
    }

    private void HandleResourceRemoved(int worldX, int worldY)
    {
        if (WorldTileToVisualKey(worldX, worldY, out string key))
        {
            ClearVisualTracking(key);
            if (_spawnedVisuals.TryGetValue(key, out GameObject visual) && visual != null)
            {
                Destroy(visual);
            }
            _spawnedVisuals.Remove(key);
        }
    }

    private void HandleResourceSpawned(int worldX, int worldY, string resourceId)
    {
        WorldDataManager worldData = WorldDataManager.Instance;
        if (worldData == null) return;

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        Vector2Int chunkPos = worldData.WorldToChunkCoords(worldPos);
        
        int chunkSize = worldData.chunkSizeTiles;
        int localX = worldX - (chunkPos.x * chunkSize);
        int localY = worldY - (chunkPos.y * chunkSize);

        int tileIndex = localY * chunkSize + localX;

        SpawnResourceVisualLocally(chunkPos.x, chunkPos.y, tileIndex, resourceId);
    }

    private void StartOrRestartHitFlash(string key, GameObject visual)
    {
        if (visual == null)
        {
            ClearVisualTracking(key);
            return;
        }

        if (!_baseVisualScales.TryGetValue(key, out Vector3 baseScale))
        {
            baseScale = visual.transform.localScale;
            _baseVisualScales[key] = baseScale;
        }

        if (_activeHitFlashCoroutines.TryGetValue(key, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
        }

        // Always reset to canonical scale before replaying hit feedback.
        visual.transform.localScale = baseScale;
        _activeHitFlashCoroutines[key] = StartCoroutine(HitFlashVisual(key, visual, baseScale));
    }

    private IEnumerator HitFlashVisual(string key, GameObject visual, Vector3 baseScale)
    {
        if (visual == null)
        {
            ClearVisualTracking(key);
            yield break;
        }

        visual.transform.localScale = baseScale * 0.95f;
        yield return new WaitForSeconds(0.1f);

        if (visual != null)
            visual.transform.localScale = baseScale;

        _activeHitFlashCoroutines.Remove(key);
    }

    private void ClearVisualTracking(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (_activeHitFlashCoroutines.TryGetValue(key, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
        }

        _activeHitFlashCoroutines.Remove(key);
        _baseVisualScales.Remove(key);
    }

    private void StopAllHitFlashCoroutines()
    {
        foreach (Coroutine running in _activeHitFlashCoroutines.Values)
        {
            if (running != null)
                StopCoroutine(running);
        }

        _activeHitFlashCoroutines.Clear();

        foreach (string key in _spawnedVisuals.Keys)
        {
            if (_spawnedVisuals.TryGetValue(key, out GameObject visual) && visual != null &&
                _baseVisualScales.TryGetValue(key, out Vector3 baseScale))
            {
                visual.transform.localScale = baseScale;
            }
        }

        _baseVisualScales.Clear();
    }

    private bool TryGetVisualFromWorld(int worldX, int worldY, out GameObject visual)
    {
        visual = null;
        if (!WorldTileToVisualKey(worldX, worldY, out string key)) return false;
        return _spawnedVisuals.TryGetValue(key, out visual) && visual != null;
    }

    private bool WorldTileToVisualKey(int worldX, int worldY, out string key)
    {
        key = null;
        WorldDataManager worldData = WorldDataManager.Instance;
        if (worldData == null) return false;

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        Vector2Int chunkPos = worldData.WorldToChunkCoords(worldPos);
        
        int chunkSize = worldData.chunkSizeTiles;
        int localX = worldX - (chunkPos.x * chunkSize);
        int localY = worldY - (chunkPos.y * chunkSize);

        int tileIndex = localY * chunkSize + localX;
        
        key = MakeVisualKey(chunkPos.x, chunkPos.y, tileIndex);
        return true;
    }

    // ── Per-type spawn helpers ────────────────────────────────────────────────

    /// <summary>Groups catalog resource IDs by their resourceType string (lowercase).</summary>
    private Dictionary<string, List<string>> BuildResourceIdsByType(ResourceCatalogManager catalog)
    {
        var result = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in catalog.resourceConfigs)
        {
            string type = (kvp.Value?.resourceType ?? "tree").ToLower();
            if (!result.TryGetValue(type, out var list))
                result[type] = list = new List<string>();
            list.Add(kvp.Key);
        }
        return result;
    }

    /// <summary>Picks a resourceId from the given list using weighted random.</summary>
    private string PickWeightedResource(List<string> ids, ResourceCatalogManager catalog)
    {
        int totalWeight = 0;
        foreach (var id in ids)
        {
            var cfg = catalog.GetResourceConfig(id);
            if (cfg != null) totalWeight += Mathf.Max(1, cfg.spawnWeight);
        }
        if (totalWeight == 0) return ids.Count > 0 ? ids[0] : null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var id in ids)
        {
            var cfg = catalog.GetResourceConfig(id);
            if (cfg == null) continue;
            cumulative += Mathf.Max(1, cfg.spawnWeight);
            if (roll < cumulative) return id;
        }
        return ids[ids.Count - 1];
    }

    /// <summary>
    /// Rebuilds _worldResourceCounts by scanning all loaded chunks.
    /// Called after world data bootstrap completes.
    /// </summary>
    private void RebuildResourceCounts()
    {
        _worldResourceCounts.Clear();
        var worldData = WorldDataManager.Instance;
        var catalog   = ResourceCatalogManager.Instance;
        if (worldData == null || catalog == null) return;

        foreach (var sectionConfig in worldData.sectionConfigs)
        {
            if (!sectionConfig.IsActive) continue;
            var section = worldData.GetSection(sectionConfig.SectionId);
            if (section == null) continue;

            foreach (var chunk in section.Values)
            {
                if (chunk == null || !chunk.IsLoaded) continue;
                foreach (var tile in chunk.GetAllResources())
                {
                    var config = catalog.GetResourceConfig(tile.Resource.ResourceId);
                    if (config == null) continue;
                    string type = (config.resourceType ?? "tree").ToLower();
                    _worldResourceCounts.TryGetValue(type, out int count);
                    _worldResourceCounts[type] = count + 1;
                }
            }
        }

        var sb = new System.Text.StringBuilder();
        foreach (var kvp in _worldResourceCounts)
            sb.Append($"{kvp.Key}={kvp.Value} ");
        LogDebug($"[RebuildResourceCounts] {sb}");
    }

    // ── World-data-ready handler ──────────────────────────────────────────────

    private void HandleWorldDataReady()
    {
        RebuildResourceCounts();

        if (!PhotonNetwork.IsMasterClient) return;

        // If this is a brand-new world (flag set by CreateWorld.LoadCreatedWorld), fill it now.
        if (WorldSelectionManager.Instance != null && WorldSelectionManager.Instance.IsNewWorld)
            StartCoroutine(RunInitialWorldFill());
    }

    // ── Initial world fill (one-time, new worlds only) ────────────────────────

    private IEnumerator RunInitialWorldFill()
    {
        LogDebug("Starting initial world fill pass.");

        var worldData = WorldDataManager.Instance;
        var catalog   = ResourceCatalogManager.Instance;

        if (worldData == null || !worldData.IsInitialized || catalog == null || !catalog.IsReady)
        {
            Debug.LogWarning("[ResourceSpawnerManager] Cannot run initial fill — managers not ready.");
            yield break;
        }

        if (resourceTypeMappings == null || resourceTypeMappings.Count == 0)
        {
            Debug.LogWarning("[ResourceSpawnerManager] Cannot run initial fill — resourceTypeMappings empty.");
            yield break;
        }

        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        var resourceIdsByType = BuildResourceIdsByType(catalog);
        int chunkSize  = GetChunkSize();
        int totalSpawned = 0;
        int chunksProcessed = 0;
        const int chunksPerFrame = 3;

        _dailyNoiseOffsetX = Random.Range(100000f, 200000f) + 1000000f;
        _dailyNoiseOffsetY = Random.Range(300000f, 400000f) + 1000000f;

        foreach (var sectionConfig in worldData.sectionConfigs)
        {
            if (!sectionConfig.IsActive) continue;
            for (int cx = sectionConfig.ChunkStartX; cx < sectionConfig.ChunkStartX + sectionConfig.ChunksWidth; cx++)
            {
            for (int cy = sectionConfig.ChunkStartY; cy < sectionConfig.ChunkStartY + sectionConfig.ChunksHeight; cy++)
            {
                var chunkPos = new Vector2Int(cx, cy);
                UnifiedChunkData chunk = worldData.GetChunk(sectionConfig.SectionId, chunkPos);
                if (chunk == null) { chunksProcessed++; goto YieldCheck; }

                int chunkBudget = maxResourcesPerChunk - chunk.GetResourceCount();

                if (chunkBudget > 0)
                {
                    foreach (var mapping in resourceTypeMappings)
                    {
                        if (chunkBudget <= 0) break;

                        string typeKey = mapping.resourceType?.ToLower() ?? "";
                        if (string.IsNullOrEmpty(typeKey)) continue;

                        if (mapping.maxOnMap > 0)
                        {
                            _worldResourceCounts.TryGetValue(typeKey, out int currentTypeCount);
                            if (currentTypeCount >= mapping.maxOnMap) continue;
                        }

                        if (!resourceIdsByType.TryGetValue(typeKey, out var idsForType) || idsForType.Count == 0)
                            continue;

                        TileScanStats scanStats;
                        var validTiles = FindValidTilesForType(chunk, chunkSize, mapping.allowedTilemaps, out scanStats);
                        if (validTiles.Count == 0) continue;

                        var noisePassedTiles = new List<int>();
                        foreach (int tileIndex in validTiles)
                        {
                            Vector2Int wt = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
                            float sX = (wt.x + 0.5f) * noiseScale + _dailyNoiseOffsetX;
                            float sY = (wt.y + 0.5f) * noiseScale + _dailyNoiseOffsetY;
                            if (Mathf.PerlinNoise(sX, sY) >= noiseThreshold)
                                noisePassedTiles.Add(tileIndex);
                        }
                        Shuffle(noisePassedTiles);

                        int typeGlobalBudget = mapping.maxOnMap > 0
                            ? mapping.maxOnMap - (_worldResourceCounts.TryGetValue(typeKey, out int tc) ? tc : 0)
                            : int.MaxValue;

                        int spawnCount = Mathf.Min(chunkBudget, Mathf.Min(noisePassedTiles.Count, typeGlobalBudget));

                        for (int i = 0; i < spawnCount; i++)
                        {
                            int tileIndex = noisePassedTiles[i];
                            string pickedId = PickWeightedResource(idsForType, catalog);
                            if (pickedId == null) continue;

                            ResourceConfigData configData = catalog.GetResourceConfig(pickedId);
                            if (configData == null) continue;

                            Vector2Int wt2 = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
                            bool placed = chunk.PlaceResource(pickedId, Mathf.Max(1, configData.maxHp), wt2.x, wt2.y);
                            if (!placed) continue;

                            totalSpawned++;
                            chunkBudget--;

                            _worldResourceCounts.TryGetValue(typeKey, out int typeCount);
                            _worldResourceCounts[typeKey] = typeCount + 1;

                            chunk.IsDirty = true;
                            WorldSaveManager.TryMarkChunkDirty(chunk.ChunkX, chunk.ChunkY, chunk.SectionId);
                            syncManager?.BroadcastResourceSpawned(wt2.x, wt2.y, pickedId, Mathf.Max(1, configData.maxHp));
                            SpawnResourceVisualLocally(chunk.ChunkX, chunk.ChunkY, tileIndex, pickedId);
                        }
                    }
                }

                chunksProcessed++;
                YieldCheck:
                if (chunksProcessed % chunksPerFrame == 0)
                    yield return null;
            }
            }
        }

        LogDebug($"Initial world fill complete. Spawned={totalSpawned}.");
    }
}
