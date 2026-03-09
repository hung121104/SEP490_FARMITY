using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Host-only component that supersedes the legacy UpdateWorld.cs auto-save.
///
/// SETUP
/// -----
/// 1. Add to the same scene as ChunkDataSyncManager / WorldDataBootstrapper.
///    It self-deactivates on non-master clients via Awake().
/// 2. In every place a tile is mutated (CropManager, WateringCan logic, etc.),
///    call WorldSaveManager.TryMarkChunkDirty(worldX, worldY, sectionId) after
///    computing the chunkX/chunkY from world coordinates.
/// 3. Disable / remove the old UpdateWorld.cs component — this replaces it.
///
/// DIRTY TRACKING
/// --------------
///   Entire chunks are marked dirty (not individual tiles).  On each save,
///   ALL non-empty tiles in the dirty chunk are serialised.  This guarantees
///   the backend always receives a complete, accurate snapshot of the chunk.
///
/// AUTO-SAVE FLOW
/// --------------
///   Timer fires every AutoSaveIntervalSeconds → BuildPayload() → PUT /player-data/world.
///   On success: dirty set is cleared.
///   On failure: dirty set is retained for next attempt (no data loss).
///
/// QUIT-FLUSH FLOW
/// ---------------
///   Application.wantsToQuit callback cancels first quit attempt, runs
///   UpdateWorldAsync(), then calls Application.Quit() to allow exit.
/// </summary>
public class WorldSaveManager : MonoBehaviourPunCallbacks
{
    // ──────────────────────────────────────────────────── Inspector

    [Header("Timing")]
    [Tooltip("Auto-save interval in seconds (15–20 recommended).")]
    public float AutoSaveIntervalSeconds = 17f;

    [Header("Debug")]
    public bool ShowDebugLogs = true;

    // ──────────────────────────────────────────────────── Singleton

    public static WorldSaveManager Instance { get; private set; }

    // ──────────────────────────────────────────────────── State

    private readonly HashSet<(int chunkX, int chunkY, int sectionId)> _dirtyChunks
        = new HashSet<(int, int, int)>();

    private float _timer     = 0f;
    private bool  _isSaving  = false;

    /// <summary>True while a save coroutine is running.</summary>
    public bool IsSaving => _isSaving;
    private bool  _quitSent  = false;

    // ──────────────────────────────────────────────────── Unity lifecycle

    private void Awake()
    {
        if (!PhotonNetwork.IsMasterClient) { Destroy(this); return; }

        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()  => Application.wantsToQuit += OnWantsToQuit;
    private void OnDisable() => Application.wantsToQuit -= OnWantsToQuit;

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient || _isSaving) return;

