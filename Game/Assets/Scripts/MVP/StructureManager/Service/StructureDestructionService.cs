using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class StructureDestructionService : IStructureDestructionService
{
    private readonly bool showDebugLogs;
    private readonly IStructureDataProvider structureDataProvider;
    private readonly ChunkDataSyncManager syncManager;

    // Track pending hit requests being processed (Master only)
    private readonly HashSet<Vector3Int> processingHits = new HashSet<Vector3Int>();

    /// <summary>
    /// Static delegate for inventory operations.
    /// Wired by View layer (Composition Root) so Service never depends on View directly.
    /// </summary>
    public static System.Func<string, int, Quality, bool> OnAddItemToInventory;

    public StructureDestructionService(IStructureDataProvider dataProvider, ChunkDataSyncManager syncManager, bool showDebugLogs = true)
    {
        this.structureDataProvider = dataProvider;
        this.syncManager = syncManager;
        this.showDebugLogs = showDebugLogs;
    }

    /// <summary>
    /// Processes item drop when a structure is destroyed.
    /// Called by both Master (directly) and Clients (via network event from ChunkDataSyncManager).
    /// </summary>
    public static void ProcessStructureItemDrop(int worldX, int worldY, string structureId, string lastHitPlayerId)
    {
        if (string.IsNullOrEmpty(lastHitPlayerId) || string.IsNullOrEmpty(structureId))
            return;

        string localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
        if (localPlayerId != lastHitPlayerId)
            return; // Not the last hitter, don't add item

        // Use delegate wired by View layer — Service never depends on View directly
        if (OnAddItemToInventory == null)
        {
            Debug.LogError($"[StructureDestructionService] OnAddItemToInventory not wired - cannot add item {structureId}");
            return;
        }

        bool added = OnAddItemToInventory.Invoke(structureId, 1, Quality.Normal);

        Debug.Log($"[StructureDestructionService] Added item {structureId} to inventory for last hitter {localPlayerId} - success={added}");
    }

    /// <summary>
    /// Call this from local player input. 
    /// Non-master: sends hit request to Master.
    /// Master: processes damage immediately.
    /// </summary>
    public bool RequestHit(Vector3Int pos, int damage, string playerActorId)
    {
        // Check if there is actually a structure here
        if (!WorldDataManager.Instance.HasStructureAtWorldPosition(pos))
        {
            return false;
        }

        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null || !chunk.TryGetStructure(pos.x, pos.y, out var structureData))
        {
            return false;
        }

        // Check if already destroyed
        int currentHp = chunk.GetStructureHp(pos.x, pos.y);
        if (currentHp <= 0)
        {
            if (showDebugLogs)
                Debug.Log($"[StructureDestructionService] Structure at {pos} already destroyed, ignoring hit");
            return false;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // Master processes immediately
            return ProcessHitInternal(pos, damage, playerActorId);
        }
        else
        {
            // Non-master sends request to Master
            syncManager?.RequestStructureHit(pos.x, pos.y, damage, playerActorId);
            
            // Play local predictive effect
            syncManager?.BroadcastLocalHitEffect(pos.x, pos.y);
            
            if (showDebugLogs)
                Debug.Log($"[StructureDestructionService] Client: Sent hit request for structure at {pos}");
            return true;
        }
    }

    /// <summary>
    /// Master only: Process a hit request. This is called by ChunkDataSyncManager when receiving hit request.
    /// Sequential processing ensures no race condition.
    /// </summary>
    public bool ProcessHitRequest(Vector3Int pos, int damage, string playerActorId)
    {
        if (!PhotonNetwork.IsMasterClient) return false;

        // Prevent concurrent processing of same structure
        if (processingHits.Contains(pos))
        {
            // Structure is being processed, queue or retry
            if (showDebugLogs)
                Debug.Log($"[StructureDestructionService] Master: Structure at {pos} is being processed, delaying hit");
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
            {
                return false;
            }

            string structureId = structureData.StructureId;
            StructureData so = structureDataProvider.GetStructureData(structureId);
            if (so == null) return false;

            // Get current HP from chunk
            int currentHp = chunk.GetStructureHp(pos.x, pos.y);
            if (currentHp == 0)
            {
                currentHp = so.MaxHealth;
            }

            // Check if already destroyed (race condition protection)
            if (currentHp <= 0)
            {
                if (showDebugLogs)
                    Debug.Log($"[StructureDestructionService] Master: Structure at {pos} already destroyed when processing hit");
                return false;
            }

            // Apply damage
            int newHp = currentHp - damage;
            if (newHp < 0) newHp = 0;

            if (showDebugLogs)
            {
                Debug.Log($"[StructureDestructionService] Master: Structure {structureId} at {pos} took {damage} damage from player {playerActorId}. HP: {currentHp} -> {newHp}/{so.MaxHealth}");
            }

            // Update HP in chunk data (Master authoritative)
            chunk.UpdateStructureHp(pos.x, pos.y, newHp);

            // Sync HP to all clients
            syncManager?.BroadcastStructureHpUpdated(pos.x, pos.y, newHp);

            // If removed, broadcast remove event
            if (newHp <= 0)
            {
                HandleStructureRemoved(pos, structureId, playerActorId);
            }

            return true;
        }
        finally
        {
            processingHits.Remove(pos);
        }
    }

    private void HandleStructureRemoved(Vector3Int pos, string structureId, string lastHitPlayerId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (showDebugLogs)
            Debug.Log($"[StructureDestructionService] Master: Structure {structureId} removed at {pos} by player {lastHitPlayerId}");

        // Handle item drop for last hitter using local static method
        ProcessStructureItemDrop(pos.x, pos.y, structureId, lastHitPlayerId);

        // Broadcast remove using existing STRUCTURE_REMOVED_EVENT (91) with last hitter info
        syncManager?.BroadcastStructureRemoved(pos.x, pos.y, lastHitPlayerId);

        // Remove from world data
        WorldDataManager.Instance.RemoveStructureAtWorldPosition(pos);
    }

    public bool DealDamage(Vector3Int pos, int damage, out bool isRemoved, out string structureId)
    {
        // Legacy method - redirect to new system
        isRemoved = false;
        structureId = string.Empty;
        
        string playerId = PhotonNetwork.LocalPlayer.ActorNumber.ToString();
        bool success = RequestHit(pos, damage, playerId);
        
        if (success && PhotonNetwork.IsMasterClient)
        {
            // For Master, we can get the result immediately
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
        if (!PhotonNetwork.IsMasterClient) return; // Only Master regenerates

        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null) return;

        // Check if structure still exists and HP is 0 (destroyed state)
        if (!chunk.TryGetStructure(pos.x, pos.y, out var structureData)) return;

        int currentHp = chunk.GetStructureHp(pos.x, pos.y);
        if (currentHp > 0) return; // Already has HP, no need to regenerate

        StructureData so = structureDataProvider.GetStructureData(structureData.StructureId);
        int maxHp = so?.MaxHealth ?? 3;

        if (showDebugLogs)
            Debug.Log($"[StructureDestructionService] Master: Regenerating HP for structure at {pos} to {maxHp}");

        // Update chunk data
        chunk.UpdateStructureHp(pos.x, pos.y, maxHp);

        // Sync to all clients
        syncManager?.BroadcastStructureHpUpdated(pos.x, pos.y, maxHp);
    }

    // ── Query ──────────────────────────────────────────────────────────────

    public bool IsStructureAlreadyDestroyed(Vector3Int pos)
    {
        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(pos);
        if (chunk == null) return false;
        if (!chunk.HasStructure(pos.x, pos.y)) return false;

        int currentHp = chunk.GetStructureHp(pos.x, pos.y);
        return currentHp <= 0;
    }
}
