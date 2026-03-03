using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Presenter for the Dropped Item system following MVP pattern.
/// Coordinates between DroppedItemManagerView (View) and DroppedItemManagerService (Service).
/// 
/// Responsibilities:
///   - Route sync events (from DroppedItemSyncManager) to service + view
///   - Route chunk events (from ChunkLoadingManager) to view
///   - Handle drop/pickup request flow
///   - Decide when to spawn/despawn visuals based on chunk state
///   - Trigger inventory integration for local player pickups
///
/// All Unity-specific operations (Instantiate, Destroy, FindObject) are in the View.
/// All business logic (data creation, registry) is in the Service.
/// </summary>
public class DroppedItemPresenter
{
    private readonly IDroppedItemService service;
    private readonly DroppedItemSyncManager syncManager;
    private readonly ChunkLoadingManager chunkLoadingManager;
    private readonly bool showDebugLogs;

    // ── Events (View subscribes to these) ─────────────────────

    /// <summary>Request the View to spawn a visual for this dropped item.</summary>
    public event Action<DroppedItemData> OnSpawnVisualRequested;

    /// <summary>Request the View to despawn the visual for this dropId.</summary>
    public event Action<string> OnDespawnVisualRequested;

    /// <summary>Request the View to add a picked-up item to the local player's inventory.</summary>
    public event Action<DroppedItemData> OnAddToInventoryRequested;

    /// <summary>Request the View to clear all active visuals (used during rebuild).</summary>
    public event Action OnClearAllVisualsRequested;

    // ── Constructor ───────────────────────────────────────────

    public DroppedItemPresenter(
        IDroppedItemService service,
        DroppedItemSyncManager syncManager,
        ChunkLoadingManager chunkLoadingManager,
        bool showDebugLogs = true)
    {
        this.service = service;
        this.syncManager = syncManager;
        this.chunkLoadingManager = chunkLoadingManager;
        this.showDebugLogs = showDebugLogs;

        if (service == null)
            Debug.LogError("[DroppedItemPresenter] IDroppedItemService is null!");
    }

    // ── Event Subscriptions ───────────────────────────────────

    /// <summary>Subscribe to DroppedItemSyncManager events.</summary>
    public void SubscribeToSyncEvents()
    {
        if (syncManager != null)
        {
            syncManager.OnItemSpawned += HandleItemSpawned;
            syncManager.OnItemRemoved += HandleItemRemoved;
            syncManager.OnSyncBatchReceived += HandleSyncBatch;
        }
        else
        {
            Debug.LogError("[DroppedItemPresenter] DroppedItemSyncManager is null — cannot subscribe to sync events.");
        }
    }

    /// <summary>Unsubscribe from DroppedItemSyncManager events.</summary>
    public void UnsubscribeFromSyncEvents()
    {
        if (syncManager != null)
        {
            syncManager.OnItemSpawned -= HandleItemSpawned;
            syncManager.OnItemRemoved -= HandleItemRemoved;
            syncManager.OnSyncBatchReceived -= HandleSyncBatch;
        }
    }

    /// <summary>Subscribe to ChunkLoadingManager events for chunk-based visibility.</summary>
    public void SubscribeToChunkEvents()
    {
        if (chunkLoadingManager != null)
        {
            chunkLoadingManager.OnChunkLoaded += HandleChunkLoaded;
            chunkLoadingManager.OnChunkUnloaded += HandleChunkUnloaded;
        }
        else
        {
            Debug.LogWarning("[DroppedItemPresenter] ChunkLoadingManager not found — chunk visibility disabled.");
        }
    }

    /// <summary>Unsubscribe from ChunkLoadingManager events.</summary>
    public void UnsubscribeFromChunkEvents()
    {
        if (chunkLoadingManager != null)
        {
            chunkLoadingManager.OnChunkLoaded -= HandleChunkLoaded;
            chunkLoadingManager.OnChunkUnloaded -= HandleChunkUnloaded;
        }
    }

    // ── Public API (called by View) ───────────────────────────

    /// <summary>
    /// Handle a drop request from the inventory.
    /// Creates DroppedItemData via service and sends through Photon sync.
    /// </summary>
    /// <param name="item">The ItemModel being dropped.</param>
    /// <param name="playerPosition">Current world position of the local player.</param>
    /// <param name="dropOffset">Offset applied to player position.</param>
    public void RequestDropItem(ItemModel item, Vector3 playerPosition, Vector2 dropOffset)
    {
        if (item == null)
        {
            Debug.LogWarning("[DroppedItemPresenter] Cannot drop null item.");
            return;
        }

        DroppedItemData data = service.CreateDroppedItemData(item, playerPosition, dropOffset);
        if (data == null) return;

        if (showDebugLogs)
            Debug.Log($"[DroppedItemPresenter] Requesting drop: {data.itemName} at ({data.worldX:F1}, {data.worldY:F1})");

        // Send through Photon — Master will assign dropId, persist, and broadcast
        syncManager?.SendDropRequest(data);
    }

