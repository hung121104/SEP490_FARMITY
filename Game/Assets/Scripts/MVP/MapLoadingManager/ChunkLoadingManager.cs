using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// View layer for the chunk loading system (MVP pattern).
///
/// Responsibilities:
///   – Exposes Inspector fields so scene configuration requires no code changes
///   – Owns Unity visual operations: Tilemap writes, GameObject activation, pool calls, coroutines
///   – Forwards Unity lifecycle and Photon callbacks to <see cref="ChunkLoadingPresenter"/>
///   – Implements <see cref="IChunkLoadingView"/> so the Presenter can trigger side effects
///
/// Zero business logic lives here. All load/unload decisions are made in ChunkLoadingPresenter.
/// </summary>
public class ChunkLoadingManager : MonoBehaviourPunCallbacks, IChunkLoadingView
{
    // ── Inspector fields ─────────────────────────────────────────────────────

    [Header("Loading Settings")]
    [Tooltip("Load radius in chunks (1 = 3×3, 2 = 5×5, etc.)")]
    public int loadRadius = 1;

    [Tooltip("Check player position every X seconds")]
    public float updateInterval = 1f;

    [Tooltip("Delay before unloading chunks after player leaves area")]
    public float unloadDelay = 5f;

    [Header("Visual Settings")]
    [Tooltip("Show loaded chunks with crops")]
    public bool visualizeCrops = true;

    [Tooltip("Show loaded chunks with resources")]
    public bool visualizeResources = true;

    [Tooltip("Prefab used to render a crop. Assign a prefab with a SpriteRenderer (can be on a child object).")]
    public GameObject cropVisualPrefab;

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

    [Header("Async Spawn Settings")]
    [Tooltip("Maximum tiles to process per frame during async chunk spawn. Higher = faster but more frame lag.")]
    public int tilesPerFrame = 50;

    // ── Public events (external systems subscribe to these) ──────────────────

    /// <summary>Fired after a chunk has been loaded and its visuals spawned.</summary>
    public event Action<Vector2Int> OnChunkLoaded;

    /// <summary>Fired after a chunk's visuals have been deactivated.</summary>
    public event Action<Vector2Int> OnChunkUnloaded;

    // ── Private — MVP instances ───────────────────────────────────────────────
    private ChunkLoadingModel     _model;
    private ChunkLoadingPresenter _presenter;

