using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// All chunk lifecycle decisions for the map loading system.
///
/// Plain C# class — no MonoBehaviour, no Unity lifecycle methods.
/// <see cref="ChunkLoadingManager"/> (View) creates an instance, forwards Unity events to it,
/// and executes every visual effect the presenter requests via <see cref="IChunkLoadingView"/>.
///
/// Responsibilities:
///   – Determine which chunks to load / unload based on player position and load radius
///   – Drive the unload timer queue
///   – Decide between the fast-path (SetActive) and first-load (spawn) paths
///   – Expose shared helpers (chunk data lookup, tile index) that the View's visual methods need
/// </summary>
public class ChunkLoadingPresenter
{
    private readonly ChunkLoadingModel _model;
    private readonly IChunkLoadingView _view;

    // ── Config — set from the View's Inspector fields immediately after construction ────
    public int   LoadRadius       { get; set; } = 1;
    public float UpdateInterval   { get; set; } = 1f;
    public float UnloadDelay      { get; set; } = 5f;
    public bool  VisualizeCrops   { get; set; } = true;
    public bool  VisualizeResources { get; set; } = true;
    public bool  ShowDebugLogs    { get; set; } = true;
    public bool  EnableDailyReload { get; set; } = true;

    // ─────────────────────────────────────────────────────────────────────────
    public ChunkLoadingPresenter(ChunkLoadingModel model, IChunkLoadingView view)
    {
        _model = model;
        _view  = view;
    }

    // ── Frame tick — called by ChunkLoadingManager.Update ────────────────────
    /// <summary>
    /// Drive periodic position checks and drain the unload timer queue.
    /// Must be called every frame while the game is running.
    /// </summary>
    public void Tick(float currentTime)
    {
        if (_model.LocalPlayerTransform == null) return;

        if (currentTime >= _model.NextUpdateTime)
        {
            UpdatePlayerChunkPosition();
            _model.NextUpdateTime = currentTime + UpdateInterval;
        }

        if (_model.UnloadQueue.Count > 0)
        {
            _model.UnloadBuffer.Clear();
            foreach (var kvp in _model.UnloadQueue)
                if (currentTime >= kvp.Value)
                    _model.UnloadBuffer.Add(kvp.Key);

            foreach (var chunkPos in _model.UnloadBuffer)
            {
                UnloadChunk(chunkPos);
                _model.UnloadQueue.Remove(chunkPos);
            }
        }
    }

    // ── Player bootstrapping ──────────────────────────────────────────────────
    /// <summary>
    /// Called by the FindLocalPlayer coroutine once all services are ready and the
    /// local player Transform has been resolved via <see cref="PlayerRegistry"/>.
    /// Triggers the very first chunk load around the player.
    /// </summary>
    public void OnLocalPlayerRegistered(Transform playerTransform, float currentTime)
    {
        _model.LocalPlayerTransform = playerTransform;
        _model.NextUpdateTime       = currentTime;
        UpdatePlayerChunkPosition();
    }

    // ── Photon / room events (dispatched from View) ───────────────────────────
    /// <summary>Handle a remote player leaving the room.</summary>
    public void OnPlayerLeft(int actorNumber)
    {
        if (!_model.PlayerChunkPositions.ContainsKey(actorNumber)) return;
        _model.PlayerChunkPositions.Remove(actorNumber);

        if (_model.LocalPlayerTransform != null)
            UpdateLoadedChunks(WorldDataManager.Instance.WorldToChunkCoords(_model.LocalPlayerTransform.position));
    }

    // ── Daily reload ──────────────────────────────────────────────────────────
    /// <summary>Snapshot the currently loaded chunks and ask the View to stagger-refresh them.</summary>
    public void OnDayChanged()
    {
        if (!EnableDailyReload) return;
        if (ShowDebugLogs)
            Debug.Log("[ChunkLoading] New day — starting staggered visual reload");
        _view.StartDailyReload(new List<Vector2Int>(_model.LoadedChunks));
    }

    // ── Public API (thin delegations from ChunkLoadingManager public methods) ─
    public bool             IsChunkLoaded(Vector2Int chunkPos) => _model.LoadedChunks.Contains(chunkPos);
    public List<Vector2Int> GetLoadedChunks()                  => new List<Vector2Int>(_model.LoadedChunks);

    /// <summary>
    /// If the chunk is already loaded, request a full visual rebuild; otherwise trigger a fresh load.
    /// Called when external code needs to guarantee a chunk is visible with up-to-date data.
    /// </summary>
    public void EnsureChunkLoaded(Vector2Int chunkPos)
    {
        if (_model.LoadedChunks.Contains(chunkPos))
        {
            _view.RebuildChunkVisuals(chunkPos);
            return;
        }
        LoadChunk(chunkPos);
    }

    /// <summary>Synchronously unload all chunks, clear the queue, then reload around the local player.</summary>
    public void ForceReloadAllChunks()
    {
        if (ShowDebugLogs)
            Debug.Log($"[ChunkLoading] Force reloading {_model.LoadedChunks.Count} chunk(s)");

        var snapshot = new List<Vector2Int>(_model.LoadedChunks);
        foreach (var chunkPos in snapshot)
            UnloadChunk(chunkPos);

        _model.UnloadQueue.Clear();

        if (_model.LocalPlayerTransform != null)
            UpdateLoadedChunks(WorldDataManager.Instance.WorldToChunkCoords(_model.LocalPlayerTransform.position));
    }

