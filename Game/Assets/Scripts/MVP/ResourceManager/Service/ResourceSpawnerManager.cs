using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;

/// <summary>
/// Host-authoritative resource spawner.
/// Spawns resource state in RAM and broadcasts visual-only prefab instantiation via RPC.
/// </summary>
public class ResourceSpawnerManager : MonoBehaviourPun, IInRoomCallbacks
{
    public static ResourceSpawnerManager Instance { get; private set; }

    [Header("Spawn Mask")]
    public Tilemap spawnZoneMap;

    [Header("Spawn Rules")]
    public int maxResourcesPerChunk = 40;
    public int dailySpawnRate = 5;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        _resourceHarvestingService = new ResourceHarvestingService(
            WorldDataManager.Instance,
            FindAnyObjectByType<ChunkDataSyncManager>(),
            FindAnyObjectByType<InventoryGameView>()
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

    public bool IsValidSpawnTile(int chunkX, int chunkY, int tileIndex)
    {
        if (spawnZoneMap == null) return false;

        Vector3 worldPos = TileIndexToWorldPosition(chunkX, chunkY, tileIndex);
        // World tile coordinates in this project are already cell-aligned.
        // Sampling with +0.5f shifts lookup upward/right and creates directional bias.
        Vector3Int cellPos = spawnZoneMap.WorldToCell(worldPos);
        return spawnZoneMap.HasTile(cellPos);
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
        int chunksNoValidTiles = 0;
        int totalValidTilesFound = 0;
        TileScanStats totalTileScanStats = new TileScanStats();

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

        List<string> resourceIds = new List<string>(catalog.resourceConfigs.Keys);
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

            Dictionary<Vector2Int, UnifiedChunkData> section =
                worldData.GetSection(sectionConfig.SectionId);
            if (section == null)
            {
                nullSections++;
                continue;
            }

            foreach (var pair in section)
            {
                chunksChecked++;
                UnifiedChunkData chunk = pair.Value;
                if (chunk == null || !chunk.IsLoaded) continue;
                chunksLoaded++;

                int currentResources = chunk.GetResourceCount();
                if (currentResources >= maxResourcesPerChunk)
                {
                    chunksAtCapacity++;
                    continue;
                }

                int amountToSpawn = Mathf.Min(
                    dailySpawnRate,
                    maxResourcesPerChunk - currentResources);
                if (amountToSpawn <= 0)
                {
                    chunksNoSpawnBudget++;
                    continue;
                }

                TileScanStats scanStats;
                List<int> validTiles = FindValidEmptyTiles(chunk, chunkSize, out scanStats);
                totalTileScanStats.TotalTilesChecked += scanStats.TotalTilesChecked;
                totalTileScanStats.InvalidSpawnMask += scanStats.InvalidSpawnMask;
                totalTileScanStats.BlockedByTilled += scanStats.BlockedByTilled;
                totalTileScanStats.BlockedByCrop += scanStats.BlockedByCrop;
                totalTileScanStats.BlockedByStructure += scanStats.BlockedByStructure;
                totalTileScanStats.BlockedByResource += scanStats.BlockedByResource;

                if (validTiles.Count == 0)
                {
                    chunksNoValidTiles++;
                    continue;
                }

                totalValidTilesFound += validTiles.Count;

                // --- Evualate Perlin Noise for Valid Tiles ---
                List<int> noisePassedTiles = new List<int>();
                foreach (int tileIndex in validTiles)
                {
                    Vector2Int worldTile = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
                    
                    // Sample from the center of the grid cell to prevent integer artifacts
                    float sampleX = (worldTile.x + 0.5f) * noiseScale + _dailyNoiseOffsetX;
                    float sampleY = (worldTile.y + 0.5f) * noiseScale + _dailyNoiseOffsetY;

                    float noiseVal = Mathf.PerlinNoise(sampleX, sampleY);
                        
                    if (noiseVal >= noiseThreshold)
                    {
                        noisePassedTiles.Add(tileIndex);
                    }
                }

                // We still shuffle passed tiles to avoid spawning exactly top-left downwards
                Shuffle(noisePassedTiles);
                int spawnCount = Mathf.Min(amountToSpawn, noisePassedTiles.Count);

                for (int i = 0; i < spawnCount; i++)
                {
                    int tileIndex = noisePassedTiles[i];
                    
                    // --- Weighted random selection logic ---
                    int totalWeight = 0;
                    foreach (var id in resourceIds)
                    {
                        var cfg = catalog.GetResourceConfig(id);
                        if (cfg != null) totalWeight += Mathf.Max(1, cfg.spawnWeight);
                    }

                    int randomWeight = Random.Range(0, totalWeight);
                    string pickedId = resourceIds[0];
                    int cumulativeWeight = 0;

                    foreach (var id in resourceIds)
                    {
                        var cfg = catalog.GetResourceConfig(id);
                        if (cfg != null)
                        {
                            cumulativeWeight += Mathf.Max(1, cfg.spawnWeight);
                            if (randomWeight < cumulativeWeight)
                            {
                                pickedId = id;
                                break;
                            }
                        }
                    }
                    // ---------------------------------------

                    ResourceConfigData configData = catalog.GetResourceConfig(pickedId);
                    if (configData == null) continue;

                    Vector2Int worldTile = TileIndexToWorldTile(
                        chunk.ChunkX,
                        chunk.ChunkY,
                        tileIndex);

                    bool placed = chunk.PlaceResource(
                        pickedId,
                        Mathf.Max(1, configData.maxHp),
                        worldTile.x,
                        worldTile.y);
                    if (!placed) continue;

                    totalSpawned++;

                    chunk.IsDirty = true;
                    WorldSaveManager.TryMarkChunkDirty(
                        chunk.ChunkX,
                        chunk.ChunkY,
                        chunk.SectionId);

                    ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
                    if (syncManager != null)
                    {
                        syncManager.BroadcastResourceSpawned(worldTile.x, worldTile.y, pickedId, Mathf.Max(1, configData.maxHp));
                    }
                    SpawnResourceVisualLocally(chunk.ChunkX, chunk.ChunkY, tileIndex, pickedId);
                }
            }
        }

        LogDebug(
            $"Daily spawn pass complete. Spawned={totalSpawned}, ActiveSections={activeSections}, NullSections={nullSections}, " +
            $"ChunksChecked={chunksChecked}, ChunksLoaded={chunksLoaded}, AtCapacity={chunksAtCapacity}, NoBudget={chunksNoSpawnBudget}, " +
            $"NoValidTiles={chunksNoValidTiles}, TotalValidTiles={totalValidTilesFound}.");

        if (totalSpawned == 0)
        {
            LogDebug(
                $"Zero-spawn diagnostics: TilesChecked={totalTileScanStats.TotalTilesChecked}, InvalidSpawnMask={totalTileScanStats.InvalidSpawnMask}, " +
                $"BlockedTilled={totalTileScanStats.BlockedByTilled}, BlockedCrop={totalTileScanStats.BlockedByCrop}, " +
                $"BlockedStructure={totalTileScanStats.BlockedByStructure}, BlockedResource={totalTileScanStats.BlockedByResource}.");
        }
    }

