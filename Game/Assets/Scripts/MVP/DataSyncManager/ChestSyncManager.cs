using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Bridges ChestDataModule ↔ Photon network.
/// Mirrors InventorySyncManager but operates on WorldDataManager.ChestData.
///
/// Master Authority:
///   Master holds the authoritative chest data in WorldDataManager.ChestData.
///   Clients send change requests → Master validates, applies, broadcasts.
///
/// Event Codes: 150–157 (reserved for chest)
///   150 = REQUEST_CHEST_SYNC     Client → Master (late join)
///   151 = CHEST_SYNC_BATCH       Master → Client (full chest data batch)
///   152 = CHEST_SYNC_COMPLETE    Master → Client (all batches sent)
///   153 = CHEST_SLOT_REQUEST     Client → Master (single slot change)
///   154 = CHEST_SLOT_BROADCAST   Master → All    (slot change confirmed)
///   155 = CHEST_REGISTER         Master → All    (new chest registered)
///   156 = CHEST_OPEN_NOTIFY      Client → All    (player opened chest)
///   157 = CHEST_CLOSE_NOTIFY     Client → All    (player closed chest)
/// </summary>
public class ChestSyncManager : MonoBehaviourPunCallbacks
{
    // ── Singleton ────────────────────────────────────────────
    public static ChestSyncManager Instance { get; private set; }

    // ── Event Codes ──────────────────────────────────────────
    private const byte REQUEST_CHEST_SYNC   = 150;
    private const byte CHEST_SYNC_BATCH     = 151;
    private const byte CHEST_SYNC_COMPLETE  = 152;
    private const byte CHEST_SLOT_REQUEST   = 153;
    private const byte CHEST_SLOT_BROADCAST = 154;
    private const byte CHEST_REGISTER       = 155;
    private const byte CHEST_OPEN_NOTIFY    = 156;
    private const byte CHEST_CLOSE_NOTIFY   = 157;

    // ── Delta Operation Types ────────────────────────────────
    private const byte OP_SET_SLOT   = 0;
    private const byte OP_CLEAR_SLOT = 1;
    private const byte OP_SWAP_SLOTS = 2;

    // ── Settings ─────────────────────────────────────────────
    [Header("Sync Settings")]
    [SerializeField] private int chestsPerBatch = 5;
    [SerializeField] private float batchDelay = 0.3f;
    [SerializeField] private bool showDebugLogs = true;

    // ── State ────────────────────────────────────────────────
    private bool isSyncing = false;
    private bool hasSyncedThisSession = false;

