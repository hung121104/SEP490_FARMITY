using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;

/// <summary>
/// Manages synchronization of chunk data across network
/// Handles late-join player synchronization
/// </summary>
public class ChunkDataSyncManager : MonoBehaviourPunCallbacks
{
    [Header("Sync Settings")]
    [Tooltip("Maximum chunks to sync per batch (to avoid packet size limits)")]
    public int chunksPerBatch = 10;
    
    [Tooltip("Delay between batches in seconds")]
    public float batchDelay = 0.5f;
    
    [Tooltip("Enable debug logging")]
    public bool showDebugLogs = true;
    
    // Cached references
    private ChunkLoadingManager chunkLoadingManager;
    
    // Photon event codes
    private const byte REQUEST_WORLD_SYNC_EVENT = 110;
    private const byte WORLD_SYNC_BATCH_EVENT = 111;
    private const byte WORLD_SYNC_COMPLETE_EVENT = 112;
    private const byte CROP_PLANTED_EVENT = 113;
    private const byte CROP_REMOVED_EVENT = 114;
    private const byte CROP_STAGE_UPDATED_EVENT = 115;
    private const byte TILE_TILLED_EVENT = 116;
    private const byte TILE_UNTILLED_EVENT = 117;
    private const byte POLLEN_HARVESTED_EVENT  = 118;
    private const byte CROP_CROSSBRED_EVENT     = 119;
    
