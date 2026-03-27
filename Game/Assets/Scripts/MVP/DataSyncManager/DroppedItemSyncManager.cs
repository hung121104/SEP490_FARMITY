using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;

/// <summary>
/// Handles Photon RaiseEvent-based synchronization of dropped items.
/// Follows the same pattern as ChunkDataSyncManager (event codes, binary serialization,
/// isSyncing flag, late-join batch sync, Master authority).
///
/// Event Codes:
///   140 - ITEM_DROP_REQUEST    (client → master)
///   141 - ITEM_SPAWNED         (master → all)
///   142 - ITEM_PICKUP_REQUEST  (client → master)
///   143 - ITEM_REMOVED         (master → all, includes pickedByActor)
///   144 - ITEM_SYNC_REQUEST    (late joiner → master)
///   145 - ITEM_SYNC_BATCH      (master → late joiner)
///   146 - ITEM_DESPAWN_NOTIFY  (master → all, TTL expired)
/// </summary>
public class DroppedItemSyncManager : MonoBehaviourPunCallbacks
{
    // ── Event Codes ──────────────────────────────────────────
    private const byte ITEM_DROP_REQUEST    = 140;
    private const byte ITEM_SPAWNED         = 141;
    private const byte ITEM_PICKUP_REQUEST  = 142;
    private const byte ITEM_REMOVED         = 143;
    private const byte ITEM_SYNC_REQUEST    = 144;
    private const byte ITEM_SYNC_BATCH      = 145;
    private const byte ITEM_DESPAWN_NOTIFY  = 146;
    private const byte ITEM_PARTIAL_PICKUP_REQUEST = 147;
    private const byte ITEM_PARTIAL_PICKUP_BROADCAST = 148;

    [Header("Sync Settings")]
    [Tooltip("Maximum items per batch during late-join sync")]
    public int itemsPerBatch = 20;

    [Tooltip("Delay between batches in seconds")]
    public float batchDelay = 0.3f;

    [Tooltip("Enable debug logging")]
    public bool showDebugLogs = true;

    // ── State Flags (same pattern as ChunkDataSyncManager) ───
    private bool isSyncing = false;
    private bool hasSyncedThisSession = false;

    // ── Events for DroppedItemManager to subscribe ───────────
    /// <summary>Fired on ALL clients when Master confirms an item spawn.</summary>
    public event Action<DroppedItemData> OnItemSpawned;

    /// <summary>Fired on ALL clients when an item is removed (pickup or despawn).</summary>
    public event Action<string, int> OnItemRemoved;  // (dropId, pickedByActorNumber) — 0 if despawn

    /// <summary>Fired on ALL clients when an item is partially picked up.</summary>
    public event Action<string, int, int> OnItemPartiallyPicked; // (dropId, amountPicked, pickedByActorNumber)

    /// <summary>Fired on late-join client when a batch of items arrives.</summary>
    public event Action<DroppedItemData[]> OnSyncBatchReceived;

