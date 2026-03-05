using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Bridges InventoryDataModule ↔ Photon network.
///
/// Master Authority:
///   Master holds the authoritative inventory data in WorldDataManager.InventoryData.
///   Clients send change requests → Master validates, applies, broadcasts.
///
/// Event Codes: 130–135 (reserved for inventory)
///   130 = REQUEST_INV_SYNC      Client → Master (late join: "send me all inventories")
///   131 = INV_SYNC_BATCH        Master → Client (full inventory data batch)
///   132 = INV_SYNC_COMPLETE     Master → Client (all batches sent)
///   133 = SLOT_CHANGE_REQUEST   Client → Master (single slot change)
///   134 = SLOT_BROADCAST        Master → Others (single slot change applied)
///   135 = INV_REGISTER          Master → Others (new character registered)
///
/// Usage (from gameplay code):
///   InventorySyncManager.Instance.RequestSetSlot(slotIndex, itemId, quantity);
///   InventorySyncManager.Instance.RequestAddQuantity(slotIndex, itemId, amount);
///   InventorySyncManager.Instance.RequestRemoveQuantity(slotIndex, amount);
///   InventorySyncManager.Instance.RequestSwapSlots(slotA, slotB);
/// </summary>
public class InventorySyncManager : MonoBehaviourPunCallbacks
{
    // ── Singleton ────────────────────────────────────────────
    public static InventorySyncManager Instance { get; private set; }

    // ── Event Codes ──────────────────────────────────────────
    private const byte REQUEST_INV_SYNC      = 130;
    private const byte INV_SYNC_BATCH        = 131;
    private const byte INV_SYNC_COMPLETE     = 132;
    private const byte SLOT_CHANGE_REQUEST   = 133;
    private const byte SLOT_BROADCAST        = 134;
    private const byte INV_REGISTER          = 135;

    // ── Delta Operation Types ────────────────────────────────
    private const byte OP_SET_SLOT       = 0;
    private const byte OP_CLEAR_SLOT     = 1;
    private const byte OP_ADD_QUANTITY   = 2;
    private const byte OP_REMOVE_QUANTITY = 3;
    private const byte OP_SWAP_SLOTS     = 4;

    // ── Settings ─────────────────────────────────────────────
    [Header("Sync Settings")]
    [Tooltip("Maximum inventories to sync per batch")]
    public int inventoriesPerBatch = 5;

    [Tooltip("Delay between batches in seconds")]
    public float batchDelay = 0.3f;

    [Tooltip("Enable debug logging")]
    public bool showDebugLogs = true;

    // ── State ────────────────────────────────────────────────
    private bool isSyncing = false;
    private bool hasSyncedThisSession = false;

    // ── Events (UI subscribes here) ──────────────────────────
    /// <summary>Fired whenever any inventory slot changes (local or remote).</summary>
    public static event System.Action OnInventoryChanged;

    /// <summary>Fired when a character inventory is registered.</summary>
    public static event System.Action<string> OnCharacterRegistered;