    private bool isSyncing = false;
    private bool hasSyncedThisSession = false;
    
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }
    
    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }
    
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[ChunkSync] Not connected to Photon network");
            return;
        }
        
        // Cache ChunkLoadingManager reference
        chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
        
        // Wait a moment for WorldDataManager to initialize
        Invoke(nameof(RequestWorldSync), 1f);
    }
    
    /// <summary>
    /// Request world sync from master client (for late joiners)
    /// </summary>
    private void RequestWorldSync()
    {
        if (hasSyncedThisSession)
        {
            if (showDebugLogs)
                Debug.Log("[ChunkSync] Already synced this session, skipping request");
            return;
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            if (showDebugLogs)
                Debug.Log("[ChunkSync] Is master client, no need to request sync");
            hasSyncedThisSession = true;
            return;
        }
        
        if (!WorldDataManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[ChunkSync] WorldDataManager not initialized yet");
            Invoke(nameof(RequestWorldSync), 1f);
            return;
        }
        
        if (showDebugLogs)
            Debug.Log("[ChunkSync] Requesting world sync from master client...");
        
        // Send request to master client
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.MasterClient
        };
        
        PhotonNetwork.RaiseEvent(
            REQUEST_WORLD_SYNC_EVENT,
            PhotonNetwork.LocalPlayer.ActorNumber,
            options,
            SendOptions.SendReliable
        );
    }
    
    /// <summary>
    /// Handle Photon events
    /// </summary>
    private void OnPhotonEvent(EventData photonEvent)
    {
        byte eventCode = photonEvent.Code;
        
        switch (eventCode)
        {
            case REQUEST_WORLD_SYNC_EVENT:
                if (PhotonNetwork.IsMasterClient)
                {
                    int requestingPlayer = (int)photonEvent.CustomData;
                    HandleWorldSyncRequest(requestingPlayer);
                }
                break;
                
            case WORLD_SYNC_BATCH_EVENT:
                if (!PhotonNetwork.IsMasterClient)
                {
                    HandleWorldSyncBatch(photonEvent.CustomData);
                }
                break;
                
            case WORLD_SYNC_COMPLETE_EVENT:
                if (!PhotonNetwork.IsMasterClient)
                {
                    HandleWorldSyncComplete();
                }
                break;
                
            case CROP_PLANTED_EVENT:
                HandleCropPlanted(photonEvent.CustomData);
                break;
                
            case CROP_REMOVED_EVENT:
                HandleCropRemoved(photonEvent.CustomData);
                break;
                
            case CROP_STAGE_UPDATED_EVENT:
                HandleCropStageUpdated(photonEvent.CustomData);
                break;
                
            case TILE_TILLED_EVENT:
                HandleTileTilled(photonEvent.CustomData);
                break;

            case TILE_UNTILLED_EVENT:
                HandleTileUntilled(photonEvent.CustomData);
                break;

            case POLLEN_HARVESTED_EVENT:
                HandlePollenHarvested(photonEvent.CustomData);
                break;

            case CROP_CROSSBRED_EVENT:
                HandleCropCrossbred(photonEvent.CustomData);
                break;
        }
    }
    
    /// <summary>
    /// Master client handles sync request from late joiner
    /// </summary>
    private void HandleWorldSyncRequest(int requestingPlayerActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Master received sync request from player {requestingPlayerActorNumber}");
        
        StartCoroutine(SendWorldDataToPlayer(requestingPlayerActorNumber));
    }
    
    /// <summary>
    /// Send world data in batches to avoid packet size limits
    /// </summary>
    private System.Collections.IEnumerator SendWorldDataToPlayer(int targetPlayerActorNumber)
    {
        if (isSyncing)
        {
            Debug.LogWarning("[ChunkSync] Already syncing, ignoring new request");
            yield break;
        }
        
        isSyncing = true;
        
        WorldDataManager manager = WorldDataManager.Instance;
        List<ChunkSyncData> allChunkData = new List<ChunkSyncData>();
        
        // Collect all chunks with crops or tilled tiles
        foreach (var config in manager.sectionConfigs)
        {
            if (!config.IsActive) continue;
            
            var section = manager.GetSection(config.SectionId);
            if (section == null) continue;
            
            foreach (var chunkPair in section)
            {
                UnifiedChunkData chunk = chunkPair.Value;
                if (chunk.GetCropCount() == 0 && chunk.GetTilledCount() == 0) continue; // Skip empty chunks

                var allSlots = chunk.GetAllTiles();
                var entries  = new SyncTileEntry[allSlots.Count];
                for (int ei = 0; ei < allSlots.Count; ei++)
                {
                    var slot = allSlots[ei];
                    entries[ei] = new SyncTileEntry
                    {
                        WorldX   = slot.WorldX,
                        WorldY   = slot.WorldY,
                        IsTilled = slot.IsTilled,
                        HasCrop  = slot.HasCrop,
                        Crop     = slot.Crop
                    };
                }
                ChunkSyncData syncData = new ChunkSyncData
                {
                    ChunkX    = chunk.ChunkX,
                    ChunkY    = chunk.ChunkY,
                    SectionId = chunk.SectionId,
                    Tiles     = entries
                };

                allChunkData.Add(syncData);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Sending {allChunkData.Count} chunks with data to player {targetPlayerActorNumber}");
        
        // Send in batches
        int totalBatches = Mathf.CeilToInt((float)allChunkData.Count / chunksPerBatch);
        
        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            int startIndex = batchIndex * chunksPerBatch;
            int count = Mathf.Min(chunksPerBatch, allChunkData.Count - startIndex);
            
            ChunkSyncData[] batch = new ChunkSyncData[count];
            for (int i = 0; i < count; i++)
            {
                batch[i] = allChunkData[startIndex + i];
            }
            
            // Send batch to specific player
            RaiseEventOptions options = new RaiseEventOptions
            {
                TargetActors = new int[] { targetPlayerActorNumber }
            };
            
            object[] data = new object[] { batchIndex, totalBatches, SerializeBatch(batch) };
            
            PhotonNetwork.RaiseEvent(
                WORLD_SYNC_BATCH_EVENT,
                data,
                options,
                SendOptions.SendReliable
            );
            
            if (showDebugLogs)
                Debug.Log($"[ChunkSync] Sent batch {batchIndex + 1}/{totalBatches} ({count} chunks)");
            
            yield return new WaitForSeconds(batchDelay);
        }
        
        // Send completion event
        RaiseEventOptions completeOptions = new RaiseEventOptions
        {
            TargetActors = new int[] { targetPlayerActorNumber }
        };
        
        PhotonNetwork.RaiseEvent(
            WORLD_SYNC_COMPLETE_EVENT,
            allChunkData.Count,
            completeOptions,
            SendOptions.SendReliable
        );
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] World sync complete for player {targetPlayerActorNumber}");
        
        isSyncing = false;
    }
    
    /// <summary>
    /// Handle incoming sync batch
    /// </summary>
    private void HandleWorldSyncBatch(object data)
    {
        object[] dataArray = (object[])data;
        int batchIndex = (int)dataArray[0];
        int totalBatches = (int)dataArray[1];
        byte[] serializedBatch = (byte[])dataArray[2];
        
        ChunkSyncData[] batch = DeserializeBatch(serializedBatch);
        
        WorldDataManager manager = WorldDataManager.Instance;
        
        foreach (var chunkData in batch)
        {
            Vector2Int chunkPos = new Vector2Int(chunkData.ChunkX, chunkData.ChunkY);
            UnifiedChunkData chunk = manager.GetChunk(chunkData.SectionId, chunkPos);
            
            if (chunk == null)
            {
                Debug.LogWarning($"[ChunkSync] Chunk ({chunkData.ChunkX}, {chunkData.ChunkY}) not found in section {chunkData.SectionId}");
                continue;
            }
            
            // Clear existing data and load synced tiles (tilled and crops)
            chunk.Clear();

            foreach (var entry in chunkData.Tiles)
            {
                if (entry.IsTilled)
                    chunk.TillTile(entry.WorldX, entry.WorldY);

                if (entry.HasCrop)
                {
                    chunk.PlantCrop(entry.Crop.PlantId, entry.WorldX, entry.WorldY);
                    if (entry.Crop.CropStage > 0)
                        chunk.UpdateCropStage(entry.WorldX, entry.WorldY, entry.Crop.CropStage);
                    // Restore pollen count (UpdateCropStage resets it, so do this after)
                    for (byte p = 0; p < entry.Crop.PollenHarvestCount; p++)
                        chunk.IncrementPollenHarvestCount(entry.WorldX, entry.WorldY);
                }
            }

                // Ensure visuals are spawned for this chunk on the client
                if (chunkLoadingManager != null)
                {
                    chunkLoadingManager.EnsureChunkLoaded(chunkPos);
                    // Refresh visuals to apply tilled tiles and crops
                    chunkLoadingManager.RefreshChunkVisuals(chunkPos);
                }
        }
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received batch {batchIndex + 1}/{totalBatches} ({batch.Length} chunks)");
    }
    
    /// <summary>
    /// Handle sync complete
    /// </summary>
    private void HandleWorldSyncComplete()
    {
        int totalCrops = (int)WorldDataManager.Instance.GetStats().TotalCrops;
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] ✓ World sync complete! Loaded {totalCrops} crops");
        
        hasSyncedThisSession = true;
        
        // Log stats
        WorldDataManager.Instance.LogStats();
    }
    
    /// <summary>
    /// Broadcast crop planted event to all other players
    /// </summary>
    public void BroadcastCropPlanted(int worldX, int worldY, string plantId)
    {
        if (!PhotonNetwork.IsConnected) return;

        object[] data = new object[] { worldX, worldY, plantId };
        
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others
        };
        
        PhotonNetwork.RaiseEvent(
            CROP_PLANTED_EVENT,
            data,
            options,
            SendOptions.SendReliable
        );
    }
    
    /// <summary>
    /// Handle crop planted event from other player
    /// </summary>
    private void HandleCropPlanted(object data)
    {
        object[] dataArray = (object[])data;
        int worldX    = (int)dataArray[0];
        int worldY    = (int)dataArray[1];
        string plantId = (string)dataArray[2];

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.PlantCropAtWorldPosition(worldPos, plantId);

        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received crop planted: '{plantId}' at ({worldX}, {worldY})");
        
        // Refresh visuals for this chunk
        if (chunkLoadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
            if (chunkLoadingManager.IsChunkLoaded(chunkPos))
            {
                chunkLoadingManager.RefreshChunkVisuals(chunkPos);
            }
        }
    }
    
    /// <summary>
    /// Broadcast crop removed event
    /// </summary>
    public void BroadcastCropRemoved(int worldX, int worldY)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        object[] data = new object[] { worldX, worldY };
        
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others
        };
        
        PhotonNetwork.RaiseEvent(
            CROP_REMOVED_EVENT,
            data,
            options,
            SendOptions.SendReliable
        );
    }
    
    /// <summary>
    /// Handle crop removed event
    /// </summary>
    private void HandleCropRemoved(object data)
    {
        object[] dataArray = (object[])data;
        int worldX = (int)dataArray[0];
        int worldY = (int)dataArray[1];
        
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.RemoveCropAtWorldPosition(worldPos);
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received crop removed at ({worldX}, {worldY})");
        
        // Refresh visuals for this chunk
        if (chunkLoadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
            if (chunkLoadingManager.IsChunkLoaded(chunkPos))
            {
                chunkLoadingManager.RefreshChunkVisuals(chunkPos);
            }
        }
    }
    
    /// <summary>
    /// Broadcast crop stage updated event
    /// </summary>
    public void BroadcastCropStageUpdated(int worldX, int worldY, byte newStage)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        object[] data = new object[] { worldX, worldY, newStage };
        
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others
        };
        
        PhotonNetwork.RaiseEvent(
            CROP_STAGE_UPDATED_EVENT,
            data,
            options,
            SendOptions.SendReliable
        );
    }
    
    /// <summary>
    /// Handle crop stage updated event
    /// </summary>
    private void HandleCropStageUpdated(object data)
    {
        object[] dataArray = (object[])data;
        int worldX = (int)dataArray[0];
        int worldY = (int)dataArray[1];
        byte newStage = Convert.ToByte(dataArray[2]);
        
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.UpdateCropStage(worldPos, newStage);
        
        // Refresh chunk visuals to show the updated crop stage
        if (chunkLoadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
            if (chunkLoadingManager.IsChunkLoaded(chunkPos))
            {
                chunkLoadingManager.RefreshChunkVisuals(chunkPos);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received crop stage update: Stage {newStage} at ({worldX}, {worldY})");
    }

    /// <summary>
    /// Broadcast a tile-tilled event to other players
    /// </summary>
    public void BroadcastTileTilled(int worldX, int worldY)
    {
        if (!PhotonNetwork.IsConnected) return;

        object[] data = new object[] { worldX, worldY };

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others
        };

        PhotonNetwork.RaiseEvent(
            TILE_TILLED_EVENT,
            data,
            options,
            SendOptions.SendReliable
        );
    }

    /// <summary>
    /// Broadcast a tile-untill event to other players
    /// </summary>
    public void BroadcastTileUntilled(int worldX, int worldY)
    {
        if (!PhotonNetwork.IsConnected) return;

        object[] data = new object[] { worldX, worldY };

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others
        };

        PhotonNetwork.RaiseEvent(
            TILE_UNTILLED_EVENT,
            data,
            options,
            SendOptions.SendReliable
        );
    }

    private void HandleTileTilled(object data)
    {
        object[] dataArray = (object[])data;
        int worldX = (int)dataArray[0];
        int worldY = (int)dataArray[1];

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.TillTileAtWorldPosition(worldPos);

        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received tile tilled at ({worldX}, {worldY})");

        if (chunkLoadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
            if (chunkLoadingManager.IsChunkLoaded(chunkPos))
            {
                chunkLoadingManager.RefreshChunkVisuals(chunkPos);
            }
        }
    }

    private void HandleTileUntilled(object data)
    {
        object[] dataArray = (object[])data;
        int worldX = (int)dataArray[0];
        int worldY = (int)dataArray[1];

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.UntillTileAtWorldPosition(worldPos);

        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received tile untiled at ({worldX}, {worldY})");

        if (chunkLoadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
            if (chunkLoadingManager.IsChunkLoaded(chunkPos))
            {
                chunkLoadingManager.RefreshChunkVisuals(chunkPos);
            }
        }
    }

    /// <summary>
    /// Broadcast to all other clients that pollen was collected at (worldX, worldY).
    /// Sends the new authoritative count so receivers set it directly (no double-increment risk).
    /// </summary>
    public void BroadcastPollenHarvested(int worldX, int worldY, byte newCount)
    {
        if (!PhotonNetwork.IsConnected) return;

        object[] data = new object[] { worldX, worldY, newCount };

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.Others
        };

        PhotonNetwork.RaiseEvent(
            POLLEN_HARVESTED_EVENT,
            data,
            options,
            SendOptions.SendReliable
        );
    }

    private void HandlePollenHarvested(object data)
    {
        object[] dataArray = (object[])data;
        int  worldX   = (int)dataArray[0];
        int  worldY   = (int)dataArray[1];
        byte newCount = Convert.ToByte(dataArray[2]);

        Vector3 worldPos = new Vector3(worldX, worldY, 0);

        // Set the tile's count to the authoritative value via the chunk directly
        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(worldPos);
        if (chunk != null)
        {
            chunk.ResetPollenHarvestCount(worldX, worldY);
            for (byte i = 0; i < newCount; i++)
                chunk.IncrementPollenHarvestCount(worldX, worldY);
        }

        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Pollen harvested at ({worldX},{worldY}), count now {newCount}.");
    }

    /// <summary>
    /// Wait until WorldDataManager is initialized (or timeout) then send world data to the actor.
    /// This lets the master proactively sync late-join players.
    /// </summary>
    private System.Collections.IEnumerator WaitAndSendWorldData(int targetActorNumber)
    {
        float elapsed = 0f;
        float timeout = 5f;

        while (!WorldDataManager.Instance.IsInitialized && elapsed < timeout)
        {
            elapsed += 0.25f;
            yield return new WaitForSeconds(0.25f);
        }

        // Small buffer to allow the new player to finish local setup
        yield return new WaitForSeconds(0.5f);

        // Start sending (SendWorldDataToPlayer will respect isSyncing)
        StartCoroutine(SendWorldDataToPlayer(targetActorNumber));
    }
    
    #region Serialization Helpers
    
    [System.Serializable]
    private class ChunkSyncData
    {
        public int ChunkX;
        public int ChunkY;
        public int SectionId;
        // Each entry carries positional/flag data + crop sub-data for the wire format
        public SyncTileEntry[] Tiles;
    }

    /// <summary>
    /// Lightweight wire-format entry. Carries WorldX/WorldY/flags alongside crop data
    /// so the receiver can reconstruct the full TileSlot.
    /// CropTileData is kept clean without positional fields.
    /// </summary>
    private struct SyncTileEntry
    {
        public int  WorldX;
        public int  WorldY;
        public bool IsTilled;
        public bool HasCrop;
        public UnifiedChunkData.CropTileData Crop;
    }
    
    // Wire format per tile entry:
    //   WorldX(4) WorldY(4) flags(1) [if HasCrop: PlantIdLen(1) PlantId(N) Stage(1) TotalAge(4) PollenCount(1)]
    // flags: bit0=IsTilled, bit1=HasCrop
    private byte[] SerializeBatch(ChunkSyncData[] batch)
    {
        var bytes = new System.Collections.Generic.List<byte>();

        int totalEntries = 0;
        foreach (var chunk in batch) totalEntries += chunk.Tiles.Length;

        bytes.AddRange(System.BitConverter.GetBytes(batch.Length));   // 4 chunk count
        bytes.AddRange(System.BitConverter.GetBytes(totalEntries));   // 4 total entries (informational)

        foreach (var chunk in batch)
        {
            bytes.AddRange(System.BitConverter.GetBytes(chunk.ChunkX));
            bytes.AddRange(System.BitConverter.GetBytes(chunk.ChunkY));
            bytes.AddRange(System.BitConverter.GetBytes(chunk.SectionId));
            bytes.AddRange(System.BitConverter.GetBytes(chunk.Tiles.Length));

            foreach (var entry in chunk.Tiles)
            {
                bytes.AddRange(System.BitConverter.GetBytes(entry.WorldX));  // 4
                bytes.AddRange(System.BitConverter.GetBytes(entry.WorldY));  // 4

                byte flags = 0;
                if (entry.IsTilled) flags |= 1;
                if (entry.HasCrop)  flags |= 2;
                bytes.Add(flags);                                            // 1

                if (entry.HasCrop)
                {
                    byte[] plantIdBytes = string.IsNullOrEmpty(entry.Crop.PlantId)
                        ? System.Array.Empty<byte>()
                        : System.Text.Encoding.UTF8.GetBytes(entry.Crop.PlantId);
                    bytes.Add((byte)plantIdBytes.Length);                    // 1
                    bytes.AddRange(plantIdBytes);                            // N
                    bytes.Add(entry.Crop.CropStage);                        // 1
                    bytes.AddRange(System.BitConverter.GetBytes(entry.Crop.TotalAge)); // 4
                    bytes.Add(entry.Crop.PollenHarvestCount);               // 1
                }
            }
        }

        return bytes.ToArray();
    }
    
    private ChunkSyncData[] DeserializeBatch(byte[] data)
    {
        int offset = 0;
        int chunkCount  = System.BitConverter.ToInt32(data, offset); offset += 4;
        int totalEntries = System.BitConverter.ToInt32(data, offset); offset += 4;

        ChunkSyncData[] batch = new ChunkSyncData[chunkCount];

        for (int i = 0; i < chunkCount; i++)
        {
            ChunkSyncData chunk = new ChunkSyncData();
            chunk.ChunkX    = System.BitConverter.ToInt32(data, offset); offset += 4;
            chunk.ChunkY    = System.BitConverter.ToInt32(data, offset); offset += 4;
            chunk.SectionId = System.BitConverter.ToInt32(data, offset); offset += 4;
            int entryCount  = System.BitConverter.ToInt32(data, offset); offset += 4;

            chunk.Tiles = new SyncTileEntry[entryCount];

            for (int j = 0; j < entryCount; j++)
            {
                SyncTileEntry entry = new SyncTileEntry();
                entry.WorldX = System.BitConverter.ToInt32(data, offset); offset += 4;
                entry.WorldY = System.BitConverter.ToInt32(data, offset); offset += 4;

                byte flags   = data[offset++];
                entry.IsTilled = (flags & 1) != 0;
                entry.HasCrop  = (flags & 2) != 0;

                if (entry.HasCrop)
                {
                    int plantIdLen = data[offset++];
                    entry.Crop.PlantId = plantIdLen > 0
                        ? System.Text.Encoding.UTF8.GetString(data, offset, plantIdLen) : string.Empty;
                    offset += plantIdLen;
                    entry.Crop.CropStage          = data[offset++];
                    entry.Crop.TotalAge           = System.BitConverter.ToInt32(data, offset); offset += 4;
                    entry.Crop.PollenHarvestCount = offset < data.Length ? data[offset++] : (byte)0;
                }

                chunk.Tiles[j] = entry;
            }

            batch[i] = chunk;
        }

        return batch;
    }
    
    #endregion
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (showDebugLogs)
                Debug.Log($"[ChunkSync] Player {newPlayer.NickName} joined - proactively sending sync");

            StartCoroutine(WaitAndSendWorldData(newPlayer.ActorNumber));
        }
    }

    // ── Crossbreeding sync ────────────────────────────────────────────────

    /// <summary>
    /// Broadcast to all other clients that a crop has been cross-pollinated.
    /// Payload: worldX(4) + worldY(4) + startStage(1) + plantIdLen(1) + plantId(N)
    /// </summary>
    public void BroadcastCropCrossbred(int worldX, int worldY, string resultPlantId, byte startStage)
    {
        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(resultPlantId);
        var payload = new System.Collections.Generic.List<byte>();
        payload.AddRange(BitConverter.GetBytes(worldX));    // 4
        payload.AddRange(BitConverter.GetBytes(worldY));    // 4
        payload.Add(startStage);                            // 1
        payload.Add((byte)idBytes.Length);                  // 1
        payload.AddRange(idBytes);                          // N

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(CROP_CROSSBRED_EVENT, payload.ToArray(), opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[ChunkSync] BroadcastCropCrossbred ({worldX},{worldY}) → '{resultPlantId}' stage {startStage}");
    }

    private void HandleCropCrossbred(object data)
    {
        if (data is not byte[] bytes) return;

        int offset  = 0;
        int worldX  = BitConverter.ToInt32(bytes, offset); offset += 4;
        int worldY  = BitConverter.ToInt32(bytes, offset); offset += 4;
        byte stage  = bytes[offset++];
        int idLen   = bytes[offset++];
        string plantId = idLen > 0 ? System.Text.Encoding.UTF8.GetString(bytes, offset, idLen) : string.Empty;

        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.SetCropPlantId(worldPos, plantId, stage);

        // Refresh visuals for the affected chunk
        Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
        if (chunkLoadingManager != null && chunkLoadingManager.IsChunkLoaded(chunkPos))
            chunkLoadingManager.RefreshChunkVisuals(chunkPos);

        if (showDebugLogs)
            Debug.Log($"[ChunkSync] HandleCropCrossbred ({worldX},{worldY}) → '{plantId}' stage {stage}");
    }
}