    // ── Private — View-layer caches (Unity handles, not business state) ───────
    private readonly Dictionary<Vector2Int, Coroutine> _spawnCoroutines = new Dictionary<Vector2Int, Coroutine>();
    private readonly Dictionary<string, Tilemap>       _tilemapCache    = new Dictionary<string, Tilemap>();
    private StructurePool _cachedStructurePool;
    private CropPool      _cachedCropPool;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[ChunkLoading] Not connected to Photon — aborting initialization.");
            return;
        }

        _model     = new ChunkLoadingModel();
        _presenter = new ChunkLoadingPresenter(_model, this);

        _presenter.LoadRadius         = loadRadius;
        _presenter.UpdateInterval     = updateInterval;
        _presenter.UnloadDelay        = unloadDelay;
        _presenter.VisualizeCrops     = visualizeCrops;
        _presenter.VisualizeResources = visualizeResources;
        _presenter.ShowDebugLogs      = showDebugLogs;
        _presenter.EnableDailyReload  = enableDailyReload;

        if (enableDailyReload)
        {
            _model.TimeManager = FindAnyObjectByType<TimeManagerView>();
            if (_model.TimeManager != null)
            {
                _model.TimeManager.OnDayChanged += OnDayChangedHandler;
                if (showDebugLogs) Debug.Log("[ChunkLoading] Subscribed to OnDayChanged");
            }
            else
            {
                Debug.LogWarning("[ChunkLoading] TimeManagerView not found in scene!");
            }
        }

        StartCoroutine(FindLocalPlayer());
    }

    private void OnDestroy()
    {
        if (_model?.TimeManager != null)
            _model.TimeManager.OnDayChanged -= OnDayChangedHandler;
        PlayerRegistry.OnLocalPlayerSpawned -= OnRegistryPlayerSpawned;
    }

    private void Update()
    {
        if (_presenter == null || !WorldDataManager.Instance.IsInitialized) return;
        _presenter.Tick(Time.time);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Bootstrap — wait for services then hand off to Presenter
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FindLocalPlayer()
    {
        while (PlantCatalogService.Instance == null || !PlantCatalogService.Instance.IsReady)
        {
            if (showDebugLogs) Debug.Log("[ChunkLoading] Waiting for PlantCatalogService…");
            yield return new WaitForSeconds(0.5f);
        }

        float bootTimeout = 30f;
        float bootElapsed = 0f;
        while (WorldDataBootstrapper.Instance != null && !WorldDataBootstrapper.Instance.IsReady)
        {
            if (showDebugLogs) Debug.Log("[ChunkLoading] Waiting for WorldDataBootstrapper…");
            yield return new WaitForSeconds(0.5f);
            bootElapsed += 0.5f;
            if (bootElapsed >= bootTimeout)
            {
                Debug.LogWarning("[ChunkLoading] Timed out waiting for WorldDataBootstrapper — proceeding.");
                break;
            }
        }

        if (PlayerRegistry.LocalPlayerTransform != null)
        {
            if (showDebugLogs)
                Debug.Log($"[ChunkLoading] Local player already in registry: {PlayerRegistry.LocalPlayerTransform.name}");
            _presenter.OnLocalPlayerRegistered(PlayerRegistry.LocalPlayerTransform, Time.time);
        }
        else
        {
            PlayerRegistry.OnLocalPlayerSpawned += OnRegistryPlayerSpawned;
            yield return new WaitUntil(() => _model.LocalPlayerTransform != null);
        }
    }

    private void OnRegistryPlayerSpawned(Transform playerTransform)
    {
        PlayerRegistry.OnLocalPlayerSpawned -= OnRegistryPlayerSpawned;
        if (showDebugLogs) Debug.Log($"[ChunkLoading] Local player registered: {playerTransform.name}");
        _presenter.OnLocalPlayerRegistered(playerTransform, Time.time);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IChunkLoadingView — Presenter → View callbacks
    // ─────────────────────────────────────────────────────────────────────────

    void IChunkLoadingView.SpawnChunkVisualsAsync(Vector2Int chunkPos, UnifiedChunkData chunk)
    {
        var co = StartCoroutine(SpawnAndNotify(chunkPos, chunk));
        _spawnCoroutines[chunkPos] = co;
    }

    void IChunkLoadingView.ActivateChunkVisuals(Vector2Int chunkPos)   => ActivateChunkVisualsInternal(chunkPos);
    void IChunkLoadingView.DeactivateChunkVisuals(Vector2Int chunkPos) => DeactivateChunkVisualsInternal(chunkPos);
    void IChunkLoadingView.RebuildChunkVisuals(Vector2Int chunkPos)    => RefreshChunkVisuals(chunkPos);
    void IChunkLoadingView.NotifyChunkLoaded(Vector2Int chunkPos)      => OnChunkLoaded?.Invoke(chunkPos);
    void IChunkLoadingView.NotifyChunkUnloaded(Vector2Int chunkPos)    => OnChunkUnloaded?.Invoke(chunkPos);

    void IChunkLoadingView.CancelSpawnCoroutine(Vector2Int chunkPos)
    {
        if (_spawnCoroutines.TryGetValue(chunkPos, out Coroutine co))
        {
            if (co != null) StopCoroutine(co);
            _spawnCoroutines.Remove(chunkPos);
        }
    }

    void IChunkLoadingView.StartDailyReload(List<Vector2Int> chunksSnapshot)
    {
        if (showDebugLogs)
            Debug.Log($"[ChunkLoading] Staggered daily reload: {chunksSnapshot.Count} chunk(s)");
        StartCoroutine(StaggeredDailyReload(chunksSnapshot));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Photon callbacks
    // ─────────────────────────────────────────────────────────────────────────

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log("[ChunkLoading] Master client left — closing room.");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (showDebugLogs) Debug.Log($"[ChunkLoading] Player {otherPlayer.NickName} left.");
        _presenter?.OnPlayerLeft(otherPlayer.ActorNumber);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Day-change relay
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Simulate Day Change")]
    private void OnDayChangedHandler() => _presenter?.OnDayChanged();

    // ─────────────────────────────────────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator SpawnAndNotify(Vector2Int chunkPos, UnifiedChunkData chunk)
    {
        yield return StartCoroutine(SpawnChunkVisuals(chunkPos, chunk));
        _spawnCoroutines.Remove(chunkPos);
        OnChunkLoaded?.Invoke(chunkPos);
    }

    /// <summary>
    /// Spawns all crop/structure/resource visuals and tile overlays for a chunk.
    /// Throttled to <see cref="tilesPerFrame"/> tiles per frame when run as a real coroutine.
    /// Can be sync-drained via <c>while(e.MoveNext())</c> when immediate results are needed.
    /// </summary>
    private IEnumerator SpawnChunkVisuals(Vector2Int chunkPos, UnifiedChunkData chunk)
    {
        if (PlantCatalogService.Instance == null || !PlantCatalogService.Instance.IsReady)
        {
            if (showDebugLogs) Debug.LogWarning("[ChunkLoading] PlantCatalogService not ready — skipping visual spawn.");
            yield break;
        }

        var cropVisuals         = new List<GameObject>();
        var tilledPositions     = new List<Vector3Int>();
        var wateredPositions    = new List<Vector3Int>();
        int resourceCount       = 0;
        int processedThisFrame  = 0;

        ResourceSpawnerManager resourceSpawner = null;
        if (visualizeResources)
        {
            resourceSpawner = ResourceSpawnerManager.Instance ?? FindAnyObjectByType<ResourceSpawnerManager>();
            if (resourceSpawner == null && showDebugLogs)
                Debug.LogWarning("[ChunkLoading] ResourceSpawnerManager not found.");
        }

        Tilemap tilledTilemap  = FindTilemap(tilledTilemapName);
        Tilemap wateredTilemap = FindTilemap(wateredTilemapName);

        foreach (var tile in chunk.GetAllTiles())
        {
            // ── Tilled overlay ────────────────────────────────────────────────
            if (tile.IsTilled)
            {
                if (tilledTile == null)
                    { if (showDebugLogs) Debug.LogWarning("[ChunkLoading] TilledTile asset not assigned!"); }
                else if (tilledTilemap == null)
                    { if (showDebugLogs) Debug.LogWarning($"[ChunkLoading] Tilemap '{tilledTilemapName}' not found!"); }
                else
                    tilledPositions.Add(tilledTilemap.WorldToCell(new Vector3(tile.WorldX, tile.WorldY, 0)));
            }

            // ── Watered overlay ───────────────────────────────────────────────
            if (tile.IsTilled && tile.Crop.IsWatered)
            {
                if (wateredTile == null)
                    Debug.LogWarning("[ChunkLoading] WateredTile asset not assigned!");
                else if (wateredTilemap != null)
                    wateredPositions.Add(wateredTilemap.WorldToCell(new Vector3(tile.WorldX, tile.WorldY, 0)));
            }

            // ── Crop visual ───────────────────────────────────────────────────
            if (tile.HasCrop)
            {
                PlantData plantData   = GetPlantData(tile.Crop.PlantId);
                Sprite    stageSprite = null;
                bool      isFallback  = false;

                if (plantData == null)
                {
                    var fallback = FallbackConfig.Instance?.PlaceholderSprite;
                    if (fallback != null) { stageSprite = fallback; isFallback = true; }
                    else
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"[ChunkLoading] No plant data for '{tile.Crop.PlantId}' at ({tile.WorldX},{tile.WorldY})");
                        goto NextTile;
                    }
                }
                else
                {
                    if (!plantData.isHybrid && tile.Crop.CropStage >= plantData.growthStages.Count)
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"[ChunkLoading] Invalid stage {tile.Crop.CropStage} for {plantData.plantName}");
                        goto NextTile;
                    }
                    stageSprite = PlantCatalogService.Instance?.GetStageSprite(tile.Crop.PlantId, tile.Crop.CropStage);
                    if (stageSprite == null)
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"[ChunkLoading] Null sprite for {plantData.plantName} stage {tile.Crop.CropStage}");
                        goto NextTile;
                    }
                }

                if (_cachedCropPool == null)
                    _cachedCropPool = CropPool.Instance ?? FindAnyObjectByType<CropPool>();

                var pos3 = new Vector3(tile.WorldX, tile.WorldY, 0f);
                GameObject visual;
                if (_cachedCropPool != null)            visual = _cachedCropPool.Get(pos3);
                else if (cropVisualPrefab != null)      visual = Instantiate(cropVisualPrefab, pos3, Quaternion.identity);
                else                                    visual = new GameObject { transform = { position = pos3 } };

                visual.name = isFallback
                    ? $"Crop_Fallback_{tile.Crop.PlantId}_{tile.WorldX}_{tile.WorldY}"
                    : $"Crop_{plantData.plantName}_{tile.WorldX}_{tile.WorldY}";

                SpriteRenderer sr = CropPool.GetSourceRenderer(visual) ?? visual.AddComponent<SpriteRenderer>();
                if (_cachedCropPool == null) sr.sortingLayerName = "WalkInfront";
                ApplyCropSortingOrder(sr, tile.Crop.CropStage);
                sr.sprite = stageSprite;
                cropVisuals.Add(visual);
            }

            // ── Structure visual ──────────────────────────────────────────────
            if (tile.HasStructure)
            {
                if (tile.Structure.CurrentHp <= 0)
                {
                    if (showDebugLogs)
                        Debug.Log($"[ChunkLoading] Skipping destroyed structure '{tile.Structure.StructureId}' at ({tile.WorldX},{tile.WorldY})");
                    goto NextTile;
                }

                if (_cachedStructurePool == null)
                    _cachedStructurePool = FindAnyObjectByType<StructurePool>();

                if (_cachedStructurePool != null)
                {
                    string        structId   = tile.Structure.StructureId;
                    StructureData structData = _cachedStructurePool.GetStructureData(structId);

                    if (structData != null)
                    {
                        var structObj = _cachedStructurePool.Get(structId, new Vector3Int(tile.WorldX, tile.WorldY, 0));
                        structObj.transform.position = new Vector3(tile.WorldX + 0.5f, tile.WorldY + 0.25f, 0f);

                        // Apply sprite with bottom-center pivot for all structures
                        SpriteRenderer sr = structObj.GetComponentInChildren<SpriteRenderer>(true)
                                         ?? structObj.AddComponent<SpriteRenderer>();
                        Sprite itemSprite = ItemCatalogService.Instance?.GetCachedSprite(structId);
                        if (itemSprite != null)
                        {
                            sr.sprite = Sprite.Create(
                                itemSprite.texture,
                                itemSprite.rect,
                                new Vector2(0.5f, 0f),
                                itemSprite.pixelsPerUnit,
                                0,
                                SpriteMeshType.FullRect);
                        }
                        sr.sortingLayerName = "WalkInfront";

                        structObj.SetActive(true);
                        foreach (var col in structObj.GetComponentsInChildren<Collider2D>())
                            col.enabled = true;

                        structObj.GetComponentInChildren<IWorldStructure>(true)
                                 ?.InitializeFromWorld(tile.WorldX, tile.WorldY, structData);

                        structObj.name = $"Structure_{structId}_{tile.WorldX}_{tile.WorldY}";
                        if (!_model.StructureVisuals.ContainsKey(chunkPos))
                            _model.StructureVisuals[chunkPos] = new List<(string, GameObject)>();
                        _model.StructureVisuals[chunkPos].Add((structId, structObj));

                        if (showDebugLogs)
                            Debug.Log($"[ChunkLoading] Spawned structure '{structId}' at ({tile.WorldX},{tile.WorldY})");
                    }
                    else
                    {
                        var fallback = FallbackConfig.Instance?.PlaceholderSprite;
                        if (fallback != null)
                        {
                            var ph = new GameObject($"Structure_Fallback_{tile.Structure.StructureId}_{tile.WorldX}_{tile.WorldY}");
                            ph.transform.position = new Vector3(tile.WorldX + 0.5f, tile.WorldY + 0.25f, 0f);
                            var sr = ph.AddComponent<SpriteRenderer>();
                            sr.sprite = fallback;
                            sr.sortingLayerName = "WalkInfront";
                            if (!_model.StructureVisuals.ContainsKey(chunkPos))
                                _model.StructureVisuals[chunkPos] = new List<(string, GameObject)>();
                            _model.StructureVisuals[chunkPos].Add((tile.Structure.StructureId, ph));
                        }
                        else if (showDebugLogs)
                            Debug.LogWarning($"[ChunkLoading] No structure data for '{tile.Structure.StructureId}' at ({tile.WorldX},{tile.WorldY})");
                    }
                }
            }

            // ── Resource visual ───────────────────────────────────────────────
            if (visualizeResources && tile.HasResource && resourceSpawner != null
                && !string.IsNullOrEmpty(tile.Resource.ResourceId))
            {
                int idx = _presenter.WorldTileToTileIndex(chunk.ChunkX, chunk.ChunkY, tile.WorldX, tile.WorldY);
                if (idx >= 0)
                {
                    resourceSpawner.SpawnResourceVisualLocally(chunk.ChunkX, chunk.ChunkY, idx, tile.Resource.ResourceId);
                    resourceCount++;
                }
            }

            NextTile:
            if (++processedThisFrame >= tilesPerFrame)
            {
                processedThisFrame = 0;
                yield return null;
            }
        }

        // Batch-apply tilemap tiles in a single repaint pass each
        if (tilledTilemap != null && tilledPositions.Count > 0)
        {
            var arr = new TileBase[tilledPositions.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = tilledTile;
            tilledTilemap.SetTiles(tilledPositions.ToArray(), arr);
        }
        if (wateredTilemap != null && wateredPositions.Count > 0)
        {
            var arr = new TileBase[wateredPositions.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = wateredTile;
            wateredTilemap.SetTiles(wateredPositions.ToArray(), arr);
        }

        _model.CropVisuals[chunkPos]  = cropVisuals;
        _model.TilledCells[chunkPos]  = tilledPositions;
        _model.WateredCells[chunkPos] = wateredPositions;

        if (showDebugLogs && (cropVisuals.Count > 0 || tilledPositions.Count > 0 || resourceCount > 0))
            Debug.Log($"[ChunkLoading] Spawned {cropVisuals.Count} crops + {resourceCount} resources + {tilledPositions.Count} tilled tiles for ({chunkPos.x},{chunkPos.y})");
    }

    private IEnumerator StaggeredDailyReload(List<Vector2Int> chunks)
    {
        foreach (var chunkPos in chunks)
        {
            RefreshChunkVisuals(chunkPos);
            yield return null;
        }
        _model.UnloadQueue.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Visual activate / deactivate (called via IChunkLoadingView)
    // ─────────────────────────────────────────────────────────────────────────

    private void ActivateChunkVisualsInternal(Vector2Int chunkPos)
    {
        if (_model.CropVisuals.TryGetValue(chunkPos, out var crops))
            foreach (var go in crops)
                if (go != null) go.SetActive(true);

        if (_model.StructureVisuals.TryGetValue(chunkPos, out var structs))
            foreach (var (_, go) in structs)
                if (go != null) go.SetActive(true);

        Tilemap tilledTilemap  = FindTilemap(tilledTilemapName);
        Tilemap wateredTilemap = FindTilemap(wateredTilemapName);

        if (tilledTilemap != null && _model.TilledCells.TryGetValue(chunkPos, out var tPos) && tPos.Count > 0)
        {
            var arr = new TileBase[tPos.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = tilledTile;
            tilledTilemap.SetTiles(tPos.ToArray(), arr);
        }
        if (wateredTilemap != null && _model.WateredCells.TryGetValue(chunkPos, out var wPos) && wPos.Count > 0)
        {
            var arr = new TileBase[wPos.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = wateredTile;
            wateredTilemap.SetTiles(wPos.ToArray(), arr);
        }

        // Resources have no deactivation API — re-spawn them
        if (visualizeResources && _presenter.TryGetChunkData(chunkPos, out UnifiedChunkData chunk))
        {
            ResourceSpawnerManager rs = ResourceSpawnerManager.Instance ?? FindAnyObjectByType<ResourceSpawnerManager>();
            if (rs != null)
                foreach (var tile in chunk.GetAllTiles())
                {
                    if (!tile.HasResource || string.IsNullOrEmpty(tile.Resource.ResourceId)) continue;
                    int idx = _presenter.WorldTileToTileIndex(chunk.ChunkX, chunk.ChunkY, tile.WorldX, tile.WorldY);
                    if (idx >= 0) rs.SpawnResourceVisualLocally(chunk.ChunkX, chunk.ChunkY, idx, tile.Resource.ResourceId);
                }
        }
    }

    private void DeactivateChunkVisualsInternal(Vector2Int chunkPos)
    {
        if (_model.CropVisuals.TryGetValue(chunkPos, out var crops))
            foreach (var go in crops)
                if (go != null) go.SetActive(false);

        if (_model.StructureVisuals.TryGetValue(chunkPos, out var structs))
            foreach (var (_, go) in structs)
                if (go != null) go.SetActive(false);

        // Tilemap cells must be cleared (shared surface, no per-chunk toggle).
        // Position lists are preserved so ActivateChunkVisualsInternal can repaint instantly.
        if (_model.TilledCells.TryGetValue(chunkPos, out var tPos) && tPos.Count > 0)
            FindTilemap(tilledTilemapName)?.SetTiles(tPos.ToArray(), new TileBase[tPos.Count]);

        if (_model.WateredCells.TryGetValue(chunkPos, out var wPos) && wPos.Count > 0)
            FindTilemap(wateredTilemapName)?.SetTiles(wPos.ToArray(), new TileBase[wPos.Count]);

        ReleaseChunkResources(chunkPos);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public visual API (called by CropManager, ChunkDataSyncManager, etc.)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fully release and re-spawn all visuals for a chunk using current tile data.
    /// Use this when data has changed (planted, harvested, structure placed, etc.).
    /// </summary>
    public void RefreshChunkVisuals(Vector2Int chunkPos)
    {
        if (!_presenter.IsChunkLoaded(chunkPos)) return;

        if (_model.CropVisuals.TryGetValue(chunkPos, out var crops))
        {
            if (_cachedCropPool == null)
                _cachedCropPool = CropPool.Instance ?? FindAnyObjectByType<CropPool>();
            foreach (var go in crops)
            {
                if (go == null) continue;
                if (_cachedCropPool != null) _cachedCropPool.Release(go);
                else Destroy(go);
            }
            _model.CropVisuals.Remove(chunkPos);
        }

        if (_model.TilledCells.TryGetValue(chunkPos, out var tPos))
        {
            FindTilemap(tilledTilemapName)?.SetTiles(tPos.ToArray(), new TileBase[tPos.Count]);
            _model.TilledCells.Remove(chunkPos);
        }
        if (_model.WateredCells.TryGetValue(chunkPos, out var wPos))
        {
            FindTilemap(wateredTilemapName)?.SetTiles(wPos.ToArray(), new TileBase[wPos.Count]);
            _model.WateredCells.Remove(chunkPos);
        }

        ReleaseChunkStructures(chunkPos);
        ReleaseChunkResources(chunkPos);

        if (_presenter.TryGetChunkData(chunkPos, out UnifiedChunkData chunk))
        {
            var e = SpawnChunkVisuals(chunkPos, chunk);
            while (e.MoveNext()) { } // sync-drain (yield nulls are no-ops here)
        }
    }

    /// <summary>
    /// Refresh the visual for a single tile without touching the rest of the chunk.
    /// Prefer this over <see cref="RefreshChunkVisuals"/> for per-tile events (planting, watering, harvesting).
    /// </summary>
    public void UpdateTileVisual(int worldX, int worldY)
    {
        if (WorldDataManager.Instance == null) return;
        Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(new Vector3(worldX, worldY, 0));
        if (!_presenter.IsChunkLoaded(chunkPos)) return;
        if (!_presenter.TryGetChunkData(chunkPos, out UnifiedChunkData chunk)) return;

        // Remove existing crop GO at this tile
        if (_model.CropVisuals.TryGetValue(chunkPos, out var cropList))
        {
            if (_cachedCropPool == null)
                _cachedCropPool = CropPool.Instance ?? FindAnyObjectByType<CropPool>();
            for (int i = cropList.Count - 1; i >= 0; i--)
            {
                var go = cropList[i];
                if (go == null) { cropList.RemoveAt(i); continue; }
                var p = go.transform.position;
                if (Mathf.RoundToInt(p.x) == worldX && Mathf.RoundToInt(p.y) == worldY)
                {
                    if (_cachedCropPool != null) _cachedCropPool.Release(go); else Destroy(go);
                    cropList.RemoveAt(i);
                    break;
                }
            }
        }

        Tilemap tilledTilemap  = FindTilemap(tilledTilemapName);
        Tilemap wateredTilemap = FindTilemap(wateredTilemapName);
        Vector3Int cellPos = tilledTilemap != null
            ? tilledTilemap.WorldToCell(new Vector3(worldX, worldY, 0))
            : new Vector3Int(worldX, worldY, 0);

        tilledTilemap?.SetTile(cellPos, null);
        wateredTilemap?.SetTile(cellPos, null);
        if (_model.TilledCells.TryGetValue(chunkPos, out var tl))  tl.Remove(cellPos);
        if (_model.WateredCells.TryGetValue(chunkPos, out var wl)) wl.Remove(cellPos);

        UnifiedChunkData.TileSlot? slotN = chunk.GetTile(worldX, worldY);
        if (!slotN.HasValue) return;
        var slot = slotN.Value;

        if (slot.IsTilled && tilledTile != null && tilledTilemap != null)
        {
            tilledTilemap.SetTile(cellPos, tilledTile);
            if (!_model.TilledCells.ContainsKey(chunkPos)) _model.TilledCells[chunkPos] = new List<Vector3Int>();
            _model.TilledCells[chunkPos].Add(cellPos);
        }
        if (slot.IsTilled && slot.Crop.IsWatered && wateredTile != null && wateredTilemap != null)
        {
            wateredTilemap.SetTile(cellPos, wateredTile);
            if (!_model.WateredCells.ContainsKey(chunkPos)) _model.WateredCells[chunkPos] = new List<Vector3Int>();
            _model.WateredCells[chunkPos].Add(cellPos);
        }
        if (slot.HasCrop && PlantCatalogService.Instance?.IsReady == true)
        {
            Sprite sp = PlantCatalogService.Instance.GetStageSprite(slot.Crop.PlantId, slot.Crop.CropStage);
            if (sp != null)
            {
                if (_cachedCropPool == null) _cachedCropPool = CropPool.Instance ?? FindAnyObjectByType<CropPool>();
                var pos3 = new Vector3(worldX, worldY, 0f);
                var visual = _cachedCropPool != null  ? _cachedCropPool.Get(pos3)
                           : cropVisualPrefab != null ? Instantiate(cropVisualPrefab, pos3, Quaternion.identity)
                           : new GameObject           { transform = { position = pos3 } };
                visual.name = $"Crop_{slot.Crop.PlantId}_{worldX}_{worldY}";
                SpriteRenderer sr = CropPool.GetSourceRenderer(visual) ?? visual.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                ApplyCropSortingOrder(sr, slot.Crop.CropStage);
                if (!_model.CropVisuals.ContainsKey(chunkPos)) _model.CropVisuals[chunkPos] = new List<GameObject>();
                _model.CropVisuals[chunkPos].Add(visual);
            }
        }
    }

    /// <summary>
    /// Update only the watered-overlay cell for a single tile.
    /// Cheaper than <see cref="UpdateTileVisual"/> when only the water state changed.
    /// </summary>
    public void UpdateWaterOverlay(int worldX, int worldY)
    {
        if (WorldDataManager.Instance == null) return;
        Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(new Vector3(worldX, worldY, 0));
        if (!_presenter.IsChunkLoaded(chunkPos)) return;
        if (!_presenter.TryGetChunkData(chunkPos, out UnifiedChunkData chunk)) return;

        Tilemap wateredTilemap = FindTilemap(wateredTilemapName);
        if (wateredTilemap == null) return;

        Vector3Int cellPos = wateredTilemap.WorldToCell(new Vector3(worldX, worldY, 0));
        UnifiedChunkData.TileSlot? slotN = chunk.GetTile(worldX, worldY);
        bool shouldWater = slotN.HasValue && slotN.Value.IsTilled && slotN.Value.Crop.IsWatered;

        if (shouldWater && wateredTile != null)
        {
            wateredTilemap.SetTile(cellPos, wateredTile);
            if (!_model.WateredCells.ContainsKey(chunkPos)) _model.WateredCells[chunkPos] = new List<Vector3Int>();
            if (!_model.WateredCells[chunkPos].Contains(cellPos))
                _model.WateredCells[chunkPos].Add(cellPos);
        }
        else
        {
            wateredTilemap.SetTile(cellPos, null);
            if (_model.WateredCells.TryGetValue(chunkPos, out var list)) list.Remove(cellPos);
        }
    }

    /// <summary>Remove the watered overlay tile at a world position (called by CropGrowthService on water decay).</summary>
    public void ClearWateredTileAt(Vector3 worldPos)
    {
        Tilemap wateredTilemap = FindTilemap(wateredTilemapName);
        if (wateredTilemap == null) return;
        Vector3Int cellPos  = wateredTilemap.WorldToCell(worldPos);
        wateredTilemap.SetTile(cellPos, null);
        Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
        if (_model.WateredCells.TryGetValue(chunkPos, out var list)) list.Remove(cellPos);
    }

    /// <summary>Returns the structure GO at a given world position (accounts for the +0.5f spawn offset).</summary>
    public GameObject GetStructureVisualAt(Vector3Int worldPos)
    {
        Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(new Vector3(worldPos.x, worldPos.y, 0f));
        if (!_model.StructureVisuals.TryGetValue(chunkPos, out var list)) return null;
        foreach (var (_, go) in list)
        {
            if (go == null) continue;
            float vx = go.transform.position.x, vy = go.transform.position.y;
            if ((Mathf.Approximately(vx, worldPos.x)       && Mathf.Approximately(vy, worldPos.y)) ||
                (Mathf.Approximately(vx, worldPos.x + 0.5f) && Mathf.Approximately(vy, worldPos.y + 0.25f)))
                return go;
        }
        return null;
    }

    /// <summary>Ensures a chunk is visible with up-to-date data. Delegates to Presenter.</summary>
    public void EnsureChunkLoaded(Vector2Int chunkPos)     => _presenter.EnsureChunkLoaded(chunkPos);

    /// <summary>Returns a snapshot of all currently active chunk positions.</summary>
    public List<Vector2Int> GetLoadedChunks()              => _presenter.GetLoadedChunks();

    /// <summary>Returns true if the given chunk is currently active.</summary>
    public bool IsChunkLoaded(Vector2Int chunkPos)         => _presenter.IsChunkLoaded(chunkPos);

    /// <summary>Synchronously unload all chunks then reload around the local player.</summary>
    public void ForceReloadAllChunks()                     => _presenter.ForceReloadAllChunks();

    // ─────────────────────────────────────────────────────────────────────────
    //  Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ReleaseChunkStructures(Vector2Int chunkPos)
    {
        if (!_model.StructureVisuals.ContainsKey(chunkPos)) return;
        if (_cachedStructurePool == null)
            _cachedStructurePool = FindAnyObjectByType<StructurePool>();

        foreach (var (structureId, go) in _model.StructureVisuals[chunkPos])
        {
            if (go == null || !go.activeInHierarchy) continue;
            Vector3Int? pos = null;
            var parts = go.name.Split('_');
            if (parts.Length >= 4
                && int.TryParse(parts[parts.Length - 2], out int px)
                && int.TryParse(parts[parts.Length - 1], out int py))
                pos = new Vector3Int(px, py, 0);
            _cachedStructurePool.Release(structureId, go, pos);
        }
        _model.StructureVisuals.Remove(chunkPos);
    }

    private void ReleaseChunkResources(Vector2Int chunkPos)
    {
        if (!visualizeResources) return;
        ResourceSpawnerManager rs = ResourceSpawnerManager.Instance ?? FindAnyObjectByType<ResourceSpawnerManager>();
        if (rs == null) return;
        if (!_presenter.TryGetChunkData(chunkPos, out UnifiedChunkData chunk)) return;

        int removed = 0;
        foreach (var tile in chunk.GetAllTiles())
        {
            if (!tile.HasResource) continue;
            int idx = _presenter.WorldTileToTileIndex(chunk.ChunkX, chunk.ChunkY, tile.WorldX, tile.WorldY);
            if (idx < 0) continue;
            rs.RemoveResourceVisual(chunk.ChunkX, chunk.ChunkY, idx);
            removed++;
        }
        if (showDebugLogs && removed > 0)
            Debug.Log($"[ChunkLoading] Removed {removed} resource visuals for chunk ({chunkPos.x},{chunkPos.y})");
    }

    private PlantData GetPlantData(string plantId)
    {
        var plant = PlantCatalogService.Instance?.GetPlantData(plantId);
        if (plant == null && showDebugLogs) Debug.LogWarning($"[ChunkLoading] PlantId '{plantId}' not found.");
        return plant;
    }

    /// <summary>Stage 0 (seed/just-planted) renders on Ground layer order 5; all other stages use WalkInfront order 0.</summary>
    private static void ApplyCropSortingOrder(SpriteRenderer sr, int stage)
    {
        if (stage == 0)
        {
            sr.sortingLayerName = "Ground";
            sr.sortingOrder     = 5;
        }
        else
        {
            sr.sortingLayerName = "WalkInfront";
            sr.sortingOrder     = 0;
        }
    }

    private Tilemap FindTilemap(string tilemapName)
    {
        if (_tilemapCache.TryGetValue(tilemapName, out Tilemap cached) && cached != null)
            return cached;
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (tm.gameObject.name == tilemapName)
            {
                _tilemapCache[tilemapName] = tm;
                return tm;
            }
        if (showDebugLogs) Debug.LogWarning($"[ChunkLoading] Tilemap '{tilemapName}' not found!");
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showLoadedChunksGizmos || _model == null) return;
        WorldDataManager mgr = WorldDataManager.Instance;
        if (mgr == null) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        foreach (var cp in _model.LoadedChunks)
        {
            var wp = mgr.ChunkToWorldPosition(cp);
            var sz = new Vector3(mgr.chunkSizeTiles, mgr.chunkSizeTiles, 1);
            Gizmos.DrawWireCube(wp + sz / 2, sz);
        }
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        foreach (var cp in _model.UnloadQueue.Keys)
        {
            var wp = mgr.ChunkToWorldPosition(cp);
            var sz = new Vector3(mgr.chunkSizeTiles, mgr.chunkSizeTiles, 1);
            Gizmos.DrawWireCube(wp + sz / 2, sz);
        }
        Gizmos.color = Color.cyan;
        foreach (var pc in _model.PlayerChunkPositions.Values)
        {
            var wp = mgr.ChunkToWorldPosition(pc);
            Gizmos.DrawSphere(wp + new Vector3(mgr.chunkSizeTiles / 2f, mgr.chunkSizeTiles / 2f, 0), 2f);
        }
    }
}