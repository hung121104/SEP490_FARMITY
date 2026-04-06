using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// Unified service for all structure operations: placement, removal, destruction (damage/HP).
/// Merges the former StructureDestructionService into a single service layer.
/// </summary>
public class StructureService : IStructureService
{
    private readonly ChunkDataSyncManager syncManager;
    private readonly ChunkLoadingManager  loadingManager;
    private readonly IStructureDataProvider structureDataProvider;
    private readonly bool showDebugLogs;

    // Track pending hit requests being processed (Master only)
    private readonly HashSet<Vector3Int> processingHits = new HashSet<Vector3Int>();

    /// <summary>
    /// Static delegate for inventory operations.
    /// Wired by View layer (Composition Root) so Service never depends on View directly.
    /// </summary>
    public static System.Func<string, int, Quality, bool> OnAddItemToInventory;

    public StructureService(ChunkDataSyncManager syncManager,
                            ChunkLoadingManager loadingManager,
                            IStructureDataProvider structureDataProvider,
                            bool showDebugLogs = true)
    {
        this.syncManager            = syncManager;
        this.loadingManager         = loadingManager;
        this.structureDataProvider  = structureDataProvider;
        this.showDebugLogs          = showDebugLogs;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PLACEMENT
    // ══════════════════════════════════════════════════════════════════════

    public bool CanPlaceStructure(Vector3 worldPosition, StructureData data)
    {
        if (data == null) return false;

        int anchorX = Mathf.FloorToInt(worldPosition.x);
        int anchorY = Mathf.FloorToInt(worldPosition.y);

        Vector3 tileWorld = new Vector3(anchorX, anchorY, 0f);

        if (!IsPositionInActiveSection(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) not in active section.");
            return false;
        }

        if (!IsBuildableGround(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) is not buildable ground.");
            return false;
        }

        if (WorldDataManager.Instance.HasCropAtWorldPosition(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) has a crop.");
            return false;
        }

        if (WorldDataManager.Instance.HasStructureAtWorldPosition(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) already has a structure.");
            return false;
        }

        if (WorldDataManager.Instance.IsTilledAtWorldPosition(tileWorld))
        {
            if (showDebugLogs)
                Debug.LogWarning($"[StructureService] Tile ({anchorX},{anchorY}) is tilled soil.");
            return false;
        }

        return true;
    }

    public bool PlaceStructure(Vector3 worldPosition, StructureData data)
    {
        if (!CanPlaceStructure(worldPosition, data))
            return false;

        int anchorX = Mathf.FloorToInt(worldPosition.x);
        int anchorY = Mathf.FloorToInt(worldPosition.y);

        Vector3 tileWorld = new Vector3(anchorX, anchorY, 0f);
        int initialHp = data.MaxHealth;
        bool ok = WorldDataManager.Instance.PlaceStructureAtWorldPosition(tileWorld, data.StructureId, initialHp, (byte)data.StructureLevel);
        if (!ok)
        {
            Debug.LogError($"[StructureService] Failed to write tile ({anchorX},{anchorY}).");
            return false;
        }

        if (showDebugLogs)
            Debug.Log($"[StructureService] ✓ Placed '{data.StructureId}' at ({anchorX},{anchorY}) with HP={initialHp}");

        RefreshAffectedChunks(anchorX, anchorY, 1, 1);

        if (PhotonNetwork.IsConnected && syncManager != null)
            BroadcastStructurePlaced(anchorX, anchorY, data.StructureId, (byte)data.StructureLevel);

        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  REMOVAL
    // ══════════════════════════════════════════════════════════════════════

    public bool RemoveStructure(Vector3 worldPosition, StructureData data)
    {
        if (data == null) return false;

        int anchorX = Mathf.FloorToInt(worldPosition.x);
        int anchorY = Mathf.FloorToInt(worldPosition.y);

        Vector3 tileWorld = new Vector3(anchorX, anchorY, 0f);
        WorldDataManager.Instance.RemoveStructureAtWorldPosition(tileWorld);

        if (showDebugLogs)
            Debug.Log($"[StructureService] ✗ Removed '{data.StructureId}' at ({anchorX},{anchorY})");

        RefreshAffectedChunks(anchorX, anchorY, 1, 1);

        if (PhotonNetwork.IsConnected && syncManager != null)
            BroadcastStructureRemoved(anchorX, anchorY);

        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DESTRUCTION (damage / HP / regen)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Call this from local player input.
    /// Non-master: sends hit request to Master.
    /// Master: processes damage immediately.
    /// </summary>
    public bool RequestHit(Vector3Int pos, int damage, string playerActorId)
    {
        if (!WorldDataManager.Instance.HasStructureAtWorldPosition(pos))
            return false;

        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null || !chunk.TryGetStructure(pos.x, pos.y, out var structureData))
            return false;

        int currentHp = chunk.GetStructureHp(pos.x, pos.y);
        if (currentHp <= 0)
        {
            if (showDebugLogs)
                Debug.Log($"[StructureService] Structure at {pos} already destroyed, ignoring hit");
            return false;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            return ProcessHitInternal(pos, damage, playerActorId);
        }
        else
        {
            syncManager?.RequestStructureHit(pos.x, pos.y, damage, playerActorId);
            syncManager?.BroadcastLocalHitEffect(pos.x, pos.y);

            if (showDebugLogs)
                Debug.Log($"[StructureService] Client: Sent hit request for structure at {pos}");
            return true;
        }
    }

    /// <summary>
    /// Master only: Process a hit request. Called by Presenter when receiving network hit request events.
    /// Sequential processing ensures no race condition.
    /// </summary>
    public bool ProcessHitRequest(Vector3Int pos, int damage, string playerActorId)
    {
        if (!PhotonNetwork.IsMasterClient) return false;

        if (processingHits.Contains(pos))
        {
            if (showDebugLogs)
                Debug.Log($"[StructureService] Master: Structure at {pos} is being processed, delaying hit");
            return false;
        }

        return ProcessHitInternal(pos, damage, playerActorId);
    }

    private bool ProcessHitInternal(Vector3Int pos, int damage, string playerActorId)
    {
        processingHits.Add(pos);

        try
        {
            UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
            if (chunk == null || !chunk.TryGetStructure(pos.x, pos.y, out var structureData))
                return false;

            string structureId = structureData.StructureId;

            StructureData so = structureDataProvider.GetStructureData(structureId);
            if (so == null) return false;

            int currentHp = chunk.GetStructureHp(pos.x, pos.y);
            if (currentHp <= 0)
            {
                if (showDebugLogs)
                    Debug.Log($"[StructureService] Master: Structure at {pos} already destroyed when processing hit");
                return false;
            }

            int newHp = currentHp - damage;
            if (newHp < 0) newHp = 0;

            if (showDebugLogs)
                Debug.Log($"[StructureService] Master: Structure {structureId} at {pos} took {damage} damage from player {playerActorId}. HP: {currentHp} -> {newHp}/{so.MaxHealth}");

            chunk.UpdateStructureHp(pos.x, pos.y, newHp);
            syncManager?.BroadcastStructureHpUpdated(pos.x, pos.y, newHp);

            if (newHp <= 0)
                HandleStructureDestroyed(pos, structureId, playerActorId);

            return true;
        }
        finally
        {
            processingHits.Remove(pos);
        }
    }

    private void HandleStructureDestroyed(Vector3Int pos, string structureId, string lastHitPlayerId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (showDebugLogs)
            Debug.Log($"[StructureService] Master: Structure {structureId} removed at {pos} by player {lastHitPlayerId}");

        ProcessChestContentsDrop(pos.x, pos.y, lastHitPlayerId);
        ProcessStructureItemDrop(pos.x, pos.y, structureId, lastHitPlayerId);

        WorldDataManager.Instance.UnregisterChest((short)pos.x, (short)pos.y);

        syncManager?.BroadcastStructureRemoved(pos.x, pos.y, lastHitPlayerId);
        WorldDataManager.Instance.RemoveStructureAtWorldPosition(pos);
    }

    public bool DealDamage(Vector3Int pos, int damage, out bool isRemoved, out string structureId)
    {
        isRemoved = false;
        structureId = string.Empty;

        string playerId = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
        bool success = RequestHit(pos, damage, playerId);

        if (success && PhotonNetwork.IsMasterClient)
        {
            UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
            if (chunk != null && chunk.TryGetStructure(pos.x, pos.y, out var data))
            {
                structureId = data.StructureId;
                int hp = chunk.GetStructureHp(pos.x, pos.y);
                isRemoved = hp <= 0;
            }
        }

        return success;
    }

    public void RegenerateHP(Vector3Int pos)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null) return;

        if (!chunk.TryGetStructure(pos.x, pos.y, out var structureData)) return;

        StructureData so = structureDataProvider.GetStructureData(structureData.StructureId);
        int maxHp = so?.MaxHealth ?? 3;

        int currentHp = chunk.GetStructureHp(pos.x, pos.y);
        if (currentHp >= maxHp) return;

        if (showDebugLogs)
            Debug.Log($"[StructureService] Master: Regenerating HP for structure at {pos} from {currentHp} to {maxHp}");

        chunk.UpdateStructureHp(pos.x, pos.y, maxHp);
        syncManager?.BroadcastStructureHpUpdated(pos.x, pos.y, maxHp);
    }