    // ── Shared helpers used by the View's visual methods ─────────────────────
    /// <summary>
    /// Find the <see cref="UnifiedChunkData"/> for <paramref name="chunkPos"/> by searching all active section configs.
    /// </summary>
    public bool TryGetChunkData(Vector2Int chunkPos, out UnifiedChunkData chunk)
    {
        chunk = null;
        if (WorldDataManager.Instance == null) return false;
        foreach (var config in WorldDataManager.Instance.sectionConfigs)
        {
            if (!config.IsActive || !config.ContainsChunk(chunkPos)) continue;
            chunk = WorldDataManager.Instance.GetChunk(config.SectionId, chunkPos);
            return chunk != null;
        }
        return false;
    }

    /// <summary>
    /// Convert a world tile coordinate to the local flat index within its chunk
    /// (used by ResourceSpawnerManager calls). Returns -1 when the tile is outside the chunk.
    /// </summary>
    public int WorldTileToTileIndex(int chunkX, int chunkY, int worldX, int worldY)
    {
        int size = Mathf.Max(1, WorldDataManager.Instance != null ? WorldDataManager.Instance.chunkSizeTiles : 30);
        int lx   = worldX - chunkX * size;
        int ly   = worldY - chunkY * size;
        if (lx < 0 || ly < 0 || lx >= size || ly >= size) return -1;
        return ly * size + lx;
    }

    // ── Private logic ─────────────────────────────────────────────────────────
    private void UpdatePlayerChunkPosition()
    {
        if (_model.LocalPlayerTransform == null) return;

        Vector2Int currentChunk = WorldDataManager.Instance.WorldToChunkCoords(_model.LocalPlayerTransform.position);
        int actorNumber         = PhotonNetwork.LocalPlayer.ActorNumber;

        if (_model.PlayerChunkPositions.TryGetValue(actorNumber, out Vector2Int prev) && prev == currentChunk)
            return;

        _model.PlayerChunkPositions[actorNumber] = currentChunk;
        if (ShowDebugLogs)
            Debug.Log($"[ChunkLoading] Player moved to chunk ({currentChunk.x}, {currentChunk.y})");

        UpdateLoadedChunks(currentChunk);
    }

    private void UpdateLoadedChunks(Vector2Int centerChunk)
    {
        // Build the desired chunk set for this radius
        var desired = new HashSet<Vector2Int>();
        for (int x = -LoadRadius; x <= LoadRadius; x++)
            for (int y = -LoadRadius; y <= LoadRadius; y++)
                desired.Add(new Vector2Int(centerChunk.x + x, centerChunk.y + y));

        // Load missing; cancel pending unloads for chunks that are now needed again
        foreach (var chunkPos in desired)
        {
            if (!_model.LoadedChunks.Contains(chunkPos))
                LoadChunk(chunkPos);
            _model.UnloadQueue.Remove(chunkPos);
        }

        // Schedule unload for out-of-range loaded chunks
        float unloadAt = Time.time + UnloadDelay;
        foreach (var loaded in new List<Vector2Int>(_model.LoadedChunks))
        {
            if (!desired.Contains(loaded) && !_model.UnloadQueue.ContainsKey(loaded))
            {
                _model.UnloadQueue[loaded] = unloadAt;
                if (ShowDebugLogs)
                    Debug.Log($"[ChunkLoading] Marking chunk ({loaded.x}, {loaded.y}) for unload");
            }
        }
    }

    private void LoadChunk(Vector2Int chunkPos)
    {
        if (!TryGetChunkData(chunkPos, out UnifiedChunkData chunk)) return;

        _model.LoadedChunks.Add(chunkPos);
        chunk.IsLoaded = true;

        // Fast path — visuals were built on a previous load; just re-show them
        if (_model.CropVisuals.ContainsKey(chunkPos))
        {
            if (ShowDebugLogs)
                Debug.Log($"[ChunkLoading] Re-activating chunk ({chunkPos.x}, {chunkPos.y})");
            _view.ActivateChunkVisuals(chunkPos);
            _view.NotifyChunkLoaded(chunkPos);
            return;
        }

        if (ShowDebugLogs)
            Debug.Log($"[ChunkLoading] Loading chunk ({chunkPos.x}, {chunkPos.y}) — {chunk.GetCropCount()} crops / {chunk.GetResourceCount()} resources");

        if (VisualizeCrops || VisualizeResources)
        {
            // Async path — View fires NotifyChunkLoaded at end of spawn coroutine
            _view.SpawnChunkVisualsAsync(chunkPos, chunk);
            return;
        }

        _view.NotifyChunkLoaded(chunkPos);
    }

    private void UnloadChunk(Vector2Int chunkPos)
    {
        if (!_model.LoadedChunks.Contains(chunkPos)) return;

        _view.NotifyChunkUnloaded(chunkPos);
        _model.LoadedChunks.Remove(chunkPos);
        _view.CancelSpawnCoroutine(chunkPos);
        _view.DeactivateChunkVisuals(chunkPos);

        if (ShowDebugLogs)
            Debug.Log($"[ChunkLoading] Deactivating chunk ({chunkPos.x}, {chunkPos.y})");
    }
}