    // ══════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

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
            if (showDebugLogs)
                Debug.LogWarning("[InvSync] Not connected to Photon network");
            return;
        }

        // Late-join: request inventory sync after a short delay
        Invoke(nameof(RequestInventorySync), 1.5f);
    }

    // ══════════════════════════════════════════════════════════
    // PUBLIC API — called by gameplay / UI code
    // ══════════════════════════════════════════════════════════

    /// <summary>Get local player's characterId (from Photon UserId).</summary>
    public string LocalCharacterId
    {
        get
        {
            if (PhotonNetwork.LocalPlayer == null) return null;
            return !string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.UserId)
                ? PhotonNetwork.LocalPlayer.UserId
                : PhotonNetwork.LocalPlayer.NickName;
        }
    }

    /// <summary>
    /// Register local player's inventory on Master.
    /// Call once when entering the game world.
    /// </summary>
    public void RegisterLocalPlayerInventory(byte maxSlots = 36)
    {
        string charId = LocalCharacterId;
        if (string.IsNullOrEmpty(charId))
        {
            Debug.LogError("[InvSync] Cannot register — no local characterId available.");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            MasterRegisterCharacter(charId, maxSlots);
        }
        else
        {
            // Send register request to master
            byte[] payload = EncodeRegisterRequest(charId, maxSlots);
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(INV_REGISTER, payload, opts, SendOptions.SendReliable);
        }
    }

    /// <summary>Request Master to set a slot (replace item).</summary>
    public void RequestSetSlot(byte slotIndex, ushort itemId, ushort quantity)
    {
        SendSlotRequest(OP_SET_SLOT, slotIndex, itemId, quantity, 0);
    }

    /// <summary>Request Master to clear a slot.</summary>
    public void RequestClearSlot(byte slotIndex)
    {
        SendSlotRequest(OP_CLEAR_SLOT, slotIndex, 0, 0, 0);
    }

    /// <summary>Request Master to add quantity to a slot.</summary>
    public void RequestAddQuantity(byte slotIndex, ushort itemId, ushort amount)
    {
        SendSlotRequest(OP_ADD_QUANTITY, slotIndex, itemId, amount, 0);
    }

    /// <summary>Request Master to remove quantity from a slot.</summary>
    public void RequestRemoveQuantity(byte slotIndex, ushort amount)
    {
        SendSlotRequest(OP_REMOVE_QUANTITY, slotIndex, 0, amount, 0);
    }

    /// <summary>Request Master to swap two slots.</summary>
    public void RequestSwapSlots(byte slotA, byte slotB)
    {
        SendSlotRequest(OP_SWAP_SLOTS, slotA, 0, 0, slotB);
    }

    // ══════════════════════════════════════════════════════════
    // LATE-JOIN SYNC
    // ══════════════════════════════════════════════════════════

    private void RequestInventorySync()
    {
        if (hasSyncedThisSession) return;

        if (PhotonNetwork.IsMasterClient)
        {
            if (showDebugLogs)
                Debug.Log("[InvSync] Is master client, no need to request sync");
            hasSyncedThisSession = true;
            return;
        }

        if (!WorldDataManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[InvSync] WorldDataManager not initialized yet, retrying...");
            Invoke(nameof(RequestInventorySync), 1f);
            return;
        }

        if (showDebugLogs)
            Debug.Log("[InvSync] Requesting inventory sync from master...");

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(
            REQUEST_INV_SYNC,
            PhotonNetwork.LocalPlayer.ActorNumber,
            opts,
            SendOptions.SendReliable
        );
    }

    // ══════════════════════════════════════════════════════════
    // PHOTON EVENT DISPATCHER
    // ══════════════════════════════════════════════════════════

    private void OnPhotonEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            // ── Master receives ──────────────────────────────
            case REQUEST_INV_SYNC when PhotonNetwork.IsMasterClient:
                int actorNumber = (int)photonEvent.CustomData;
                HandleSyncRequest(actorNumber);
                break;

            case SLOT_CHANGE_REQUEST when PhotonNetwork.IsMasterClient:
                HandleSlotChangeRequest((byte[])photonEvent.CustomData, photonEvent.Sender);
                break;

            case INV_REGISTER when PhotonNetwork.IsMasterClient:
                HandleRegisterRequest((byte[])photonEvent.CustomData);
                break;

            // ── Client receives ──────────────────────────────
            case INV_SYNC_BATCH when !PhotonNetwork.IsMasterClient:
                HandleSyncBatch(photonEvent.CustomData);
                break;

            case INV_SYNC_COMPLETE when !PhotonNetwork.IsMasterClient:
                HandleSyncComplete(photonEvent.CustomData);
                break;

            case SLOT_BROADCAST:
                HandleSlotBroadcast((byte[])photonEvent.CustomData);
                break;

            case INV_REGISTER when !PhotonNetwork.IsMasterClient:
                HandleRegisterBroadcast((byte[])photonEvent.CustomData);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════
    // MASTER: SYNC REQUEST HANDLER
    // ══════════════════════════════════════════════════════════

    private void HandleSyncRequest(int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (showDebugLogs)
            Debug.Log($"[InvSync] Master received sync request from actor {requestingActorNumber}");

        StartCoroutine(SendAllInventoriesToPlayer(requestingActorNumber));
    }

    private IEnumerator SendAllInventoriesToPlayer(int targetActorNumber)
    {
        if (isSyncing)
        {
            Debug.LogWarning("[InvSync] Already syncing, queuing...");
            yield return new WaitUntil(() => !isSyncing);
        }

        isSyncing = true;

        var module = WorldDataManager.Instance.InventoryData;
        if (module == null)
        {
            Debug.LogError("[InvSync] InventoryDataModule is null");
            isSyncing = false;
            yield break;
        }

        // Collect all character IDs
        List<string> charIds = new List<string>(module.GetAllCharacterIds());

        int total = charIds.Count;
        int totalBatches = Mathf.CeilToInt((float)total / inventoriesPerBatch);
        if (totalBatches == 0) totalBatches = 1; // still send complete event

        RaiseEventOptions targetOpts = new RaiseEventOptions
        {
            TargetActors = new int[] { targetActorNumber }
        };

        for (int batchIdx = 0; batchIdx < totalBatches; batchIdx++)
        {
            int start = batchIdx * inventoriesPerBatch;
            int count = Mathf.Min(inventoriesPerBatch, total - start);

            // Serialize batch of inventories
            byte[] batchData = SerializeInventoryBatch(module, charIds, start, count);

            object[] payload = new object[] { batchIdx, totalBatches, batchData };

            PhotonNetwork.RaiseEvent(INV_SYNC_BATCH, payload, targetOpts, SendOptions.SendReliable);

            if (showDebugLogs)
                Debug.Log($"[InvSync] Sent batch {batchIdx + 1}/{totalBatches} ({count} inventories) to actor {targetActorNumber}");

            yield return new WaitForSeconds(batchDelay);
        }

        // Send completion
        PhotonNetwork.RaiseEvent(INV_SYNC_COMPLETE, total, targetOpts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[InvSync] Sync complete for actor {targetActorNumber} ({total} inventories)");

        isSyncing = false;
    }

    // ══════════════════════════════════════════════════════════
    // CLIENT: SYNC BATCH HANDLER
    // ══════════════════════════════════════════════════════════

    private void HandleSyncBatch(object data)
    {
        object[] arr = (object[])data;
        int batchIdx     = (int)arr[0];
        int totalBatches = (int)arr[1];
        byte[] batchData = (byte[])arr[2];

        DeserializeInventoryBatch(batchData);

        if (showDebugLogs)
            Debug.Log($"[InvSync] Received batch {batchIdx + 1}/{totalBatches}");
    }

    private void HandleSyncComplete(object data)
    {
        int totalInventories = (int)data;
        hasSyncedThisSession = true;

        if (showDebugLogs)
            Debug.Log($"[InvSync] ✓ Inventory sync complete! {totalInventories} inventories loaded");

        OnInventoryChanged?.Invoke();
    }

    // ══════════════════════════════════════════════════════════
    // MASTER: SLOT CHANGE & REGISTER
    // ══════════════════════════════════════════════════════════

    private void HandleSlotChangeRequest(byte[] requestData, int senderActorNumber)
    {
        // Decode request
        if (!DecodeSlotRequest(requestData, out string charId, out byte opType,
                                out byte slotIndex, out ushort itemId, out ushort quantity, out byte slotB))
        {
            Debug.LogWarning($"[InvSync] Invalid slot change request from actor {senderActorNumber}");
            return;
        }

        // ── Server-side validation ──────────────────────────
        // TODO: Add game-specific validation here:
        //   - Check if character belongs to the requesting player
        //   - Validate item existence / stack limits
        //   - Anti-cheat checks

        bool success = ApplyOperation(charId, opType, slotIndex, itemId, quantity, slotB);

        if (!success)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[InvSync] Slot change failed: char={charId} op={opType} slot={slotIndex}");
            return;
        }

        // Broadcast to all clients (including sender for confirmation)
        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(SLOT_BROADCAST, requestData, opts, SendOptions.SendReliable);

        OnInventoryChanged?.Invoke();

        if (showDebugLogs)
            Debug.Log($"[InvSync] Applied & broadcast: char={charId} op={opType} slot={slotIndex}");
    }

    private void HandleRegisterRequest(byte[] data)
    {
        if (!DecodeRegisterRequest(data, out string charId, out byte maxSlots)) return;

        MasterRegisterCharacter(charId, maxSlots);
    }

    private void MasterRegisterCharacter(string charId, byte maxSlots)
    {
        WorldDataManager.Instance.InventoryData.RegisterCharacter(charId, maxSlots);

        // Broadcast registration to all other clients
        byte[] payload = EncodeRegisterRequest(charId, maxSlots);
        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(INV_REGISTER, payload, opts, SendOptions.SendReliable);

        OnCharacterRegistered?.Invoke(charId);

        if (showDebugLogs)
            Debug.Log($"[InvSync] Master registered character '{charId}' ({maxSlots} slots)");
    }

    // ══════════════════════════════════════════════════════════
    // CLIENT: BROADCAST HANDLERS
    // ══════════════════════════════════════════════════════════

    private void HandleSlotBroadcast(byte[] data)
    {
        if (!DecodeSlotRequest(data, out string charId, out byte opType,
                                out byte slotIndex, out ushort itemId, out ushort quantity, out byte slotB))
            return;

        ApplyOperation(charId, opType, slotIndex, itemId, quantity, slotB);
        OnInventoryChanged?.Invoke();
    }

    private void HandleRegisterBroadcast(byte[] data)
    {
        if (!DecodeRegisterRequest(data, out string charId, out byte maxSlots)) return;

        WorldDataManager.Instance.InventoryData.RegisterCharacter(charId, maxSlots);
        OnCharacterRegistered?.Invoke(charId);

        if (showDebugLogs)
            Debug.Log($"[InvSync] Remote character registered: '{charId}'");
    }

    // ══════════════════════════════════════════════════════════
    // PHOTON CALLBACKS
    // ══════════════════════════════════════════════════════════

    /// <summary>Master handles new player entering.</summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (showDebugLogs)
            Debug.Log($"[InvSync] Player entered: {newPlayer.NickName} (actor {newPlayer.ActorNumber})");

        // The late-joiner will send REQUEST_INV_SYNC after initialisation.
        // We could also proactively send here, but following ChunkDataSyncManager's
        // pull-based pattern is safer (avoids race when WorldDataManager is not ready).
    }

    /// <summary>Cleanup when a player leaves.</summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Optionally: keep inventory in memory so it persists if they rejoin.
        // Uncomment below to free memory on leave:
        // string charId = !string.IsNullOrEmpty(otherPlayer.UserId) ? otherPlayer.UserId : otherPlayer.NickName;
        // WorldDataManager.Instance.InventoryData.UnregisterCharacter(charId);

        if (showDebugLogs)
            Debug.Log($"[InvSync] Player left: {otherPlayer.NickName}");
    }

    /// <summary>Handle Master Client switching.</summary>
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (showDebugLogs)
            Debug.Log($"[InvSync] Master switched to: {newMasterClient.NickName}");

        // New master already has all inventory data in memory (it was synced earlier).
        // No additional action needed — InventoryDataModule state is already local.
    }

    // ══════════════════════════════════════════════════════════
    // OPERATION APPLY (shared by Master & Client)
    // ══════════════════════════════════════════════════════════

    private bool ApplyOperation(string charId, byte opType, byte slotIndex,
                                 ushort itemId, ushort quantity, byte slotB)
    {
        var module = WorldDataManager.Instance.InventoryData;
        if (module == null) return false;

        switch (opType)
        {
            case OP_SET_SLOT:
                return module.SetSlot(charId, slotIndex, itemId, quantity);

            case OP_CLEAR_SLOT:
                return module.ClearSlot(charId, slotIndex);

            case OP_ADD_QUANTITY:
                return module.AddQuantity(charId, slotIndex, itemId, quantity);

            case OP_REMOVE_QUANTITY:
                return module.RemoveQuantity(charId, slotIndex, quantity);

            case OP_SWAP_SLOTS:
                return module.SwapSlots(charId, slotIndex, slotB);

            default:
                Debug.LogWarning($"[InvSync] Unknown operation type: {opType}");
                return false;
        }
    }

    // ══════════════════════════════════════════════════════════
    // INTERNAL: SEND SLOT REQUEST
    // ══════════════════════════════════════════════════════════

    private void SendSlotRequest(byte opType, byte slotIndex, ushort itemId, ushort quantity, byte slotB)
    {
        string charId = LocalCharacterId;
        if (string.IsNullOrEmpty(charId))
        {
            Debug.LogError("[InvSync] Cannot send request — no characterId.");
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            // Offline mode: apply directly
            ApplyOperation(charId, opType, slotIndex, itemId, quantity, slotB);
            OnInventoryChanged?.Invoke();
            return;
        }

        byte[] payload = EncodeSlotRequest(charId, opType, slotIndex, itemId, quantity, slotB);

        if (PhotonNetwork.IsMasterClient)
        {
            // Master: apply directly & broadcast
            ApplyOperation(charId, opType, slotIndex, itemId, quantity, slotB);

            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(SLOT_BROADCAST, payload, opts, SendOptions.SendReliable);

            OnInventoryChanged?.Invoke();
        }
        else
        {
            // Client: send to master for validation
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(SLOT_CHANGE_REQUEST, payload, opts, SendOptions.SendReliable);
        }
    }

    // ══════════════════════════════════════════════════════════
    // BINARY SERIALIZATION
    // ══════════════════════════════════════════════════════════

    // ── Slot request: [charIdLen(1)][charId(N)][opType(1)][slotIndex(1)][itemId(2)][quantity(2)][slotB(1)]
    //    Total: N + 8 bytes

    private static byte[] EncodeSlotRequest(string charId, byte opType, byte slotIndex,
                                             ushort itemId, ushort quantity, byte slotB)
    {
        byte[] charIdBytes = System.Text.Encoding.UTF8.GetBytes(charId ?? "");
        byte[] result = new byte[1 + charIdBytes.Length + 7];
        int o = 0;

        result[o++] = (byte)charIdBytes.Length;
        System.Buffer.BlockCopy(charIdBytes, 0, result, o, charIdBytes.Length);
        o += charIdBytes.Length;

        result[o++] = opType;
        result[o++] = slotIndex;
        result[o++] = (byte)(itemId & 0xFF);
        result[o++] = (byte)(itemId >> 8);
        result[o++] = (byte)(quantity & 0xFF);
        result[o++] = (byte)(quantity >> 8);
        result[o]   = slotB;

        return result;
    }

    private static bool DecodeSlotRequest(byte[] data, out string charId, out byte opType,
                                           out byte slotIndex, out ushort itemId,
                                           out ushort quantity, out byte slotB)
    {
        charId = null; opType = 0; slotIndex = 0; itemId = 0; quantity = 0; slotB = 0;

        if (data == null || data.Length < 8) return false;

        int o = 0;
        byte charIdLen = data[o++];
        if (data.Length < 1 + charIdLen + 7) return false;

        charId = System.Text.Encoding.UTF8.GetString(data, o, charIdLen);
        o += charIdLen;

        opType    = data[o++];
        slotIndex = data[o++];
        itemId    = (ushort)(data[o] | (data[o + 1] << 8)); o += 2;
        quantity  = (ushort)(data[o] | (data[o + 1] << 8)); o += 2;
        slotB     = data[o];

        return true;
    }

    // ── Register request: [charIdLen(1)][charId(N)][maxSlots(1)]

    private static byte[] EncodeRegisterRequest(string charId, byte maxSlots)
    {
        byte[] charIdBytes = System.Text.Encoding.UTF8.GetBytes(charId ?? "");
        byte[] result = new byte[1 + charIdBytes.Length + 1];
        result[0] = (byte)charIdBytes.Length;
        System.Buffer.BlockCopy(charIdBytes, 0, result, 1, charIdBytes.Length);
        result[1 + charIdBytes.Length] = maxSlots;
        return result;
    }

    private static bool DecodeRegisterRequest(byte[] data, out string charId, out byte maxSlots)
    {
        charId = null; maxSlots = 36;
        if (data == null || data.Length < 3) return false;

        byte len = data[0];
        if (data.Length < 1 + len + 1) return false;

        charId   = System.Text.Encoding.UTF8.GetString(data, 1, len);
        maxSlots = data[1 + len];
        return true;
    }

    // ── Inventory batch serialization (for full-sync) ────────
    // Layout: [count(2)] { [invLen(4)][invBytes(N)] } × count

    private static byte[] SerializeInventoryBatch(InventoryDataModule module,
                                                   List<string> charIds,
                                                   int start, int count)
    {
        using (var ms = new System.IO.MemoryStream())
        using (var w  = new System.IO.BinaryWriter(ms))
        {
            w.Write((ushort)count);

            for (int i = start; i < start + count && i < charIds.Count; i++)
            {
                byte[] invBytes = module.SerializeInventory(charIds[i]);

                if (invBytes == null || invBytes.Length == 0)
                {
                    w.Write(0); // zero-length inventory
                    continue;
                }

                w.Write(invBytes.Length);
                w.Write(invBytes);
            }

            return ms.ToArray();
        }
    }

    private static void DeserializeInventoryBatch(byte[] data)
    {
        if (data == null || data.Length < 2) return;

        var module = WorldDataManager.Instance.InventoryData;
        if (module == null) return;

        using (var ms = new System.IO.MemoryStream(data))
        using (var r  = new System.IO.BinaryReader(ms))
        {
            ushort count = r.ReadUInt16();

            for (int i = 0; i < count; i++)
            {
                int len = r.ReadInt32();
                if (len <= 0) continue;

                byte[] invBytes = r.ReadBytes(len);
                module.DeserializeAndLoad(invBytes);
            }
        }
    }
}
