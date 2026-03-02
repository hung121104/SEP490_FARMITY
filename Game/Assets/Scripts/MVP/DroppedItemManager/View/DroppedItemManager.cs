using UnityEngine;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central orchestrator for the Drop Item system.
/// Singleton MonoBehaviour that wires together:
///   - DroppedItemService     (in-memory registry)
///   - ChunkDataSyncManager   (Photon events — dropped item section)
///   - ChunkLoadingManager    (visibility by chunk)
///   - InventoryGameView      (add item on pickup)
///
/// Manages spawning/despawning visual prefabs, chunk-based visibility,
/// and translating sync events into concrete game actions.
///
/// Place this on a persistent GameObject in the scene (ChunkDataSyncManager
/// already handles the Photon sync).
/// </summary>
public class DroppedItemManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static DroppedItemManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Prefab")]
    [Tooltip("The DroppedItem prefab with DroppedItemView + DroppedItemPresenter.")]
    [SerializeField] private GameObject droppedItemPrefab;

    [Header("References (auto-resolved if null)")]
    [SerializeField] private DroppedItemSyncManager syncManager;
    [SerializeField] private ChunkLoadingManager chunkLoadingManager;

    [Header("Settings")]
    [Tooltip("Offset applied to player position when dropping an item.")]
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, -0.5f);

    [Tooltip("Enable debug logging.")]
    [SerializeField] private bool showDebugLogs = true;

    // ── Runtime State ─────────────────────────────────────────────────────────

    /// <summary>In-memory registry of all known dropped items.</summary>
    private IDroppedItemService _service;

    /// <summary>Active visual GameObjects keyed by dropId.</summary>
    private readonly Dictionary<string, GameObject> _activeVisuals = new();

    /// <summary>Cached reference to local player transform.</summary>
    private Transform _localPlayerTransform;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired after a dropped item visual is spawned in the world.</summary>
    public event Action<DroppedItemData> OnItemVisualSpawned;

    /// <summary>Fired after a dropped item visual is removed from the world.</summary>
    public event Action<string> OnItemVisualDespawned;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DroppedItemManager] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create the in-memory service
        _service = new DroppedItemService();
    }

    private void Start()
    {
        // Auto-resolve references if not assigned in Inspector
        if (syncManager == null)
            syncManager = FindAnyObjectByType<DroppedItemSyncManager>();
        if (chunkLoadingManager == null)
            chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        // Subscribe to sync events (dropped item events on ChunkDataSyncManager)
        if (syncManager != null)
        {
            syncManager.OnItemSpawned += HandleItemSpawned;
            syncManager.OnItemRemoved += HandleItemRemoved;
            syncManager.OnSyncBatchReceived += HandleSyncBatch;
        }
        else
        {
            Debug.LogError("[DroppedItemManager] ChunkDataSyncManager not found!");
        }

        // Subscribe to chunk events
        if (chunkLoadingManager != null)
        {
            chunkLoadingManager.OnChunkLoaded += HandleChunkLoaded;
            chunkLoadingManager.OnChunkUnloaded += HandleChunkUnloaded;
        }
        else
        {
            Debug.LogWarning("[DroppedItemManager] ChunkLoadingManager not found — chunk visibility disabled.");
        }

        // Find local player (coroutine in case player hasn't spawned yet)
        StartCoroutine(FindLocalPlayer());
    }

    private void OnDestroy()
    {
        // Unsubscribe sync events
        if (syncManager != null)
        {
            syncManager.OnItemSpawned -= HandleItemSpawned;
            syncManager.OnItemRemoved -= HandleItemRemoved;
            syncManager.OnSyncBatchReceived -= HandleSyncBatch;
        }

        // Unsubscribe chunk events
        if (chunkLoadingManager != null)
        {
            chunkLoadingManager.OnChunkLoaded -= HandleChunkLoaded;
            chunkLoadingManager.OnChunkUnloaded -= HandleChunkUnloaded;
        }

        // Cleanup visuals
        foreach (var kvp in _activeVisuals)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        _activeVisuals.Clear();
        _service?.Clear();

        if (Instance == this) Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by InventoryGameView.HandleItemDropped() to request dropping an item.
    /// Creates a DroppedItemData and sends a drop request through Photon.
    /// </summary>
    /// <param name="item">The ItemModel being dropped from inventory.</param>
    public void RequestDropItem(ItemModel item)
    {
        if (item == null)
        {
            Debug.LogWarning("[DroppedItemManager] Cannot drop null item.");
            return;
        }

        if (_localPlayerTransform == null)
        {
            Debug.LogWarning("[DroppedItemManager] Local player not found, cannot drop item.");
            return;
        }

        // Calculate drop position
        Vector3 playerPos = _localPlayerTransform.position;
        float worldX = playerPos.x + dropOffset.x;
        float worldY = playerPos.y + dropOffset.y;

        // Build DroppedItemData from ItemModel using the factory method
        DroppedItemData data = DroppedItemData.FromItemModel(item, worldX, worldY);

        // NOTE: InventoryPresenter.HandleDropItem removes exactly 1 item per drop action,
        // but the ItemModel passed still carries the full stack quantity.
        // Override quantity to 1 to match what was actually removed from inventory.
        data.quantity = 1;

        // Fill in chunk coordinates from WorldDataManager
        if (WorldDataManager.Instance != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(new Vector3(worldX, worldY, 0f));
            data.chunkX = chunkPos.x;
            data.chunkY = chunkPos.y;
        }

        // Fill in room name
        if (PhotonNetwork.CurrentRoom != null)
        {
            data.roomName = PhotonNetwork.CurrentRoom.Name;
        }

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManager] Requesting drop: {data.itemName} x{data.quantity} at ({worldX:F1}, {worldY:F1})");

        // Send through Photon — Master will assign final dropId, persist, and broadcast
        syncManager?.SendDropRequest(data);
    }

    /// <summary>
    /// Called by DroppedItemPresenter when the local player presses the pickup key.
    /// Sends a pickup request through Photon.
    /// </summary>
    /// <param name="dropId">The unique ID of the item to pick up.</param>
    public void RequestPickupItem(string dropId)
    {
        if (string.IsNullOrEmpty(dropId))
        {
            Debug.LogWarning("[DroppedItemManager] Cannot pick up item with null/empty dropId.");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManager] Requesting pickup: {dropId}");

        syncManager?.SendPickupRequest(dropId);
    }

    /// <summary>Check if a dropped item exists in the registry.</summary>
    public bool HasDroppedItem(string dropId) => _service.HasItem(dropId);

    /// <summary>Get all dropped items currently in the registry.</summary>
    public IReadOnlyCollection<DroppedItemData> GetAllDroppedItems() => _service.GetAllItems();

    /// <summary>
    /// Rebuild the in-memory registry and visuals from a database snapshot.
    /// Called by ChunkDataSyncManager when this client becomes the new Master.
    /// </summary>
    public void RebuildFromDatabase(DroppedItemData[] items)
    {
        // Clear existing state
        foreach (var kvp in _activeVisuals)
        {
            if (kvp.Value != null)
            {
                var presenter = kvp.Value.GetComponent<DroppedItemPresenter>();
                presenter?.Cleanup();
                Destroy(kvp.Value);
            }
        }
        _activeVisuals.Clear();
        _service.Clear();

        // Re-register and spawn visuals
        foreach (var data in items)
        {
            if (data == null || data.IsExpired) continue;
            _service.RegisterItem(data);

            Vector2Int chunk = new Vector2Int(data.chunkX, data.chunkY);
            if (IsChunkLoaded(chunk))
            {
                SpawnItemVisual(data);
            }
        }

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManager] Rebuilt from DB: {items.Length} items, {_service.Count} active");
    }

    // ── Sync Event Handlers ───────────────────────────────────────────────────

    /// <summary>
    /// Called when Master confirms an item was dropped (event 121 ITEM_SPAWNED).
    /// Registers in service and spawns visual if chunk is loaded.
    /// </summary>
    private void HandleItemSpawned(DroppedItemData data)
    {
        if (data == null) return;

        // Register in service
        _service.RegisterItem(data);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManager] Item spawned: {data.itemName} ({data.dropId}) at ({data.worldX:F1}, {data.worldY:F1})");

        // Spawn visual if the chunk is currently loaded/visible
        Vector2Int chunk = new Vector2Int(data.chunkX, data.chunkY);
        if (IsChunkLoaded(chunk))
        {
            SpawnItemVisual(data);
        }
    }

    /// <summary>
    /// Called when Master confirms an item was removed (event 123 ITEM_REMOVED or 126 DESPAWN).
    /// Removes from service, despawns visual, and adds to inventory if picked up by local player.
    /// </summary>
    /// <param name="dropId">The ID of the removed item.</param>
    /// <param name="pickedByActorNumber">ActorNumber of the player who picked it up, or 0 if TTL expired.</param>
    private void HandleItemRemoved(string dropId, int pickedByActorNumber)
    {
        // Get item data before removing (needed for inventory add)
        DroppedItemData data = _service.GetItem(dropId);

        // Unregister from service
        _service.UnregisterItem(dropId);

        // Despawn visual
        DespawnItemVisual(dropId);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManager] Item removed: {dropId}, pickedBy: {pickedByActorNumber}");

        // If this local player picked it up, add to inventory
        if (pickedByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber && data != null)
        {
            AddItemToInventory(data);
        }
    }

    /// <summary>
    /// Called during late-join sync (event 125 ITEM_SYNC_BATCH).
    /// Registers all items and spawns visuals for loaded chunks.
    /// </summary>
    private void HandleSyncBatch(DroppedItemData[] items)
    {
        if (items == null) return;

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManager] Sync batch received: {items.Length} items");

        foreach (var data in items)
        {
            if (data == null) continue;

            // Register (skip duplicates)
            if (_service.HasItem(data.dropId)) continue;
            _service.RegisterItem(data);

            // Spawn visual if chunk is loaded
            Vector2Int chunk = new Vector2Int(data.chunkX, data.chunkY);
            if (IsChunkLoaded(chunk))
            {
                SpawnItemVisual(data);
            }
        }
    }

    // ── Chunk Event Handlers ──────────────────────────────────────────────────

    /// <summary>
    /// When a chunk is loaded, spawn visuals for all dropped items in that chunk.
    /// </summary>
    private void HandleChunkLoaded(Vector2Int chunkPos)
    {
        var items = _service.GetItemsInChunk(chunkPos);
        foreach (var data in items)
        {
            if (!_activeVisuals.ContainsKey(data.dropId))
            {
                SpawnItemVisual(data);
            }
        }

        if (showDebugLogs && items.Count > 0)
            Debug.Log($"[DroppedItemManager] Chunk ({chunkPos.x}, {chunkPos.y}) loaded — spawned {items.Count} dropped items.");
    }

    /// <summary>
    /// When a chunk is unloaded, despawn visuals for all dropped items in that chunk.
    /// Data stays in the service registry.
    /// </summary>
    private void HandleChunkUnloaded(Vector2Int chunkPos)
    {
        var items = _service.GetItemsInChunk(chunkPos);
        int count = 0;
        foreach (var data in items)
        {
            if (_activeVisuals.ContainsKey(data.dropId))
            {
                DespawnItemVisual(data.dropId);
                count++;
            }
        }

        if (showDebugLogs && count > 0)
            Debug.Log($"[DroppedItemManager] Chunk ({chunkPos.x}, {chunkPos.y}) unloaded — despawned {count} dropped items.");
    }

    // ── Visual Spawn / Despawn ────────────────────────────────────────────────

    /// <summary>
    /// Instantiate the DroppedItem prefab and initialize its Presenter + View.
    /// </summary>
    private void SpawnItemVisual(DroppedItemData data)
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogError("[DroppedItemManager] droppedItemPrefab is not assigned!");
            return;
        }

        if (_activeVisuals.ContainsKey(data.dropId))
        {
            // Already spawned
            return;
        }

        Vector3 spawnPos = new Vector3(data.worldX, data.worldY, 0f);
        GameObject go = Instantiate(droppedItemPrefab, spawnPos, Quaternion.identity);
        go.name = $"DroppedItem_{data.itemName}_{data.dropId[..Mathf.Min(8, data.dropId.Length)]}";

        // Initialize Presenter → View (add missing components at runtime if needed)
        var view = go.GetComponent<DroppedItemView>();
        if (view == null)
        {
            Debug.LogError($"[DroppedItemManager] Prefab missing DroppedItemView! Cannot spawn item.");
            Destroy(go);
            return;
        }

        var presenter = go.GetComponent<DroppedItemPresenter>();
        if (presenter == null)
        {
            presenter = go.AddComponent<DroppedItemPresenter>();
            Debug.LogWarning("[DroppedItemManager] DroppedItemPresenter was missing on prefab — added at runtime. " +
                             "Consider adding it to the prefab in the Unity Editor.");
        }

        presenter.Initialize(data, view);

        _activeVisuals[data.dropId] = go;
        OnItemVisualSpawned?.Invoke(data);
    }

    /// <summary>
    /// Destroy the visual GameObject for a dropped item.
    /// </summary>
    private void DespawnItemVisual(string dropId)
    {
        if (_activeVisuals.TryGetValue(dropId, out GameObject go))
        {
            _activeVisuals.Remove(dropId);

            if (go != null)
            {
                var presenter = go.GetComponent<DroppedItemPresenter>();
                presenter?.Cleanup();
                Destroy(go);
            }

            OnItemVisualDespawned?.Invoke(dropId);
        }
    }

    // ── Inventory Integration ─────────────────────────────────────────────────

    /// <summary>
    /// Add the picked-up item back into the local player's inventory.
    /// Uses InventoryGameView.AddItem() which is the existing public API.
    /// </summary>
    private void AddItemToInventory(DroppedItemData data)
    {
        var inventoryGameView = FindAnyObjectByType<InventoryGameView>();
        if (inventoryGameView == null)
        {
            Debug.LogError("[DroppedItemManager] InventoryGameView not found — cannot add picked-up item!");
            return;
        }

        bool added = inventoryGameView.AddItem(data.itemId, data.quantity, data.quality);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManager] Added to inventory: {data.itemName} x{data.quantity} (quality={data.quality}) — success={added}");

        if (!added)
        {
            Debug.LogWarning($"[DroppedItemManager] Inventory full! Could not add {data.itemName} x{data.quantity}. " +
                             "Item was already removed from world — consider re-dropping.");
            // TODO: Optionally re-drop the item if inventory is full
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Check if a chunk is currently loaded in ChunkLoadingManager.
    /// Falls back to true if ChunkLoadingManager is unavailable (show all items).
    /// </summary>
    private bool IsChunkLoaded(Vector2Int chunkPos)
    {
        if (chunkLoadingManager == null) return true;
        return chunkLoadingManager.IsChunkLoaded(chunkPos);
    }

    /// <summary>
    /// Coroutine to find the local player transform. Retries until found.
    /// Same pattern as ChunkLoadingManager.FindLocalPlayer().
    /// </summary>
    private IEnumerator FindLocalPlayer()
    {
        while (_localPlayerTransform == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
            foreach (GameObject player in players)
            {
                PhotonView pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    _localPlayerTransform = player.transform;
                    if (showDebugLogs)
                        Debug.Log($"[DroppedItemManager] Found local player: {player.name}");
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