    // ── Events (UI subscribes here) ──────────────────────────
    public static event System.Action<string> OnChestChanged;
    public static event System.Action<string, int> OnChestOpened;
    public static event System.Action<string, int> OnChestClosed;

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
            if (showDebugLogs) Debug.LogWarning("[ChestSync] Not connected to Photon");
            return;
        }

        Invoke(nameof(RequestChestSync), 1.5f);
    }

    // ══════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════

    public void RegisterChest(string chestId, byte slotCount)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            MasterRegisterChest(chestId, slotCount);
        }
        else
        {
            byte[] payload = EncodeRegisterRequest(chestId, slotCount);
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(CHEST_REGISTER, payload, opts, SendOptions.SendReliable);
        }
    }

    public void RequestSetSlot(string chestId, byte slotIndex, string itemId, ushort quantity)
    {
        SendSlotRequest(OP_SET_SLOT, chestId, slotIndex, itemId, quantity, 0);
    }

    public void RequestClearSlot(string chestId, byte slotIndex)
    {
        SendSlotRequest(OP_CLEAR_SLOT, chestId, slotIndex, null, 0, 0);
    }

    public void RequestSwapSlots(string chestId, byte slotA, byte slotB)
    {
        SendSlotRequest(OP_SWAP_SLOTS, chestId, slotA, null, 0, slotB);
    }

    public void NotifyChestOpened(string chestId)
    {
        byte[] payload = EncodeNotify(chestId, PhotonNetwork.LocalPlayer.ActorNumber);
        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(CHEST_OPEN_NOTIFY, payload, opts, SendOptions.SendReliable);
    }

    public void NotifyChestClosed(string chestId)
    {
        byte[] payload = EncodeNotify(chestId, PhotonNetwork.LocalPlayer.ActorNumber);
        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(CHEST_CLOSE_NOTIFY, payload, opts, SendOptions.SendReliable);
    }

    // ══════════════════════════════════════════════════════════
    // LATE-JOIN SYNC
    // ══════════════════════════════════════════════════════════

    private void RequestChestSync()
    {
        if (hasSyncedThisSession) return;

        if (PhotonNetwork.IsMasterClient)
        {
            hasSyncedThisSession = true;
            return;
        }

        if (!WorldDataManager.Instance.IsInitialized)
        {
            Invoke(nameof(RequestChestSync), 1f);
            return;
        }

        if (showDebugLogs) Debug.Log("[ChestSync] Requesting chest sync from master...");

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(REQUEST_CHEST_SYNC, PhotonNetwork.LocalPlayer.ActorNumber, opts, SendOptions.SendReliable);
    }

    // ══════════════════════════════════════════════════════════
    // PHOTON EVENT DISPATCHER
    // ══════════════════════════════════════════════════════════

    private void OnPhotonEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            // Master receives
            case REQUEST_CHEST_SYNC when PhotonNetwork.IsMasterClient:
                HandleSyncRequest((int)photonEvent.CustomData);
                break;

            case CHEST_SLOT_REQUEST when PhotonNetwork.IsMasterClient:
                HandleSlotChangeRequest((byte[])photonEvent.CustomData);
                break;

            case CHEST_REGISTER when PhotonNetwork.IsMasterClient:
                HandleRegisterRequest((byte[])photonEvent.CustomData);
                break;

            // Client receives
            case CHEST_SYNC_BATCH when !PhotonNetwork.IsMasterClient:
                HandleSyncBatch(photonEvent.CustomData);
                break;

            case CHEST_SYNC_COMPLETE when !PhotonNetwork.IsMasterClient:
                HandleSyncComplete(photonEvent.CustomData);
                break;

            case CHEST_SLOT_BROADCAST:
                HandleSlotBroadcast((byte[])photonEvent.CustomData);
                break;

            case CHEST_REGISTER when !PhotonNetwork.IsMasterClient:
                HandleRegisterBroadcast((byte[])photonEvent.CustomData);
                break;

            // Open/Close notifications (all clients)
            case CHEST_OPEN_NOTIFY:
                HandleOpenNotify((byte[])photonEvent.CustomData);
                break;

            case CHEST_CLOSE_NOTIFY:
                HandleCloseNotify((byte[])photonEvent.CustomData);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════
    // MASTER: SYNC REQUEST HANDLER
    // ══════════════════════════════════════════════════════════

    private void HandleSyncRequest(int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (showDebugLogs) Debug.Log($"[ChestSync] Master received sync request from actor {requestingActorNumber}");
        StartCoroutine(SendAllChestsToPlayer(requestingActorNumber));
    }

    private IEnumerator SendAllChestsToPlayer(int targetActorNumber)
    {
        if (isSyncing) yield return new WaitUntil(() => !isSyncing);
        isSyncing = true;

        var module = WorldDataManager.Instance.ChestData;
        if (module == null)
        {
            isSyncing = false;
            yield break;
        }

        List<string> chestIds = new List<string>(module.GetAllChestIds());
        int total = chestIds.Count;
        int totalBatches = Mathf.CeilToInt((float)total / chestsPerBatch);
        if (totalBatches == 0) totalBatches = 1;

        RaiseEventOptions targetOpts = new RaiseEventOptions
        {
            TargetActors = new int[] { targetActorNumber }
        };

        for (int batchIdx = 0; batchIdx < totalBatches; batchIdx++)
        {
            int start = batchIdx * chestsPerBatch;
            int count = Mathf.Min(chestsPerBatch, total - start);

            byte[] batchData = SerializeChestBatch(module, chestIds, start, count);
            object[] payload = new object[] { batchIdx, totalBatches, batchData };

            PhotonNetwork.RaiseEvent(CHEST_SYNC_BATCH, payload, targetOpts, SendOptions.SendReliable);

            if (showDebugLogs)
                Debug.Log($"[ChestSync] Sent batch {batchIdx + 1}/{totalBatches} to actor {targetActorNumber}");

            yield return new WaitForSeconds(batchDelay);
        }

        PhotonNetwork.RaiseEvent(CHEST_SYNC_COMPLETE, total, targetOpts, SendOptions.SendReliable);
        isSyncing = false;
    }

    // ══════════════════════════════════════════════════════════
    // CLIENT: SYNC BATCH HANDLER
    // ══════════════════════════════════════════════════════════

    private void HandleSyncBatch(object data)
    {
        object[] arr = (object[])data;
        byte[] batchData = (byte[])arr[2];
        DeserializeChestBatch(batchData);
    }

    private void HandleSyncComplete(object data)
    {
        int totalChests = (int)data;
        hasSyncedThisSession = true;

        if (showDebugLogs)
            Debug.Log($"[ChestSync] Sync complete! {totalChests} chests loaded");

        // Fire change for all chests so open UI refreshes
        var module = WorldDataManager.Instance?.ChestData;
        if (module != null)
        {
            foreach (var chestId in module.GetAllChestIds())
                OnChestChanged?.Invoke(chestId);
        }
    }

    // ══════════════════════════════════════════════════════════
    // MASTER: SLOT CHANGE & REGISTER
    // ══════════════════════════════════════════════════════════

    private void HandleSlotChangeRequest(byte[] requestData)
    {
        if (!DecodeSlotRequest(requestData, out string chestId, out byte opType,
                               out byte slotIndex, out string itemId, out ushort quantity, out byte slotB))
            return;

        bool success = ApplyOperation(chestId, opType, slotIndex, itemId, quantity, slotB);
        if (!success) return;

        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(CHEST_SLOT_BROADCAST, requestData, opts, SendOptions.SendReliable);

        OnChestChanged?.Invoke(chestId);
    }

    private void HandleRegisterRequest(byte[] data)
    {
        if (!DecodeRegisterRequest(data, out string chestId, out byte slotCount)) return;
        MasterRegisterChest(chestId, slotCount);
    }

    private void MasterRegisterChest(string chestId, byte slotCount)
    {
        WorldDataManager.Instance.ChestData.RegisterChest(chestId, slotCount);

        byte[] payload = EncodeRegisterRequest(chestId, slotCount);
        RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(CHEST_REGISTER, payload, opts, SendOptions.SendReliable);

        if (showDebugLogs)
            Debug.Log($"[ChestSync] Master registered chest '{chestId}' ({slotCount} slots)");
    }

    // ══════════════════════════════════════════════════════════
    // CLIENT: BROADCAST HANDLERS
    // ══════════════════════════════════════════════════════════

    private void HandleSlotBroadcast(byte[] data)
    {
        if (!DecodeSlotRequest(data, out string chestId, out byte opType,
                               out byte slotIndex, out string itemId, out ushort quantity, out byte slotB))
            return;

        ApplyOperation(chestId, opType, slotIndex, itemId, quantity, slotB);
        OnChestChanged?.Invoke(chestId);
    }

    private void HandleRegisterBroadcast(byte[] data)
    {
        if (!DecodeRegisterRequest(data, out string chestId, out byte slotCount)) return;
        WorldDataManager.Instance.ChestData.RegisterChest(chestId, slotCount);
    }

    private void HandleOpenNotify(byte[] data)
    {
        if (!DecodeNotify(data, out string chestId, out int actorNumber)) return;
        OnChestOpened?.Invoke(chestId, actorNumber);
        if (showDebugLogs)
            Debug.Log($"[ChestSync] Actor {actorNumber} opened chest '{chestId}'");
    }

    private void HandleCloseNotify(byte[] data)
    {
        if (!DecodeNotify(data, out string chestId, out int actorNumber)) return;
        OnChestClosed?.Invoke(chestId, actorNumber);
        if (showDebugLogs)
            Debug.Log($"[ChestSync] Actor {actorNumber} closed chest '{chestId}'");
    }

    // ══════════════════════════════════════════════════════════
    // OPERATION APPLY (shared by Master & Client)
    // ══════════════════════════════════════════════════════════

    private bool ApplyOperation(string chestId, byte opType, byte slotIndex,
                                string itemId, ushort quantity, byte slotB)
    {
        var module = WorldDataManager.Instance?.ChestData;
        if (module == null) return false;

        switch (opType)
        {
            case OP_SET_SLOT:
                return module.SetSlot(chestId, slotIndex, itemId, quantity);
            case OP_CLEAR_SLOT:
                return module.ClearSlot(chestId, slotIndex);
            case OP_SWAP_SLOTS:
                return module.SwapSlots(chestId, slotIndex, slotB);
            default:
                Debug.LogWarning($"[ChestSync] Unknown operation type: {opType}");
                return false;
        }
    }

    // ══════════════════════════════════════════════════════════
    // INTERNAL: SEND SLOT REQUEST
    // ══════════════════════════════════════════════════════════

    private void SendSlotRequest(byte opType, string chestId, byte slotIndex,
                                 string itemId, ushort quantity, byte slotB)
    {
        if (string.IsNullOrEmpty(chestId)) return;

        byte[] payload = EncodeSlotRequest(chestId, opType, slotIndex, itemId, quantity, slotB);

        if (!PhotonNetwork.IsConnected)
        {
            ApplyOperation(chestId, opType, slotIndex, itemId, quantity, slotB);
            OnChestChanged?.Invoke(chestId);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            ApplyOperation(chestId, opType, slotIndex, itemId, quantity, slotB);
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(CHEST_SLOT_BROADCAST, payload, opts, SendOptions.SendReliable);
            OnChestChanged?.Invoke(chestId);
        }
        else
        {
            RaiseEventOptions opts = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
            PhotonNetwork.RaiseEvent(CHEST_SLOT_REQUEST, payload, opts, SendOptions.SendReliable);
        }
    }

    // ══════════════════════════════════════════════════════════
    // BINARY SERIALIZATION
    // ══════════════════════════════════════════════════════════

    // Slot request: [chestIdLen(1)][chestId(N)][opType(1)][slotIndex(1)][itemIdLen(1)][itemId(M)][quantity(2)][slotB(1)]

    private static byte[] EncodeSlotRequest(string chestId, byte opType, byte slotIndex,
                                            string itemId, ushort quantity, byte slotB)
    {
        byte[] chestIdBytes = System.Text.Encoding.UTF8.GetBytes(chestId ?? "");
        byte[] itemIdBytes = System.Text.Encoding.UTF8.GetBytes(itemId ?? "");

        byte[] result = new byte[1 + chestIdBytes.Length + 1 + 1 + 1 + itemIdBytes.Length + 2 + 1];
        int o = 0;

        result[o++] = (byte)chestIdBytes.Length;
        System.Buffer.BlockCopy(chestIdBytes, 0, result, o, chestIdBytes.Length);
        o += chestIdBytes.Length;

        result[o++] = opType;
        result[o++] = slotIndex;

        result[o++] = (byte)itemIdBytes.Length;
        System.Buffer.BlockCopy(itemIdBytes, 0, result, o, itemIdBytes.Length);
        o += itemIdBytes.Length;

        result[o++] = (byte)(quantity & 0xFF);
        result[o++] = (byte)(quantity >> 8);
        result[o] = slotB;

        return result;
    }

    private static bool DecodeSlotRequest(byte[] data, out string chestId, out byte opType,
                                          out byte slotIndex, out string itemId,
                                          out ushort quantity, out byte slotB)
    {
        chestId = null; opType = 0; slotIndex = 0; itemId = null; quantity = 0; slotB = 0;
        if (data == null || data.Length < 8) return false;

        int o = 0;
        byte chestIdLen = data[o++];
        if (data.Length < 1 + chestIdLen + 6) return false;

        chestId = System.Text.Encoding.UTF8.GetString(data, o, chestIdLen);
        o += chestIdLen;

        opType = data[o++];
        slotIndex = data[o++];

        byte itemIdLen = data[o++];
        if (data.Length < o + itemIdLen + 3) return false;
        itemId = System.Text.Encoding.UTF8.GetString(data, o, itemIdLen);
        o += itemIdLen;

        quantity = (ushort)(data[o] | (data[o + 1] << 8)); o += 2;
        slotB = data[o];

        return true;
    }

    // Register: [chestIdLen(1)][chestId(N)][slotCount(1)]

    private static byte[] EncodeRegisterRequest(string chestId, byte slotCount)
    {
        byte[] chestIdBytes = System.Text.Encoding.UTF8.GetBytes(chestId ?? "");
        byte[] result = new byte[1 + chestIdBytes.Length + 1];
        result[0] = (byte)chestIdBytes.Length;
        System.Buffer.BlockCopy(chestIdBytes, 0, result, 1, chestIdBytes.Length);
        result[1 + chestIdBytes.Length] = slotCount;
        return result;
    }

    private static bool DecodeRegisterRequest(byte[] data, out string chestId, out byte slotCount)
    {
        chestId = null; slotCount = 18;
        if (data == null || data.Length < 3) return false;

        byte len = data[0];
        if (data.Length < 1 + len + 1) return false;

        chestId = System.Text.Encoding.UTF8.GetString(data, 1, len);
        slotCount = data[1 + len];
        return true;
    }

    // Notify: [chestIdLen(1)][chestId(N)][actorNumber(4)]

    private static byte[] EncodeNotify(string chestId, int actorNumber)
    {
        byte[] chestIdBytes = System.Text.Encoding.UTF8.GetBytes(chestId ?? "");
        byte[] result = new byte[1 + chestIdBytes.Length + 4];
        int o = 0;

        result[o++] = (byte)chestIdBytes.Length;
        System.Buffer.BlockCopy(chestIdBytes, 0, result, o, chestIdBytes.Length);
        o += chestIdBytes.Length;

        result[o++] = (byte)(actorNumber & 0xFF);
        result[o++] = (byte)((actorNumber >> 8) & 0xFF);
        result[o++] = (byte)((actorNumber >> 16) & 0xFF);
        result[o] = (byte)((actorNumber >> 24) & 0xFF);

        return result;
    }

    private static bool DecodeNotify(byte[] data, out string chestId, out int actorNumber)
    {
        chestId = null; actorNumber = 0;
        if (data == null || data.Length < 6) return false;

        int o = 0;
        byte len = data[o++];
        if (data.Length < 1 + len + 4) return false;

        chestId = System.Text.Encoding.UTF8.GetString(data, o, len);
        o += len;

        actorNumber = data[o] | (data[o + 1] << 8) | (data[o + 2] << 16) | (data[o + 3] << 24);
        return true;
    }

    // Chest batch: [count(2)] { [chestLen(4)][chestBytes(N)] } × count

    private static byte[] SerializeChestBatch(ChestDataModule module, List<string> chestIds,
                                              int start, int count)
    {
        using (var ms = new System.IO.MemoryStream())
        using (var w = new System.IO.BinaryWriter(ms))
        {
            w.Write((ushort)count);

            for (int i = start; i < start + count && i < chestIds.Count; i++)
            {
                byte[] chestBytes = module.SerializeChest(chestIds[i]);
                if (chestBytes == null || chestBytes.Length == 0)
                {
                    w.Write(0);
                    continue;
                }
                w.Write(chestBytes.Length);
                w.Write(chestBytes);
            }

            return ms.ToArray();
        }
    }

    private static void DeserializeChestBatch(byte[] data)
    {
        if (data == null || data.Length < 2) return;

        var module = WorldDataManager.Instance?.ChestData;
        if (module == null) return;

        using (var ms = new System.IO.MemoryStream(data))
        using (var r = new System.IO.BinaryReader(ms))
        {
            ushort count = r.ReadUInt16();

            for (int i = 0; i < count; i++)
            {
                int len = r.ReadInt32();
                if (len <= 0) continue;

                byte[] chestBytes = r.ReadBytes(len);
                module.DeserializeAndLoad(chestBytes);
            }
        }
    }
}