    public bool IsStructureAlreadyDestroyed(Vector3Int pos)
    {
        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null) return false;
        if (!chunk.HasStructure(pos.x, pos.y)) return false;

        int currentHp = chunk.GetStructureHp(pos.x, pos.y);
        return currentHp <= 0;
    }

    public bool IsStructureFullHp(Vector3Int pos, int currentHp)
    {
        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null || !chunk.TryGetStructure(pos.x, pos.y, out var structureData))
            return false;

        StructureData so = structureDataProvider.GetStructureData(structureData.StructureId);
        int maxHp = so?.MaxHealth ?? 3;
        return currentHp >= maxHp;
    }

    // ── Item Drop Delegates ──────────────────────────────────────────────

    public static void ProcessStructureItemDrop(int worldX, int worldY, string structureId, string lastHitPlayerId)
    {
        if (string.IsNullOrEmpty(lastHitPlayerId) || string.IsNullOrEmpty(structureId))
            return;

        string localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
        if (localPlayerId != lastHitPlayerId)
            return;

        if (OnAddItemToInventory == null)
        {
            Debug.LogError($"[StructureService] OnAddItemToInventory not wired - cannot add item {structureId}");
            return;
        }

        bool added = OnAddItemToInventory.Invoke(structureId, 1, Quality.Normal);
        Debug.Log($"[StructureService] Added item {structureId} to inventory for last hitter {localPlayerId} - success={added}");
    }

    public static void ProcessChestContentsDrop(int worldX, int worldY, string lastHitPlayerId)
    {
        if (string.IsNullOrEmpty(lastHitPlayerId)) return;

        string localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
        if (localPlayerId != lastHitPlayerId) return;

        var chestModule = WorldDataManager.Instance?.ChestData;
        if (chestModule == null) return;

        short tx = (short)worldX;
        short ty = (short)worldY;

        if (!chestModule.HasChest(tx, ty)) return;

        var slots = new List<ChestSlotEntry>();
        chestModule.GetChestSlots(tx, ty, slots);

        if (slots.Count == 0) return;

        Debug.Log($"[StructureService] Chest at ({tx},{ty}) destroyed — adding {slots.Count} items to inventory");

        if (OnAddItemToInventory == null)
        {
            Debug.LogError("[StructureService] OnAddItemToInventory not wired — chest items lost!");
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (string.IsNullOrEmpty(slot.ItemId) || slot.Quantity <= 0) continue;

            bool added = OnAddItemToInventory.Invoke(slot.ItemId, slot.Quantity, Quality.Normal);
            Debug.Log($"  [{i}] {slot.ItemId} x{slot.Quantity} → inventory success={added}");
        }
    }

    // ── Network Broadcasting ──────────────────────────────────────────────

    public void BroadcastStructurePlaced(int worldX, int worldY, string structureId, byte structureLevel = 1)
    {
        syncManager.BroadcastStructurePlaced(worldX, worldY, structureId, structureLevel);
    }

    public void BroadcastStructureRemoved(int worldX, int worldY)
    {
        syncManager.BroadcastStructureRemoved(worldX, worldY);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    public bool IsPositionInActiveSection(Vector3 worldPosition)
    {
        return WorldDataManager.Instance != null
            && WorldDataManager.Instance.IsPositionInActiveSection(worldPosition);
    }

    public Vector2Int WorldToChunkCoords(Vector3 worldPosition)
    {
        return WorldDataManager.Instance.WorldToChunkCoords(worldPosition);
    }

    public void RefreshChunkVisuals(Vector2Int chunkPosition)
    {
        if (loadingManager != null && loadingManager.IsChunkLoaded(chunkPosition))
            loadingManager.RefreshChunkVisuals(chunkPosition);
    }

    private bool IsBuildableGround(Vector3 worldPosition)
    {
        Tilemap tillable = FindTilemapAtPosition(worldPosition, "TillableTilemap");
        if (tillable == null) return false;

        Vector3Int cell = tillable.WorldToCell(worldPosition);
        return tillable.GetTile(cell) != null;
    }

    private Tilemap FindTilemapAtPosition(Vector3 worldPosition, string tilemapName)
    {
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (Tilemap tm in tilemaps)
        {
            if (tm.gameObject.name != tilemapName) continue;
            Vector3Int cell = tm.WorldToCell(worldPosition);
            Vector3 cellCenter = tm.GetCellCenterWorld(cell);
            if (Vector3.Distance(cellCenter, worldPosition) < 10f)
                return tm;
        }
        return null;
    }

    private void RefreshAffectedChunks(int anchorX, int anchorY, int width, int height)
    {
        if (loadingManager == null) return;

        var visited = new HashSet<Vector2Int>();
        for (int dx = 0; dx < width; dx++)
        {
            for (int dy = 0; dy < height; dy++)
            {
                Vector2Int cp = WorldToChunkCoords(new Vector3(anchorX + dx, anchorY + dy, 0f));
                if (visited.Add(cp))
                    RefreshChunkVisuals(cp);
            }
        }
    }
}