    public void SpawnResourceVisualLocally(int chunkX, int chunkY, int tileIndex, string resourceId)
    {
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
            Debug.LogWarning($"[ResourceSpawnerManager] Missing config data for resource '{resourceId}'.");
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
        if (_spawnedVisuals.TryGetValue(key, out GameObject visual))
        {
            ClearVisualTracking(key);
            if (visual != null)
                Destroy(visual);
            _spawnedVisuals.Remove(key);
        }
    }

    private List<int> FindValidEmptyTiles(UnifiedChunkData chunk, int chunkSize, out TileScanStats stats)
    {
        stats = new TileScanStats();
        List<int> valid = new List<int>();
        int totalTiles = chunkSize * chunkSize;

        for (int tileIndex = 0; tileIndex < totalTiles; tileIndex++)
        {
            stats.TotalTilesChecked++;
            if (!IsValidSpawnTile(chunk.ChunkX, chunk.ChunkY, tileIndex))
            {
                stats.InvalidSpawnMask++;
                continue;
            }

            Vector2Int worldTile = TileIndexToWorldTile(chunk.ChunkX, chunk.ChunkY, tileIndex);
            if (chunk.IsTilled(worldTile.x, worldTile.y))
            {
                stats.BlockedByTilled++;
                continue;
            }

            if (chunk.HasCrop(worldTile.x, worldTile.y))
            {
                stats.BlockedByCrop++;
                continue;
            }

            if (chunk.HasStructure(worldTile.x, worldTile.y))
            {
                stats.BlockedByStructure++;
                continue;
            }

            if (chunk.HasResource(worldTile.x, worldTile.y))
            {
                stats.BlockedByResource++;
                continue;
            }

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
}