        _timer += Time.unscaledDeltaTime;
        if (_timer >= AutoSaveIntervalSeconds)
        {
            _timer = 0f;
            StartCoroutine(AutoSaveCoroutine());
        }
    }

    // ──────────────────────────────────────────────────── Public API

    /// <summary>
    /// Mark a chunk dirty.  Call this after any tile mutation.
    /// chunkX  = Mathf.FloorToInt(worldX / 30f)
    /// chunkY  = Mathf.FloorToInt(worldY / 30f)
    /// </summary>
    public void MarkChunkDirty(int chunkX, int chunkY, int sectionId)
        => _dirtyChunks.Add((chunkX, chunkY, sectionId));

    /// <summary>Convenience overload — derives chunkX/Y from world tile coords.</summary>
    public void MarkChunkDirtyByWorldPos(int worldX, int worldY, int sectionId)
    {
        int cx = Mathf.FloorToInt(worldX / 30f);
        int cy = Mathf.FloorToInt(worldY / 30f);
        _dirtyChunks.Add((cx, cy, sectionId));
    }

    /// <summary>Static version for use without a direct reference to the singleton.</summary>
    public static void TryMarkChunkDirty(int chunkX, int chunkY, int sectionId)
        => Instance?.MarkChunkDirty(chunkX, chunkY, sectionId);

    /// <summary>Force an immediate save — skips the timer, e.g. on scene transition.</summary>
    public void ForceSave()
    {
        if (_isSaving) return;
        StartCoroutine(AutoSaveCoroutine());
    }

    // ──────────────────────────────────────────────────── Auto-save coroutine

    private IEnumerator AutoSaveCoroutine()
    {
        // Guard: world must be loaded
        if (WorldDataBootstrapper.Instance == null || !WorldDataBootstrapper.Instance.IsReady)
        {
            if (ShowDebugLogs) Debug.Log("[WorldSave] Skipping — world not ready.");
            yield break;
        }

        _isSaving = true;

        WorldApi.UpdateWorldRequest payload = BuildPayload();

        bool hasContent = (payload.characters != null && payload.characters.Count > 0)
                       || (payload.deltas     != null && payload.deltas.Count     > 0)
                       || payload.day != null;

        if (!hasContent)
        {
            if (ShowDebugLogs) Debug.Log("[WorldSave] Auto-save skipped — nothing to send.");
            _isSaving = false;
            yield break;
        }

        if (ShowDebugLogs)
            Debug.Log($"[WorldSave] Auto-save — {payload.deltas?.Count ?? 0} dirty chunk(s).");

        bool saved = false;
        yield return WorldApi.UpdateWorld(
            SessionManager.Instance?.JwtToken,
            payload,
            (success, _) => saved = success
        );

        if (saved)
        {
            _dirtyChunks.Clear();
            if (ShowDebugLogs) Debug.Log("[WorldSave] Auto-save sent successfully.");
        }
        else
        {
            if (ShowDebugLogs)
                Debug.LogWarning("[WorldSave] Auto-save failed — dirty chunks retained for next attempt.");
        }

        _isSaving = false;
    }

    // ──────────────────────────────────────────────────── Quit-flush

    private bool OnWantsToQuit()
    {
        if (_quitSent) return true;   // flush already done — allow quit

        _quitSent = true;
        _ = FlushAndQuitAsync();
        return false;                  // block OS quit until flush completes
    }

    private async Task FlushAndQuitAsync()
    {
        if (ShowDebugLogs) Debug.Log("[WorldSave] Quit detected — flushing final save …");

        try
        {
            if (WorldDataBootstrapper.Instance != null && WorldDataBootstrapper.Instance.IsReady)
            {
                WorldApi.UpdateWorldRequest payload = BuildPayload();
                var (success, _) = await WorldApi.UpdateWorldAsync(
                    SessionManager.Instance?.JwtToken,
                    payload
                );

                if (success) Debug.Log("[WorldSave] Quit-flush complete.");
                else         Debug.LogWarning("[WorldSave] Quit-flush HTTP request failed — quitting anyway.");
            }
            else
            {
                Debug.LogWarning("[WorldSave] Quit-flush skipped — world data not ready.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WorldSave] Quit-flush exception: {ex.Message}");
        }

        // _quitSent is already true; this second call will now return true and proceed
        Application.Quit();
    }

    // ──────────────────────────────────────────────────── Payload builder

    /// <summary>
    /// Assembles the full save payload from live game state.
    /// Used by both auto-save and quit-flush.
    /// </summary>
    private WorldApi.UpdateWorldRequest BuildPayload()
    {
        var wdm = WorldDataManager.Instance;

        var request = new WorldApi.UpdateWorldRequest
        {
            worldId = WorldSelectionManager.Instance?.SelectedWorldId,
            // In-game time from WorldDataManager (mirrors TimeManager state)
            day    = wdm?.Day,
            month  = wdm?.Month,
            year   = wdm?.Year,
            hour   = wdm?.Hour,
            minute = wdm?.Minute,
            gold   = wdm?.Gold,
        };

        // ── Characters — read live positions from PlayerEntity GameObjects ──
        var characters = new List<WorldApi.UpdateWorldRequest.CharacterUpdate>();
        foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            var pv = go.GetComponent<PhotonView>();
            if (pv == null || pv.Owner == null) continue;
            if (!pv.Owner.CustomProperties.TryGetValue("accountId", out object rawId)) continue;
            string accountId = rawId as string;
            if (string.IsNullOrEmpty(accountId)) continue;

            characters.Add(new WorldApi.UpdateWorldRequest.CharacterUpdate
            {
                accountId = accountId,
                positionX = go.transform.position.x,
                positionY = go.transform.position.y,
            });
        }
        if (characters.Count > 0) request.characters = characters;

        // ── Tile deltas — only dirty chunks ──
        if (_dirtyChunks.Count > 0 && wdm != null)
        {
            var deltas = new List<WorldApi.ChunkDeltaDto>();

            foreach (var (chunkX, chunkY, sectionId) in _dirtyChunks)
            {
                var pos       = new UnityEngine.Vector2Int(chunkX, chunkY);
                var chunkData = wdm.GetChunk(sectionId, pos);
                if (chunkData == null) continue;

                var tileDict = new Dictionary<string, WorldApi.TileDataDto>();

                foreach (var slot in chunkData.GetAllTiles())
                {
                    if (!slot.IsTilled && !slot.HasCrop) continue;  // skip empty slots

                    // localIndex = localX + localY * 30
                    int localX     = slot.WorldX - chunkX * 30;
                    int localY     = slot.WorldY - chunkY * 30;
                    int localIndex = localX + localY * 30;

                    var td = new WorldApi.TileDataDto();

                    if (slot.HasCrop)
                    {
                        td.type               = "crop";
                        td.plantId            = slot.Crop.PlantId;
                        td.cropStage          = slot.Crop.CropStage;
                        td.growthTimer        = slot.Crop.GrowthTimer;
                        td.pollenHarvestCount = slot.Crop.PollenHarvestCount;
                        td.isWatered          = slot.Crop.IsWatered;
                        td.isFertilized       = slot.Crop.IsFertilized;
                        td.isPollinated       = slot.Crop.IsPollinated;
                    }
                    else  // tilled only
                    {
                        td.type = "tilled";
                    }

                    tileDict[localIndex.ToString()] = td;
                }

                if (tileDict.Count > 0)
                {
                    deltas.Add(new WorldApi.ChunkDeltaDto
                    {
                        chunkX    = chunkX,
                        chunkY    = chunkY,
                        sectionId = sectionId,
                        tiles     = tileDict,
                    });
                }
            }

            if (deltas.Count > 0) request.deltas = deltas;
        }

        return request;
    }

    // ──────────────────────────────────────────────────── Photon callbacks

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            if (ShowDebugLogs) Debug.Log("[WorldSave] Lost MasterClient role — auto-save disabled.");
            enabled = false;
        }
    }
}
