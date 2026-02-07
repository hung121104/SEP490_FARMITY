using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
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
    
    // Photon event codes
    private const byte REQUEST_WORLD_SYNC_EVENT = 110;
    private const byte WORLD_SYNC_BATCH_EVENT = 111;
    private const byte WORLD_SYNC_COMPLETE_EVENT = 112;
    private const byte CROP_PLANTED_EVENT = 113;
    private const byte CROP_REMOVED_EVENT = 114;
    private const byte CROP_STAGE_UPDATED_EVENT = 115;
    
    private bool isSyncing = false;
    private bool hasSyncedThisSession = false;
    
    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }
    
    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }
    
    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[ChunkSync] Not connected to Photon network");
            return;
        }
        
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
        
        // Collect all chunks with crops
        foreach (var config in manager.sectionConfigs)
        {
            if (!config.IsActive) continue;
            
            var section = manager.GetSection(config.SectionId);
            if (section == null) continue;
            
            foreach (var chunkPair in section)
            {
                CropChunkData chunk = chunkPair.Value;
                if (chunk.GetCropCount() == 0) continue; // Skip empty chunks
                
                ChunkSyncData syncData = new ChunkSyncData
                {
                    ChunkX = chunk.ChunkX,
                    ChunkY = chunk.ChunkY,
                    SectionId = chunk.SectionId,
                    Crops = chunk.GetAllCrops().ToArray()
                };
                
                allChunkData.Add(syncData);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Sending {allChunkData.Count} chunks with crops to player {targetPlayerActorNumber}");
        
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
            CropChunkData chunk = manager.GetChunk(chunkData.SectionId, chunkPos);
            
            if (chunk == null)
            {
                Debug.LogWarning($"[ChunkSync] Chunk ({chunkData.ChunkX}, {chunkData.ChunkY}) not found in section {chunkData.SectionId}");
                continue;
            }
            
            // Clear existing data and load synced crops
            chunk.Clear();
            
            foreach (var crop in chunkData.Crops)
            {
                chunk.PlantCrop(crop.CropTypeID, crop.WorldX, crop.WorldY);
                if (crop.CropStage > 0)
                {
                    chunk.UpdateCropStage(crop.WorldX, crop.WorldY, crop.CropStage);
                }
            }

                // Ensure visuals are spawned for this chunk on the client
                ChunkLoadingManager loadingManager = FindObjectOfType<ChunkLoadingManager>();
                if (loadingManager != null)
                {
                    loadingManager.EnsureChunkLoaded(chunkPos);
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
    public void BroadcastCropPlanted(int worldX, int worldY, int cropTypeID)
    {
        if (!PhotonNetwork.IsConnected) return;
        
        object[] data = new object[] { worldX, worldY, cropTypeID };
        
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
        int worldX = (int)dataArray[0];
        int worldY = (int)dataArray[1];
        int cropTypeID = (int)dataArray[2];
        
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.PlantCropAtWorldPosition(worldPos, (ushort)cropTypeID);
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received crop planted: Type {cropTypeID} at ({worldX}, {worldY})");
        
        // Refresh visuals for this chunk
        ChunkLoadingManager loadingManager = FindObjectOfType<ChunkLoadingManager>();
        if (loadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
            loadingManager.RefreshChunkVisuals(chunkPos);
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
        byte newStage = (byte)(int)dataArray[2];
        
        Vector3 worldPos = new Vector3(worldX, worldY, 0);
        WorldDataManager.Instance.UpdateCropStage(worldPos, newStage);
        
        if (showDebugLogs)
            Debug.Log($"[ChunkSync] Received crop stage update: Stage {newStage} at ({worldX}, {worldY})");
    }
    
    #region Serialization Helpers
    
    [System.Serializable]
    private class ChunkSyncData
    {
        public int ChunkX;
        public int ChunkY;
        public int SectionId;
        public CropChunkData.TileData[] Crops;
    }
    
    private byte[] SerializeBatch(ChunkSyncData[] batch)
    {
        // Simple serialization: count all crops first
        int totalCrops = 0;
        foreach (var chunk in batch)
        {
            totalCrops += chunk.Crops.Length;
        }
        
        // Header: chunkCount(4) + totalCrops(4) = 8 bytes
        // Per chunk: ChunkX(4) + ChunkY(4) + SectionId(4) + CropCount(4) = 16 bytes
        // Per tile: 13 bytes (IsTilled(1) + HasCrop(1) + CropTypeID(2) + CropStage(1) + WorldX(4) + WorldY(4))
        byte[] data = new byte[8 + (batch.Length * 16) + (totalCrops * 13)];
        
        int offset = 0;
        
        // Write header
        System.BitConverter.GetBytes(batch.Length).CopyTo(data, offset);
        offset += 4;
        System.BitConverter.GetBytes(totalCrops).CopyTo(data, offset);
        offset += 4;
        
        // Write chunks
        foreach (var chunk in batch)
        {
            System.BitConverter.GetBytes(chunk.ChunkX).CopyTo(data, offset);
            offset += 4;
            System.BitConverter.GetBytes(chunk.ChunkY).CopyTo(data, offset);
            offset += 4;
            System.BitConverter.GetBytes(chunk.SectionId).CopyTo(data, offset);
            offset += 4;
            System.BitConverter.GetBytes(chunk.Crops.Length).CopyTo(data, offset);
            offset += 4;
            
            // Write tiles
            foreach (var tile in chunk.Crops)
            {
                data[offset] = (byte)(tile.IsTilled ? 1 : 0);
                offset += 1;
                data[offset] = (byte)(tile.HasCrop ? 1 : 0);
                offset += 1;
                System.BitConverter.GetBytes(tile.CropTypeID).CopyTo(data, offset);
                offset += 2;
                data[offset] = tile.CropStage;
                offset += 1;
                System.BitConverter.GetBytes(tile.WorldX).CopyTo(data, offset);
                offset += 4;
                System.BitConverter.GetBytes(tile.WorldY).CopyTo(data, offset);
                offset += 4;
            }
        }
        
        return data;
    }
    
    private ChunkSyncData[] DeserializeBatch(byte[] data)
    {
        int offset = 0;
        
        // Read header
        int chunkCount = System.BitConverter.ToInt32(data, offset);
        offset += 4;
        int totalCrops = System.BitConverter.ToInt32(data, offset);
        offset += 4;
        
        ChunkSyncData[] batch = new ChunkSyncData[chunkCount];
        
        // Read chunks
        for (int i = 0; i < chunkCount; i++)
        {
            ChunkSyncData chunk = new ChunkSyncData();
            
            chunk.ChunkX = System.BitConverter.ToInt32(data, offset);
            offset += 4;
            chunk.ChunkY = System.BitConverter.ToInt32(data, offset);
            offset += 4;
            chunk.SectionId = System.BitConverter.ToInt32(data, offset);
            offset += 4;
            int cropCount = System.BitConverter.ToInt32(data, offset);
            offset += 4;
            
            chunk.Crops = new CropChunkData.TileData[cropCount];
            
            // Read tiles
            for (int j = 0; j < cropCount; j++)
            {
                CropChunkData.TileData tile = new CropChunkData.TileData();
                
                tile.IsTilled = data[offset] == 1;
                offset += 1;
                tile.HasCrop = data[offset] == 1;
                offset += 1;
                tile.CropTypeID = System.BitConverter.ToUInt16(data, offset);
                offset += 2;
                tile.CropStage = data[offset];
                offset += 1;
                tile.WorldX = System.BitConverter.ToInt32(data, offset);
                offset += 4;
                tile.WorldY = System.BitConverter.ToInt32(data, offset);
                offset += 4;
                
                chunk.Crops[j] = tile;
            }
            
            batch[i] = chunk;
        }
        
        return batch;
    }
    
    #endregion
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient && showDebugLogs)
        {
            Debug.Log($"[ChunkSync] Player {newPlayer.NickName} joined - they will request sync");
        }
    }
}
