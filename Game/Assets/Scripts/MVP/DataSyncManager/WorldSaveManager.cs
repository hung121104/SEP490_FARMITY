using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Host-only component that supersedes the legacy SaveWorld.cs auto-save.
///
/// SETUP
/// -----
/// 1. Add to the same scene as ChunkDataSyncManager / WorldDataBootstrapper.
///    It self-deactivates on non-master clients via Awake().
/// 2. In every place a tile is mutated (CropManager, WateringCan logic, etc.),
///    call WorldSaveManager.TryMarkChunkDirty(worldX, worldY, sectionId) after
///    computing the chunkX/chunkY from world coordinates.
/// 3. Disable / remove the old SaveWorld.cs component - this replaces it.
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
    private bool  _pendingPlayerLeftSave = false;

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

    // OnPlayerLeftRoom fires on the master BEFORE Photon destroys the leaving player's
    // owns objects, so BuildPayload() can still read their StaminaView / position.
    // This ensures a non-master's final stamina (and position) are persisted even when
    // the auto-save timer has not yet elapsed.
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (WorldDataBootstrapper.Instance == null || !WorldDataBootstrapper.Instance.IsReady) return;

        if (_isSaving)
        {
            // A save is already in flight — queue a follow-up so the leaving
            // player's final position/stamina are persisted once it completes.
            _pendingPlayerLeftSave = true;
            if (ShowDebugLogs)
                Debug.Log($"[WorldSave] Player '{otherPlayer.NickName}' left during active save — queued follow-up save.");
            return;
        }

        if (ShowDebugLogs)
            Debug.Log($"[WorldSave] Player '{otherPlayer.NickName}' left — triggering immediate save.");

        StartCoroutine(AutoSaveCoroutine());
    }

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
                       || (payload.inventoryDeltas != null && payload.inventoryDeltas.Count > 0)
                       || (payload.chestDeltas != null && payload.chestDeltas.Count > 0)
                       || (payload.deletedChests != null && payload.deletedChests.Count > 0)
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
            // Sync saved stamina values back into PlayerDataManager so that if a joined
            // client re-enters mid-session, TryRestoreFromSavedCharacterData() reads the
            // current in-session values instead of the stale initial API-load values.
            if (payload.characters != null && PlayerDataManager.Instance != null)
            {
                foreach (var charUpdate in payload.characters)
                {
                    if (string.IsNullOrEmpty(charUpdate.accountId)) continue;
                    int idx = PlayerDataManager.Instance.players
                        .FindIndex(p => p.accountId == charUpdate.accountId);
                    if (idx < 0) continue;
                    var pd = PlayerDataManager.Instance.players[idx];
                    if (charUpdate.currentStamina.HasValue)
                        pd.currentStamina = charUpdate.currentStamina.Value;
                    if (charUpdate.viableStamina.HasValue)
                        pd.viableStamina = charUpdate.viableStamina.Value;
                    if (charUpdate.regenBoostMultiplier.HasValue)
                        pd.regenBoostMultiplier = charUpdate.regenBoostMultiplier.Value;
                    if (charUpdate.regenBoostRemaining.HasValue)
                        pd.regenBoostRemaining = charUpdate.regenBoostRemaining.Value;
                    if (charUpdate.toolEfficiencyReduction.HasValue)
                        pd.toolEfficiencyReduction = charUpdate.toolEfficiencyReduction.Value;
                    if (charUpdate.toolEfficiencyRemaining.HasValue)
                        pd.toolEfficiencyRemaining = charUpdate.toolEfficiencyRemaining.Value;
                    PlayerDataManager.Instance.players[idx] = pd;
                }
            }

            ClearPendingUntilledForDirtyChunks();
            _dirtyChunks.Clear();
            WorldDataManager.Instance?.InventoryData?.ClearAllDirtyFlags();
            WorldDataManager.Instance?.ChestData?.ClearAllDirtyFlags();
            WorldDataManager.Instance?.ChestData?.ClearDeletedChests();
            if (ShowDebugLogs) Debug.Log("[WorldSave] Auto-save sent successfully.");
        }
        else
        {
            if (ShowDebugLogs)
                Debug.LogWarning("[WorldSave] Auto-save failed — dirty chunks retained for next attempt.");
        }

        _isSaving = false;

        // A player left while this save was in-flight — run one more save now
        // so their final position/stamina are captured (their GO may be gone by
        // this point, but PlayerDataManager still has their last-synced values).
        if (_pendingPlayerLeftSave)
        {
            _pendingPlayerLeftSave = false;
            StartCoroutine(AutoSaveCoroutine());
        }
    }

    // ──────────────────────────────────────────────────── Quit-flush

    private bool OnWantsToQuit()
    {
#if UNITY_EDITOR
        // In the editor, stopping play mode triggers wantsToQuit but it is not a
        // real application quit.  Attempting an async HTTP flush here races against
        // Unity tearing down play-mode objects (DontDestroyOnLoad singletons become
        // invalid) and always throws.  Skip the flush — data is not lost in the editor.
        return true;
#else
        if (_quitSent) return true;   // flush already done — allow quit

        _quitSent = true;
        _ = FlushAndQuitAsync();
        return false;                  // block OS quit until flush completes
#endif
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

                if (success)
                {
                    ClearPendingUntilledForDirtyChunks();
                    _dirtyChunks.Clear();
                    WorldDataManager.Instance?.InventoryData?.ClearAllDirtyFlags();
                    WorldDataManager.Instance?.ChestData?.ClearAllDirtyFlags();
                    WorldDataManager.Instance?.ChestData?.ClearDeletedChests();
                    Debug.Log("[WorldSave] Quit-flush complete.");
                }
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

    // ──────────────────────────────────────────────────── Helpers

    /// <summary>
    /// Clear PendingUntilledPositions on every dirty chunk after a successful save
    /// so those positions are not re-sent on the next cycle.
    /// Must be called before _dirtyChunks.Clear().
    /// </summary>
    private void ClearPendingUntilledForDirtyChunks()
    {
        var wdm = WorldDataManager.Instance;
        if (wdm == null) return;
        foreach (var (cx, cy, sid) in _dirtyChunks)
        {
            var chunkData = wdm.GetChunk(sid, new UnityEngine.Vector2Int(cx, cy));
            chunkData?.PendingUntilledPositions.Clear();
        }
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
            // Weather state from WorldDataManager
            weatherToday    = wdm?.WeatherToday,
            weatherTomorrow = wdm?.WeatherTomorrow,
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

            var charUpdate = new WorldApi.UpdateWorldRequest.CharacterUpdate
            {
                accountId = accountId,
                positionX = go.transform.position.x,
                positionY = go.transform.position.y,
            };

            // Include appearance configIds if PlayerAppearanceSync is present
            var appearance = go.GetComponent<PlayerAppearanceSync>();
            if (appearance != null)
            {
                var (hair, outfit, hat, tool) = appearance.GetCurrentAppearance();
                // Send empty string (not null) so the server clears the field when the
                // player removes an outfit. NullValueHandling.Include on the DTO ensures
                // empty strings are serialised and the server always receives the update.
                charUpdate.hairConfigId   = hair   ?? string.Empty;
                charUpdate.outfitConfigId = outfit ?? string.Empty;
                charUpdate.hatConfigId    = hat    ?? string.Empty;
                charUpdate.toolConfigId   = tool   ?? string.Empty;
            }

            var stamina = go.GetComponent<StaminaView>();
            if (stamina != null)
            {
                charUpdate.currentStamina = stamina.CurrentStamina;
                charUpdate.viableStamina  = stamina.ViableStamina;
                if (stamina.RegenBoostRemaining > 0f)
                {
                    charUpdate.regenBoostMultiplier = stamina.RegenBoostMultiplier;
                    charUpdate.regenBoostRemaining  = stamina.RegenBoostRemaining;
                }
                if (stamina.ToolEfficiencyRemaining > 0f)
                {
                    charUpdate.toolEfficiencyReduction = stamina.ToolEfficiencyReduction;
                    charUpdate.toolEfficiencyRemaining = stamina.ToolEfficiencyRemaining;
                }
            }

            characters.Add(charUpdate);
        }

        // ── Fallback: include players whose GOs are already destroyed ──────────
        // When a joined client leaves, their GO may be gone before BuildPayload
        // runs (e.g. during a pending follow-up save). RPC_FinalPlayerState on
        // StaminaView writes the client's last-known state into PlayerDataManager
        // so it's available here as a fallback.
        if (PlayerDataManager.Instance != null)
        {
            var coveredIds = new HashSet<string>(characters.Select(c => c.accountId));
            foreach (var pd in PlayerDataManager.Instance.players)
            {
                if (string.IsNullOrEmpty(pd.accountId) || coveredIds.Contains(pd.accountId)) continue;

                var fallback = new WorldApi.UpdateWorldRequest.CharacterUpdate
                {
                    accountId     = pd.accountId,
                    positionX     = pd.positionX,
                    positionY     = pd.positionY,
                    hairConfigId   = pd.hairConfigId   ?? string.Empty,
                    outfitConfigId = pd.outfitConfigId ?? string.Empty,
                    hatConfigId    = pd.hatConfigId    ?? string.Empty,
                    toolConfigId   = pd.toolConfigId   ?? string.Empty,
                    currentStamina = pd.currentStamina,
                    viableStamina  = pd.viableStamina,
                };
                if (pd.regenBoostRemaining > 0f)
                {
                    fallback.regenBoostMultiplier = pd.regenBoostMultiplier;
                    fallback.regenBoostRemaining  = pd.regenBoostRemaining;
                }
                if (pd.toolEfficiencyRemaining > 0f)
                {
                    fallback.toolEfficiencyReduction = pd.toolEfficiencyReduction;
                    fallback.toolEfficiencyRemaining = pd.toolEfficiencyRemaining;
                }
                characters.Add(fallback);
            }
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
                    if (!slot.IsTilled && !slot.HasCrop && !slot.HasResource && !slot.HasStructure) continue;  // skip empty slots

                    // localIndex = localX + localY * 30
                    int localX     = slot.WorldX - chunkX * 30;
                    int localY     = slot.WorldY - chunkY * 30;
                    int localIndex = localX + localY * 30;

                    var td = new WorldApi.TileDataDto();

                    if (slot.HasCrop)
                    {
                        td.type = "crop";
                        // Auto-map all CropTileData public fields. WaterDecayTimer is
                        // excluded via [JsonIgnore] on the struct — no manual updates needed
                        // when fields are added to CropTileData in the future.
                        var cropFields = Newtonsoft.Json.Linq.JObject.FromObject(slot.Crop);
                        td._extra = cropFields.ToObject<Dictionary<string, Newtonsoft.Json.Linq.JToken>>();
                    }
                    else if (slot.HasResource)
                    {
                        td.type = "resource";
                        td._extra = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                        {
                            ["resourceId"] = slot.Resource.ResourceId,
                            ["currentHp"] = slot.Resource.CurrentHp,
                        };
                    }
                    else if (slot.HasStructure)
                    {
                        td.type = "structure";
                        td._extra = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                        {
                            ["structureId"]    = slot.Structure.StructureId,
                            ["structureLevel"] = slot.Structure.StructureLevel,
                            ["currentHp"]      = slot.Structure.CurrentHp,
                        };
                    }
                    else  // tilled only — still need to persist watered state
                    {
                        td.type = "tilled";
                        if (slot.Crop.IsWatered)
                        {
                            var tilledFields = Newtonsoft.Json.Linq.JObject.FromObject(slot.Crop);
                            td._extra = tilledFields.ToObject<Dictionary<string, Newtonsoft.Json.Linq.JToken>>();
                        }
                    }

                    tileDict[localIndex.ToString()] = td;
                }

                // Include tiles that were untilled since the last save.
                // They were removed from the in-memory dictionary so the loop above
                // cannot see them, but the server still has them as "tilled".
                // Sending type="empty" overwrites the stale DB record.
                foreach (var (wx, wy) in chunkData.PendingUntilledPositions)
                {
                    int lx  = wx - chunkX * 30;
                    int ly  = wy - chunkY * 30;
                    int idx = lx + ly * 30;
                    // Only add if not already overridden by the active-tile loop above
                    if (!tileDict.ContainsKey(idx.ToString()))
                        tileDict[idx.ToString()] = new WorldApi.TileDataDto { type = "empty" };
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

        // ── Inventory deltas — only dirty characters ──
        var invModule = wdm?.InventoryData;
        if (invModule != null)
        {
            var dirtyCharIds = invModule.GetDirtyCharacterIds();
            if (ShowDebugLogs)
                Debug.Log($"[WorldSave] Inventory check: {dirtyCharIds.Count} dirty character(s)");

            if (dirtyCharIds.Count > 0)
            {
                var invDeltas = new List<WorldApi.PlayerInventoryDelta>();
                foreach (var charId in dirtyCharIds)
                {
                    var inv = invModule.GetInventory(charId);
                    if (inv == null)
                    {
                        if (ShowDebugLogs) Debug.LogWarning($"[WorldSave] Inventory null for charId='{charId}'");
                        continue;
                    }

                    // Resolve accountId from characterId via PlayerDataManager
                    string accountId = ResolveAccountId(charId);
                    if (string.IsNullOrEmpty(accountId))
                    {
                        if (ShowDebugLogs) Debug.LogWarning($"[WorldSave] Could not resolve accountId for charId='{charId}'");
                        continue;
                    }

                    var slots = new Dictionary<string, WorldApi.InventorySlotDelta>();

                    // Send ALL slots (occupied → $set, empty → $unset on server).
                    // This ensures items removed from the inventory since the last save
                    // are deleted from the DB and do not reappear on the next game load.
                    for (byte slotIdx = 0; slotIdx < inv.MaxSlots; slotIdx++)
                    {
                        if (inv.TryGetSlot(slotIdx, out InventorySlot slot) && !slot.IsEmpty)
                        {
                            slots[slotIdx.ToString()] = new WorldApi.InventorySlotDelta
                            {
                                itemId   = slot.ItemId,
                                quantity = slot.Quantity
                            };
                        }
                        else
                        {
                            // Empty slot: quantity 0 / null itemId tells the server to $unset this key.
                            slots[slotIdx.ToString()] = new WorldApi.InventorySlotDelta
                            {
                                itemId   = null,
                                quantity = 0
                            };
                        }
                    }

                    if (ShowDebugLogs)
                        Debug.Log($"[WorldSave] Inventory delta: charId='{charId}' accountId='{accountId}' slots={slots.Count}");

                    invDeltas.Add(new WorldApi.PlayerInventoryDelta
                    {
                        accountId = accountId,
                        slots     = slots
                    });
                }
                if (invDeltas.Count > 0) request.inventoryDeltas = invDeltas;
            }
        }
        else
        {
            if (ShowDebugLogs) Debug.LogWarning("[WorldSave] InventoryDataModule is null!");
        }

        // ── Chest deltas — only dirty chests ──
        var chestModule = wdm?.ChestData;
        if (chestModule != null)
        {
            var dirtyChestIds = chestModule.GetDirtyChestIds();
            if (ShowDebugLogs)
                Debug.Log($"[WorldSave] Chest check: {dirtyChestIds.Count} dirty chest(s)");

            if (dirtyChestIds.Count > 0)
            {
                var chestDeltas = new List<WorldApi.ChestDelta>();
                var tempSlots = new List<ChestSlotEntry>();

                foreach (var chestId in dirtyChestIds)
                {
                    if (!ChestDataModule.TryParseChestId(chestId, out short tx, out short ty)) continue;
                    if (!chestModule.TryGetHeader(tx, ty, out var header)) continue;

                    var slots = new Dictionary<string, WorldApi.ChestSlotDelta>();

                    // Gather occupied slots for this chest
                    chestModule.GetChestSlots(tx, ty, tempSlots);

                    // Send ALL slots (occupied → $set, empty → $unset on server)
                    for (byte i = 0; i < header.MaxSlots; i++)
                    {
                        ChestSlotEntry found = default;
                        bool hasSlot = false;
                        for (int s = 0; s < tempSlots.Count; s++)
                        {
                            if (tempSlots[s].SlotIndex == i) { found = tempSlots[s]; hasSlot = true; break; }
                        }

                        if (hasSlot && !string.IsNullOrEmpty(found.ItemId))
                        {
                            slots[i.ToString()] = new WorldApi.ChestSlotDelta
                            {
                                itemId   = found.ItemId,
                                quantity = found.Quantity
                            };
                        }
                        else
                        {
                            slots[i.ToString()] = new WorldApi.ChestSlotDelta
                            {
                                itemId   = null,
                                quantity = 0
                            };
                        }
                    }

                    chestDeltas.Add(new WorldApi.ChestDelta
                    {
                        tileX          = tx,
                        tileY          = ty,
                        maxSlots       = header.MaxSlots,
                        structureLevel = header.StructureLevel,
                        slots          = slots
                    });
                }
                if (chestDeltas.Count > 0) request.chestDeltas = chestDeltas;
            }

            // ── Deleted chests — chests destroyed since last save ──
            var deleted = chestModule.GetDeletedChests();
            if (deleted.Count > 0)
            {
                var deletedList = new List<WorldApi.DeletedChest>();
                foreach (var (dtx, dty) in deleted)
                {
                    deletedList.Add(new WorldApi.DeletedChest { tileX = dtx, tileY = dty });
                }
                request.deletedChests = deletedList;
            }
        }

        return request;
    }

    /// <summary>
    /// Map a MongoDB characterId (_id) back to the player's accountId
    /// using PlayerDataManager's player list.
    /// </summary>
    private static string ResolveAccountId(string characterId)
    {
        if (PlayerDataManager.Instance == null) return null;
        var data = PlayerDataManager.Instance.players.Find(p => p._id == characterId);
        return string.IsNullOrEmpty(data.accountId) ? null : data.accountId;
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