    /// <summary>
    /// Handle a pickup request from DroppedItemView.
    /// Sends pickup request through Photon sync for Master validation.
    /// </summary>
    /// <param name="dropId">The unique drop ID to pick up.</param>
    public void RequestPickupItem(string dropId)
    {
        if (string.IsNullOrEmpty(dropId))
        {
            Debug.LogWarning("[DroppedItemPresenter] Cannot pick up item with null/empty dropId.");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[DroppedItemPresenter] Requesting pickup: {dropId}");

        syncManager?.SendPickupRequest(dropId);
    }

    /// <summary>Check if a dropped item exists in the registry.</summary>
    public bool HasDroppedItem(string dropId) => service.HasItem(dropId);

    /// <summary>Get all dropped items currently in the registry.</summary>
    public IReadOnlyCollection<DroppedItemData> GetAllDroppedItems()
    {
        return service.GetAllItems().AsReadOnly();
    }

    /// <summary>
    /// Rebuild registry and visuals from a database snapshot.
    /// Called when this client becomes the new Master (OnMasterClientSwitched).
    /// </summary>
    /// <param name="items">Items loaded from MongoDB.</param>
    public void RebuildFromDatabase(DroppedItemData[] items)
    {
        // Clear all existing visuals and registry
        OnClearAllVisualsRequested?.Invoke();
        service.Clear();

        int activeCount = 0;
        foreach (var data in items)
        {
            if (data == null || data.IsExpired) continue;
            service.RegisterItem(data);
            activeCount++;

            // Only spawn visual if the item's chunk is currently loaded
            Vector2Int chunk = new Vector2Int(data.chunkX, data.chunkY);
            if (IsChunkLoaded(chunk))
            {
                OnSpawnVisualRequested?.Invoke(data);
            }
        }

        if (showDebugLogs)
            Debug.Log($"[DroppedItemPresenter] Rebuilt from DB: {items.Length} total, {activeCount} active");
    }

    // ── Sync Event Handlers ───────────────────────────────────

    /// <summary>
    /// Called when Master confirms an item was dropped (event 121 ITEM_SPAWNED).
    /// Registers in service and requests visual spawn if chunk is loaded.
    /// </summary>
    private void HandleItemSpawned(DroppedItemData data)
    {
        if (data == null) return;

        // Register in service registry
        service.RegisterItem(data);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemPresenter] Item spawned: {data.itemName} ({data.dropId}) at ({data.worldX:F1}, {data.worldY:F1})");

        // Spawn visual only if the chunk is currently loaded
        Vector2Int chunk = new Vector2Int(data.chunkX, data.chunkY);
        if (IsChunkLoaded(chunk))
        {
            OnSpawnVisualRequested?.Invoke(data);
        }
    }

    /// <summary>
    /// Called when Master confirms an item was removed (event 123 ITEM_REMOVED or 126 DESPAWN).
    /// Removes from service, despawns visual, and triggers inventory add for local player.
    /// </summary>
    private void HandleItemRemoved(string dropId, int pickedByActorNumber)
    {
        // Get item data before removing (needed for inventory add)
        DroppedItemData data = service.GetItem(dropId);

        // Unregister from service
        service.UnregisterItem(dropId);

        // Despawn visual
        OnDespawnVisualRequested?.Invoke(dropId);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemPresenter] Item removed: {dropId}, pickedBy: {pickedByActorNumber}");

        // If this local player picked it up, add to inventory
        if (pickedByActorNumber == PhotonNetwork.LocalPlayer.ActorNumber && data != null)
        {
            OnAddToInventoryRequested?.Invoke(data);
        }
    }

    /// <summary>
    /// Called during late-join sync (event 125 ITEM_SYNC_BATCH).
    /// Registers all items and requests visual spawn for loaded chunks.
    /// </summary>
    private void HandleSyncBatch(DroppedItemData[] items)
    {
        if (items == null) return;

        if (showDebugLogs)
            Debug.Log($"[DroppedItemPresenter] Sync batch received: {items.Length} items");

        foreach (var data in items)
        {
            if (data == null) continue;

            // Skip duplicates
            if (service.HasItem(data.dropId)) continue;

            service.RegisterItem(data);

            // Spawn visual if chunk is loaded
            Vector2Int chunk = new Vector2Int(data.chunkX, data.chunkY);
            if (IsChunkLoaded(chunk))
            {
                OnSpawnVisualRequested?.Invoke(data);
            }
        }
    }

    // ── Chunk Event Handlers ──────────────────────────────────

    /// <summary>
    /// When a chunk is loaded, spawn visuals for all dropped items in that chunk.
    /// </summary>
    private void HandleChunkLoaded(Vector2Int chunkPos)
    {
        var items = service.GetItemsInChunk(chunkPos);
        foreach (var data in items)
        {
            OnSpawnVisualRequested?.Invoke(data);
        }

        if (showDebugLogs && items.Count > 0)
            Debug.Log($"[DroppedItemPresenter] Chunk ({chunkPos.x}, {chunkPos.y}) loaded — spawning {items.Count} items.");
    }

    /// <summary>
    /// When a chunk is unloaded, despawn visuals (data stays in registry).
    /// </summary>
    private void HandleChunkUnloaded(Vector2Int chunkPos)
    {
        var items = service.GetItemsInChunk(chunkPos);
        int count = 0;
        foreach (var data in items)
        {
            OnDespawnVisualRequested?.Invoke(data.dropId);
            count++;
        }

        if (showDebugLogs && count > 0)
            Debug.Log($"[DroppedItemPresenter] Chunk ({chunkPos.x}, {chunkPos.y}) unloaded — despawning {count} items.");
    }

    // ── Helpers ───────────────────────────────────────────────

    /// <summary>
    /// Check if a chunk is currently loaded.
    /// Falls back to true if ChunkLoadingManager is unavailable (show all items).
    /// </summary>
    private bool IsChunkLoaded(Vector2Int chunkPos)
    {
        if (chunkLoadingManager == null) return true;
        return chunkLoadingManager.IsChunkLoaded(chunkPos);
    }

    // ── Cleanup ───────────────────────────────────────────────

    /// <summary>Unsubscribe all events. Called by View.OnDestroy().</summary>
    public void Cleanup()
    {
        UnsubscribeFromSyncEvents();
        UnsubscribeFromChunkEvents();
    }
}