    // ── Photon Event Subscription ────────────────────────────

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
            Debug.LogWarning("[DroppedItemSync] Not connected to Photon network");
            return;
        }

        // Late-join: request item sync from master after a short delay
        Invoke(nameof(RequestItemSync), 1.5f);
    }

    // ── Public Methods (called by DroppedItemManager) ────────

    /// <summary>
    /// Client sends a drop request to Master.
    /// Master will validate, assign dropId/expireAt, persist to DB, and broadcast ITEM_SPAWNED.
    /// </summary>
    public void SendDropRequest(DroppedItemData data)
    {
        if (!PhotonNetwork.IsConnected) return;

        byte[] payload = SerializeSingleItem(data);

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(ITEM_DROP_REQUEST, payload, opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Sent DROP_REQUEST: {data.itemName} at ({data.worldX},{data.worldY})");
    }

    /// <summary>
    /// Client sends a pickup request to Master.
    /// Master checks if item still exists (race condition guard), then broadcasts ITEM_REMOVED.
    /// </summary>
    public void SendPickupRequest(string dropId)
    {
        if (!PhotonNetwork.IsConnected || string.IsNullOrEmpty(dropId)) return;

        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(dropId);

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(ITEM_PICKUP_REQUEST, idBytes, opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Sent PICKUP_REQUEST: {dropId}");
    }

    /// <summary>
    /// Client sends a partial pickup request to Master (when inventory is almost full).
    /// Master subtracts the amount, and broadcasts ITEM_PARTIAL_PICKUP.
    /// </summary>
    public void SendPartialPickupRequest(string dropId, int amount)
    {
        if (!PhotonNetwork.IsConnected || string.IsNullOrEmpty(dropId) || amount <= 0) return;

        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(dropId);
        var payload = new List<byte>();
        payload.AddRange(BitConverter.GetBytes(idBytes.Length)); // 4 bytes: id length
        payload.AddRange(idBytes);                               // N bytes: dropId
        payload.AddRange(BitConverter.GetBytes(amount));         // 4 bytes: amount

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(ITEM_PARTIAL_PICKUP_REQUEST, payload.ToArray(), opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Sent PARTIAL_PICKUP_REQUEST: {dropId} x{amount}");
    }

    /// <summary>
    /// Master broadcasts despawn notification (item TTL expired).
    /// Only call from Master.
    /// </summary>
    public void BroadcastItemDespawn(string dropId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(dropId);

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(ITEM_DESPAWN_NOTIFY, idBytes, opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Broadcast DESPAWN_NOTIFY: {dropId}");
    }

    // ── Late-Join Sync ───────────────────────────────────────

    /// <summary>
    /// Request dropped item sync from master (for late joiners).
    /// Same pattern as ChunkDataSyncManager.RequestWorldSync().
    /// </summary>
    private void RequestItemSync()
    {
        if (hasSyncedThisSession)
        {
            if (showDebugLogs)
                Debug.Log("[DroppedItemSync] Already synced this session, skipping");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (showDebugLogs)
                Debug.Log("[DroppedItemSync] Is master client, no need to request sync");
            hasSyncedThisSession = true;
            return;
        }

        if (showDebugLogs)
            Debug.Log("[DroppedItemSync] Requesting item sync from master...");

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(
            ITEM_SYNC_REQUEST,
            PhotonNetwork.LocalPlayer.ActorNumber,
            opts,
            SendOptions.SendReliable
        );
    }

    // ── Photon Event Dispatch ────────────────────────────────

    private void OnPhotonEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case ITEM_DROP_REQUEST:
                if (PhotonNetwork.IsMasterClient)
                    HandleDropRequest(photonEvent.CustomData, photonEvent.Sender);
                break;

            case ITEM_SPAWNED:
                HandleItemSpawned(photonEvent.CustomData);
                break;

            case ITEM_PICKUP_REQUEST:
                if (PhotonNetwork.IsMasterClient)
                    HandlePickupRequest(photonEvent.CustomData, photonEvent.Sender);
                break;

            case ITEM_REMOVED:
                HandleItemRemovedEvent(photonEvent.CustomData);
                break;

            case ITEM_SYNC_REQUEST:
                if (PhotonNetwork.IsMasterClient)
                    HandleSyncRequest(photonEvent.CustomData);
                break;

            case ITEM_SYNC_BATCH:
                if (!PhotonNetwork.IsMasterClient)
                    HandleSyncBatch(photonEvent.CustomData);
                break;

            case ITEM_DESPAWN_NOTIFY:
                HandleDespawnNotify(photonEvent.CustomData);
                break;

            case ITEM_PARTIAL_PICKUP_REQUEST:
                if (PhotonNetwork.IsMasterClient)
                    HandlePartialPickupRequest(photonEvent.CustomData, photonEvent.Sender);
                break;
                
            case ITEM_PARTIAL_PICKUP_BROADCAST:
                HandlePartialPickupBroadcast(photonEvent.CustomData);
                break;
        }
    }

    // ── Master Handlers ──────────────────────────────────────

    /// <summary>
    /// Master receives DROP_REQUEST: assign dropId, expireAt, persist, broadcast.
    /// </summary>
    private void HandleDropRequest(object data, int senderActorNumber)
    {
        if (data is not byte[] bytes) return;

        DroppedItemData item = DeserializeSingleItem(bytes);
        if (item == null) return;

        // Master assigns authoritative fields
        item.dropId            = System.Guid.NewGuid().ToString();
        item.roomName          = PhotonNetwork.CurrentRoom?.Name ?? "";
        item.droppedByActorId  = senderActorNumber;
        item.droppedAt         = DateTime.UtcNow.ToString("o");
        item.expireAt          = DateTime.UtcNow.AddSeconds(360).ToString("o");

        // Compute chunk coordinates from world position
        if (WorldDataManager.Instance != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(
                new Vector3(item.worldX, item.worldY, 0));
            item.chunkX = chunkPos.x;
            item.chunkY = chunkPos.y;
        }

        // Broadcast ITEM_SPAWNED to all clients (including self)
        byte[] payload = SerializeSingleItem(item);
        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(ITEM_SPAWNED, payload, opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Master: assigned dropId={item.dropId}, broadcast ITEM_SPAWNED");
    }

    /// <summary>
    /// Master receives PICKUP_REQUEST: validate, remove, broadcast ITEM_REMOVED.
    /// Race condition guard: if item already removed, silently ignore.
    /// </summary>
    private void HandlePickupRequest(object data, int senderActorNumber)
    {
        if (data is not byte[] bytes) return;

        string dropId = System.Text.Encoding.UTF8.GetString(bytes);

        // Check if item still exists (DroppedItemManagerView manages the service)
        var manager = DroppedItemManagerView.Instance;
        if (manager == null || !manager.HasDroppedItem(dropId))
        {
            if (showDebugLogs)
                Debug.Log($"[DroppedItemSync] Master: PICKUP_REQUEST for '{dropId}' — item not found (race condition), ignoring");
            return;
        }

        // Broadcast ITEM_REMOVED to all clients
        // Payload: dropId bytes + 4 bytes actorNumber
        byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(dropId);
        var payload = new List<byte>();
        payload.AddRange(BitConverter.GetBytes(idBytes.Length)); // 4 bytes: id length
        payload.AddRange(idBytes);                               // N bytes: dropId
        payload.AddRange(BitConverter.GetBytes(senderActorNumber)); // 4 bytes: who picked

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(ITEM_REMOVED, payload.ToArray(), opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Master: broadcast ITEM_REMOVED dropId={dropId}, pickedBy={senderActorNumber}");
    }

    /// <summary>
    /// Master receives PARTIAL_PICKUP_REQUEST: validate, update quantity, broadcast.
    /// If requested amount >= current quantity, treats it as a full pickup.
    /// </summary>
    private void HandlePartialPickupRequest(object data, int senderActorNumber)
    {
        if (data is not byte[] bytes) return;

        int offset = 0;
        int idLen = BitConverter.ToInt32(bytes, offset); offset += 4;
        string dropId = System.Text.Encoding.UTF8.GetString(bytes, offset, idLen); offset += idLen;
        int amount = BitConverter.ToInt32(bytes, offset);

        var manager = DroppedItemManagerView.Instance;
        if (manager == null || !manager.HasDroppedItem(dropId))
        {
            if (showDebugLogs)
                Debug.Log($"[DroppedItemSync] Master: PARTIAL_PICKUP for '{dropId}' not found, ignoring");
            return;
        }

        var itemData = DroppedItemManagerView.Instance.GetAllDroppedItems();
        DroppedItemData targetItem = null;
        foreach (var i in itemData) { if (i.dropId == dropId) { targetItem = i; break; } }
        
        if (targetItem == null) return;

        if (amount >= targetItem.quantity)
        {
            // Just do full pickup instead
            byte[] idBytes = System.Text.Encoding.UTF8.GetBytes(dropId);
            HandlePickupRequest(idBytes, senderActorNumber);
            return;
        }

        // Broadcast PARTIAL_PICKUP
        var payload = new List<byte>();
        var idBytes2 = System.Text.Encoding.UTF8.GetBytes(dropId);
        payload.AddRange(BitConverter.GetBytes(idBytes2.Length));
        payload.AddRange(idBytes2);
        payload.AddRange(BitConverter.GetBytes(amount)); // amount picked
        payload.AddRange(BitConverter.GetBytes(senderActorNumber));

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(ITEM_PARTIAL_PICKUP_BROADCAST, payload.ToArray(), opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Master: broadcash PARTIAL_PICKUP drop={dropId}, amt={amount}, by={senderActorNumber}");
    }

    /// <summary>
    /// Master receives ITEM_SYNC_REQUEST from a late joiner: send all items in batches.
    /// </summary>
    private void HandleSyncRequest(object data)
    {
        int requestingActor = (int)data;

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Master received SYNC_REQUEST from player {requestingActor}");

        StartCoroutine(SendItemBatchToPlayer(requestingActor));
    }

    /// <summary>
    /// Send all dropped items to a specific player in batches.
    /// Same batched approach as ChunkDataSyncManager.SendWorldDataToPlayer().
    /// </summary>
    private IEnumerator SendItemBatchToPlayer(int targetActorNumber)
    {
        if (isSyncing)
        {
            Debug.LogWarning("[DroppedItemSync] Already syncing, ignoring new request");
            yield break;
        }

        isSyncing = true;

        var manager = DroppedItemManagerView.Instance;
        List<DroppedItemData> allItems = manager != null
            ? new List<DroppedItemData>(manager.GetAllDroppedItems())
            : new List<DroppedItemData>();

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Sending {allItems.Count} items to player {targetActorNumber}");

        int totalBatches = Mathf.CeilToInt((float)allItems.Count / itemsPerBatch);
        if (totalBatches == 0) totalBatches = 1; // Send at least one empty batch

        for (int batchIdx = 0; batchIdx < totalBatches; batchIdx++)
        {
            int startIndex = batchIdx * itemsPerBatch;
            int count = Mathf.Min(itemsPerBatch, allItems.Count - startIndex);

            // Build batch
            var batchItems = new List<DroppedItemData>();
            for (int i = 0; i < count; i++)
            {
                batchItems.Add(allItems[startIndex + i]);
            }

            byte[] serialized = SerializeItemBatch(batchItems);

            object[] payload = new object[] { batchIdx, totalBatches, serialized };

            RaiseEventOptions opts = new RaiseEventOptions
            {
                TargetActors = new int[] { targetActorNumber }
            };

            PhotonNetwork.RaiseEvent(ITEM_SYNC_BATCH, payload, opts, SendOptions.SendReliable);

            if (showDebugLogs)
                Debug.Log($"[DroppedItemSync] Sent batch {batchIdx + 1}/{totalBatches} ({count} items)");

            yield return new WaitForSeconds(batchDelay);
        }

        isSyncing = false;
    }

    // ── All-Client Handlers ──────────────────────────────────

    /// <summary>
    /// All clients: Master broadcast an item spawn. Notify DroppedItemManager.
    /// </summary>
    private void HandleItemSpawned(object data)
    {
        if (data is not byte[] bytes) return;

        DroppedItemData item = DeserializeSingleItem(bytes);
        if (item == null) return;

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] ITEM_SPAWNED: {item.itemName} ({item.dropId}) at ({item.worldX},{item.worldY})");

        OnItemSpawned?.Invoke(item);
    }

    /// <summary>
    /// All clients: an item was picked up. Notify DroppedItemManager.
    /// </summary>
    private void HandleItemRemovedEvent(object data)
    {
        if (data is not byte[] bytes) return;

        int offset = 0;
        int idLen = BitConverter.ToInt32(bytes, offset); offset += 4;
        string dropId = System.Text.Encoding.UTF8.GetString(bytes, offset, idLen); offset += idLen;
        int pickedByActor = BitConverter.ToInt32(bytes, offset);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] ITEM_REMOVED: {dropId}, pickedBy={pickedByActor}");

        OnItemRemoved?.Invoke(dropId, pickedByActor);
    }

    /// <summary>
    /// Late-join client: received a batch of items from Master.
    /// </summary>
    private void HandleSyncBatch(object data)
    {
        object[] dataArray = (object[])data;
        int batchIndex   = (int)dataArray[0];
        int totalBatches = (int)dataArray[1];
        byte[] serialized = (byte[])dataArray[2];

        DroppedItemData[] items = DeserializeItemBatch(serialized);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] Received sync batch {batchIndex + 1}/{totalBatches}: {items.Length} items");

        hasSyncedThisSession = true;
        OnSyncBatchReceived?.Invoke(items);
    }

    /// <summary>
    /// All clients: item despawned due to TTL expiry.
    /// </summary>
    private void HandleDespawnNotify(object data)
    {
        if (data is not byte[] bytes) return;

        string dropId = System.Text.Encoding.UTF8.GetString(bytes);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] DESPAWN_NOTIFY: {dropId}");

        // pickedByActor = 0 means despawn (no one picked it up)
        OnItemRemoved?.Invoke(dropId, 0);
    }

    /// <summary>
    /// All clients: item was partially picked up.
    /// Updates local quantity and notifies listeners.
    /// </summary>
    private void HandlePartialPickupBroadcast(object data)
    {
        if (data is not byte[] bytes) return;

        int offset = 0;
        int idLen = BitConverter.ToInt32(bytes, offset); offset += 4;
        string dropId = System.Text.Encoding.UTF8.GetString(bytes, offset, idLen); offset += idLen;
        int amountPicked = BitConverter.ToInt32(bytes, offset); offset += 4;
        int pickedByActor = BitConverter.ToInt32(bytes, offset);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemSync] ITEM_PARTIAL_PICKUP: {dropId}, amount={amountPicked}, by={pickedByActor}");

        OnItemPartiallyPicked?.Invoke(dropId, amountPicked, pickedByActor);
    }

    // ── Binary Serialization (same pattern as ChunkDataSyncManager) ──

    /// <summary>
    /// Serialize a single DroppedItemData into a byte array.
    /// Format: each string field is length-prefixed (2 bytes length + N bytes UTF8).
    /// </summary>
    private byte[] SerializeSingleItem(DroppedItemData item)
    {
        var buf = new List<byte>();

        // String fields: dropId, roomName, itemId, itemName, iconUrl, droppedAt, expireAt
        WriteString(buf, item.dropId);
        WriteString(buf, item.roomName);
        WriteString(buf, item.itemId);
        WriteString(buf, item.itemName);
        WriteString(buf, item.iconUrl);
        WriteString(buf, item.droppedAt);
        WriteString(buf, item.expireAt);

        // Numeric fields
        buf.AddRange(BitConverter.GetBytes((int)item.itemType));      // 4
        buf.AddRange(BitConverter.GetBytes((int)item.itemCategory));  // 4
        buf.AddRange(BitConverter.GetBytes((int)item.quality));       // 4
        buf.AddRange(BitConverter.GetBytes(item.quantity));            // 4
        buf.AddRange(BitConverter.GetBytes(item.worldX));              // 4
        buf.AddRange(BitConverter.GetBytes(item.worldY));              // 4
        buf.AddRange(BitConverter.GetBytes(item.chunkX));              // 4
        buf.AddRange(BitConverter.GetBytes(item.chunkY));              // 4
        buf.AddRange(BitConverter.GetBytes(item.sectionId));           // 4
        buf.AddRange(BitConverter.GetBytes(item.droppedByActorId));    // 4
        buf.Add((byte)(item.isStackable ? 1 : 0));                    // 1

        return buf.ToArray();
    }

    /// <summary>
    /// Deserialize a single DroppedItemData from a byte array.
    /// </summary>
    private DroppedItemData DeserializeSingleItem(byte[] bytes)
    {
        try
        {
            int offset = 0;
            var item = new DroppedItemData();

            item.dropId     = ReadString(bytes, ref offset);
            item.roomName   = ReadString(bytes, ref offset);
            item.itemId     = ReadString(bytes, ref offset);
            item.itemName   = ReadString(bytes, ref offset);
            item.iconUrl    = ReadString(bytes, ref offset);
            item.droppedAt  = ReadString(bytes, ref offset);
            item.expireAt   = ReadString(bytes, ref offset);

            item.itemType         = (ItemType)BitConverter.ToInt32(bytes, offset);      offset += 4;
            item.itemCategory     = (ItemCategory)BitConverter.ToInt32(bytes, offset);   offset += 4;
            item.quality          = (Quality)BitConverter.ToInt32(bytes, offset);         offset += 4;
            item.quantity         = BitConverter.ToInt32(bytes, offset);                  offset += 4;
            item.worldX           = BitConverter.ToSingle(bytes, offset);                 offset += 4;
            item.worldY           = BitConverter.ToSingle(bytes, offset);                 offset += 4;
            item.chunkX           = BitConverter.ToInt32(bytes, offset);                  offset += 4;
            item.chunkY           = BitConverter.ToInt32(bytes, offset);                  offset += 4;
            item.sectionId        = BitConverter.ToInt32(bytes, offset);                  offset += 4;
            item.droppedByActorId = BitConverter.ToInt32(bytes, offset);                  offset += 4;
            item.isStackable      = bytes[offset] == 1;

            return item;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DroppedItemSync] DeserializeSingleItem failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Serialize a batch of items. Format: [4 bytes count] + [item1] + [item2] + ...
    /// Each item is length-prefixed so we can split them during deserialization.
    /// </summary>
    private byte[] SerializeItemBatch(List<DroppedItemData> items)
    {
        var buf = new List<byte>();
        buf.AddRange(BitConverter.GetBytes(items.Count)); // 4 bytes: item count

        foreach (var item in items)
        {
            byte[] itemBytes = SerializeSingleItem(item);
            buf.AddRange(BitConverter.GetBytes(itemBytes.Length)); // 4 bytes: item byte length
            buf.AddRange(itemBytes);                               // N bytes: item data
        }

        return buf.ToArray();
    }

    /// <summary>
    /// Deserialize a batch of items.
    /// </summary>
    private DroppedItemData[] DeserializeItemBatch(byte[] bytes)
    {
        try
        {
            int offset = 0;
            int count = BitConverter.ToInt32(bytes, offset); offset += 4;

            var items = new DroppedItemData[count];
            for (int i = 0; i < count; i++)
            {
                int itemLen = BitConverter.ToInt32(bytes, offset); offset += 4;
                byte[] itemBytes = new byte[itemLen];
                System.Array.Copy(bytes, offset, itemBytes, 0, itemLen);
                offset += itemLen;

                items[i] = DeserializeSingleItem(itemBytes);
            }

            return items;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DroppedItemSync] DeserializeItemBatch failed: {ex.Message}");
            return System.Array.Empty<DroppedItemData>();
        }
    }

    // ── Binary Helpers ───────────────────────────────────────

    /// <summary>Write a length-prefixed UTF8 string (2 bytes length + N bytes).</summary>
    private void WriteString(List<byte> buf, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            buf.AddRange(BitConverter.GetBytes((ushort)0));
            return;
        }
        byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(value);
        buf.AddRange(BitConverter.GetBytes((ushort)strBytes.Length));
        buf.AddRange(strBytes);
    }

    /// <summary>Read a length-prefixed UTF8 string.</summary>
    private string ReadString(byte[] bytes, ref int offset)
    {
        ushort len = BitConverter.ToUInt16(bytes, offset); offset += 2;
        if (len == 0) return "";
        string value = System.Text.Encoding.UTF8.GetString(bytes, offset, len);
        offset += len;
        return value;
    }
}
