using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPresenter
{
    private readonly InventoryModel model;
    private readonly IInventoryService service;
    private IInventoryView view;

    // Secondary views: receive data updates only (no input events)
    private readonly List<IInventoryView> secondaryViews = new List<IInventoryView>();

    // Item detail system integration
    private ItemDetailView itemDetailView;
    private ItemPresenter currentItemPresenter;
    
    // Track which slot is currently showing tooltip
    private int currentTooltipSlot = -1;

    // Track cursor position from View events (avoids Input.mousePosition dependency)
    private Vector2 lastKnownCursorPosition;

    // Action cooldown for network sync
    private float lastActionTime = 0f;
    private float actionCooldownDuration = 1.0f; // Đợi 1 giây sau action cuối cùng trước khi cho phép sync

    // Events for GameView or other systems
    public event Action<ItemModel> OnItemUsed;
    public event Action<ItemModel> OnItemDropped;

    #region Initialization

    public InventoryPresenter(InventoryModel inventoryModel, IInventoryService inventoryService)
    {
        model = inventoryModel;
        service = inventoryService;
        // Subtract cooldown so IsReadyToSync() returns true immediately at startup.
        // (setting to Time.time would mean 0 seconds have elapsed — not ready yet.)
        lastActionTime = Time.time - actionCooldownDuration;

        SubscribeToServiceEvents();
    }

    public void SetView(IInventoryView inventoryView)
    {
        view = inventoryView;

        if (view != null)
        {
            SubscribeToViewEvents();
            RefreshView();
        }
    }

    public void SetItemDetailView(ItemDetailView detailView)
    {
        itemDetailView = detailView;
    }

    /// <summary>
    /// Reset action cooldown timer. Called whenever user performs an action.
    /// After this is called, sync is blocked for actionCooldownDuration seconds.
    /// </summary>
    private void ResetActionTimer()
    {
        lastActionTime = Time.time;
    }

    /// <summary>
    /// Called by external systems to notify that the user is performing
    /// an action on a secondary view. Resets the cooldown so
    /// HandleRemoteInventoryChanged defers the echo.
    /// </summary>
    public void NotifyExternalAction()
    {
        lastActionTime = Time.time;
    }

    /// <summary>
    /// Check if enough time has passed since last user action to allow network sync.
    /// </summary>
    public bool IsReadyToSync()
    {
        return (Time.time - lastActionTime) >= actionCooldownDuration;
    }

    public void RemoveView()
    {
        if (view != null)
        {
            UnsubscribeFromViewEvents();
            view = null;
        }
    }

    #endregion

    #region View Event Subscriptions

    private void SubscribeToViewEvents()
    {
        view.OnSlotClicked += HandleSlotClicked;
        view.OnSlotBeginDrag += HandleSlotBeginDrag;
        view.OnSlotDrag += HandleSlotDrag;
        view.OnSlotEndDrag += HandleSlotEndDrag;
        view.OnSlotDrop += HandleSlotDrop;
        view.OnUseItemRequested += HandleUseItem;
        view.OnDropItemRequested += HandleDropItem;
        view.OnSortRequested += HandleSort;
        view.OnSlotHoverEnter += HandleSlotHoverEnter;
        view.OnSlotHoverExit += HandleSlotHoverExit;
        view.OnItemDeleteRequested += HandleItemDelete;
    }

    private void UnsubscribeFromViewEvents()
    {
        view.OnSlotClicked -= HandleSlotClicked;
        view.OnSlotBeginDrag -= HandleSlotBeginDrag;
        view.OnSlotDrag -= HandleSlotDrag;
        view.OnSlotEndDrag -= HandleSlotEndDrag;
        view.OnSlotDrop -= HandleSlotDrop;
        view.OnUseItemRequested -= HandleUseItem;
        view.OnDropItemRequested -= HandleDropItem;
        view.OnSortRequested -= HandleSort;
        view.OnSlotHoverEnter -= HandleSlotHoverEnter;
        view.OnSlotHoverExit -= HandleSlotHoverExit;
        view.OnItemDeleteRequested -= HandleItemDelete;
    }

    #endregion

    #region Service Event Subscriptions

    private void SubscribeToServiceEvents()
    {
        service.OnItemAdded += HandleItemAdded;
        service.OnItemRemoved += HandleItemRemoved;
        service.OnItemsMoved += HandleItemsMoved;
        service.OnQuantityChanged += HandleQuantityChanged;
        service.OnInventoryChanged += HandleInventoryChanged;
    }

    private void UnsubscribeFromServiceEvents()
    {
        service.OnItemAdded -= HandleItemAdded;
        service.OnItemRemoved -= HandleItemRemoved;
        service.OnItemsMoved -= HandleItemsMoved;
        service.OnQuantityChanged -= HandleQuantityChanged;
        service.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleItemAdded(ItemModel item, int slotIndex)
    {
        view?.UpdateSlot(slotIndex, item);
    }

    private void HandleItemRemoved(ItemModel item, int slotIndex)
    {
        view?.ClearSlot(slotIndex);

        // If tooltip was showing for this slot, hide it
        if (currentTooltipSlot == slotIndex)
        {
            HideCurrentItemDetail();
        }
    }

    private void HandleItemsMoved(int fromSlot, int toSlot)
    {
        var fromItem = service.GetItemAtSlot(fromSlot);
        var toItem = service.GetItemAtSlot(toSlot);

        view?.UpdateSlot(fromSlot, fromItem);
        view?.UpdateSlot(toSlot, toItem);
    }

    private void HandleQuantityChanged(int slotIndex, int newQuantity)
    {
        var item = service.GetItemAtSlot(slotIndex);
        view?.UpdateSlot(slotIndex, item);

        // Refresh tooltip if it's showing for this slot
        if (currentTooltipSlot == slotIndex)
        {
            RefreshTooltipForSlot(slotIndex);
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshView();
    }

    #endregion

    #region View Event Handlers

    private int selectedSlot = -1;
    private int draggedSlot = -1;

    //Need for checking
    private void HandleSlotClicked(int slotIndex)
    {
        ResetActionTimer();
        var item = service.GetItemAtSlot(slotIndex);

        if (item != null)
        {
            selectedSlot = slotIndex;
            Debug.Log($"[InventoryPresenter] Selected slot {slotIndex}: {item.ItemName}");
        }
        else
        {
            selectedSlot = -1;
        }
    }

    private void HandleSlotBeginDrag(int slotIndex)
    {
        ResetActionTimer();
        var item = service.GetItemAtSlot(slotIndex);
        if (item != null)
        {
            draggedSlot = slotIndex;
            view?.ShowDragPreview(item);

            // Hide item detail tooltip when dragging
            HideCurrentItemDetail();
        }
    }

    private void HandleSlotDrag(Vector2 position)
    {
        ResetActionTimer();
        lastKnownCursorPosition = position;
        view?.UpdateDragPreview(position);
    }

    private void HandleSlotEndDrag()
    {
        ResetActionTimer();
        // If drag wasn't consumed by a slot drop or delete zone,
        // check if it ended outside the inventory panel → drop item to world
        if (draggedSlot != -1)
        {
            if (view != null && !view.IsScreenPositionInsideInventory(lastKnownCursorPosition))
            {
                HandleDropItem(draggedSlot);
                Debug.Log($"[InventoryPresenter] Item dragged outside inventory — dropped to world from slot {draggedSlot}");
            }
        }

        view?.HideDragPreview();
        draggedSlot = -1;
    }

    private void HandleSlotDrop(int targetSlotIndex)
    {
        ResetActionTimer();
        if (draggedSlot != -1 && draggedSlot != targetSlotIndex)
        {
            service.MoveItem(draggedSlot, targetSlotIndex);

            //Show tooltip for the target slot after swap
            ShowTooltipAfterDrop(targetSlotIndex);
        }

        draggedSlot = -1;
        view?.HideDragPreview();
    }

    private void HandleUseItem(int slotIndex)
    {
        ResetActionTimer();
        var item = service.GetItemAtSlot(slotIndex);
        if (item != null)
        {
            OnItemUsed?.Invoke(item);
            // Optionally remove consumable items
            // if (item.ItemType == ItemType.Consumable)
            // {
            //     service.RemoveItemFromSlot(slotIndex, 1);
            // }
        }
    }

    private void HandleDropItem(int slotIndex)
    {
        ResetActionTimer();
        var item = service.GetItemAtSlot(slotIndex);
        if (item != null && !item.IsQuestItem)
        {
            OnItemDropped?.Invoke(item);
            // Remove the entire stack from inventory (drop whole stack to world)
            service.RemoveItemFromSlot(slotIndex, item.Quantity);
        }
    }

    private void HandleSort()
    {
        ResetActionTimer();
        service.SortInventory();
        HideCurrentItemDetail();
    }

    #endregion

    #region Delete Event Handler

    private void HandleItemDelete(int slotIndex)
    {
        ResetActionTimer();

        // Only handle delete if this presenter initiated the drag.
        // If draggedSlot is -1, the drag came from another panel (e.g. chest).
        if (draggedSlot == -1)
        {
            Debug.Log($"[InventoryPresenter] Ignoring delete for slot {slotIndex} — drag not from player inventory");
            return;
        }

        var item = service.GetItemAtSlot(slotIndex);

        if (item == null)
        {
            // Item already removed - sync the view to match service state
            Debug.Log($"[InventoryPresenter] Slot {slotIndex} is already empty, syncing view");
            view?.ClearSlot(slotIndex);
            view?.HideDragPreview();
            draggedSlot = -1;
            return;
        }

        // Prevent deletion of quest items and artifacts
        if (item.IsQuestItem)
        {
            Debug.LogWarning($"[InventoryPresenter] Cannot delete quest item: {item.ItemName}");
            return;
        }

        if (item.IsArtifact)
        {
            Debug.LogWarning($"[InventoryPresenter] Cannot delete artifact: {item.ItemName}");
            return;
        }

        // Delete the entire stack
        int quantity = item.Quantity;
        string itemName = item.ItemName;

        bool success = service.RemoveItemFromSlot(slotIndex, quantity);

        if (success)
        {
            Debug.Log($"[InventoryPresenter] Deleted {itemName} x{quantity} from slot {slotIndex}");

            view?.HideDragPreview();

            // Hide tooltip if it was showing
            if (currentTooltipSlot == slotIndex)
            {
                HideCurrentItemDetail();               
            }
        }
        else
        {
            Debug.LogError($"[InventoryPresenter] Failed to delete item from slot {slotIndex}");
        }
        draggedSlot = -1;
    }

    #endregion

    #region Item Detail Hover Handlers

    private void HandleSlotHoverEnter(int slotIndex, Vector2 screenPosition)
    {
        lastKnownCursorPosition = screenPosition;

        // Don't show tooltip if currently dragging
        if (draggedSlot != -1)
        {
            return; // Skip showing tooltip
        }

        var itemModel = service.GetItemAtSlot(slotIndex);
        if (itemModel == null || itemDetailView == null) return;

        ShowTooltipForSlot(slotIndex, screenPosition);
    }

    private void HandleSlotHoverExit(int slotIndex)
    {
        HideCurrentItemDetail();
    }

    private void HideCurrentItemDetail()
    {
        if (currentItemPresenter != null)
        {
            currentItemPresenter.HideItemDetails();
            currentItemPresenter.RemoveView();
            currentItemPresenter = null;
        }
        currentTooltipSlot = -1;
    }

    private void HideCurrentItemDetailImmediate()
    {
        if (currentItemPresenter != null)
        {
            currentItemPresenter.HideItemDetailsImmediate();
            currentItemPresenter.RemoveView();
            currentItemPresenter = null;
        }
        currentTooltipSlot = -1;
    }

    #endregion

    #region Tooltip Management

    /// <summary>
    /// Show tooltip for specific slot at given position
    /// </summary>
    private void ShowTooltipForSlot(int slotIndex, Vector2 screenPosition)
    {
        var itemModel = service.GetItemAtSlot(slotIndex);
        if (itemModel == null || itemDetailView == null)
        {
            Debug.LogWarning($"[InventoryPresenter] Cannot show tooltip for slot {slotIndex} - item or view missing");
            return;
        }

        // Hide previous tooltip if any
        if (currentItemPresenter != null)
        {
            HideCurrentItemDetail();
        }

        // Track which slot is showing tooltip
        currentTooltipSlot = slotIndex;

        // Create ItemService and ItemPresenter
        IItemService itemService = new ItemService(itemModel);
        currentItemPresenter = new ItemPresenter(itemModel, itemService);
        currentItemPresenter.SetView(itemDetailView);

        // Show details at cursor position
        currentItemPresenter.ShowItemDetailsAtPosition(screenPosition);
    }

    /// <summary>
    /// Show tooltip after drop/swap completes
    /// </summary>
    private void ShowTooltipAfterDrop(int slotIndex)
    {
        var itemModel = service.GetItemAtSlot(slotIndex);

        if (itemModel == null)
        {
            Debug.Log($"[InventoryPresenter] Slot {slotIndex} is empty after drop - no tooltip");
            return;
        }

        ShowTooltipForSlot(slotIndex, lastKnownCursorPosition);
    }

    /// <summary>
    /// Refresh tooltip for specific slot (update to new item data)
    /// </summary>
    private void RefreshTooltipForSlot(int slotIndex)
    {
        if (itemDetailView == null || currentItemPresenter == null)
            return;

        var newItem = service.GetItemAtSlot(slotIndex);

        if (newItem == null)
        {
            // Slot now empty, hide tooltip
            HideCurrentItemDetail();
            return;
        }

        ShowTooltipForSlot(slotIndex, lastKnownCursorPosition);
    }

    #endregion

    #region Public API for external systems

    public bool TryAddItem(string itemId, int quantity = 1, Quality quality = Quality.Normal)
    {
        return service.AddItem(itemId, quantity, quality);
    }

    public bool TryAddItem(ItemData itemData, int quantity = 1, Quality quality = Quality.Normal)
    {
        return service.AddItem(itemData, quantity, quality);
    }

    public bool TryRemoveItem(string itemId, int quantity, Quality? quality = null)
    {
        return service.RemoveItem(itemId, quantity, quality);
    }

    public bool HasItem(string itemId, int quantity = 1)
    {
        return service.HasItem(itemId, quantity);
    }

    public int GetItemCount(string itemId)
    {
        return service.GetItemCount(itemId);
    }

    public void CancelAllActions()
    {
        // 1. Reset dragged slot state
        draggedSlot = -1;

        // 2. Reset selected slot state
        selectedSlot = -1;

        // 3. Hide item detail tooltip immediately
        HideCurrentItemDetailImmediate();

        // 4. Call view to cancel all visual actions
        view?.CancelAllActions();

        Debug.Log("[InventoryPresenter] All inventory actions cancelled");
    }

    #endregion

    #region Helper Methods

    private void RefreshView()
    {
        if (view == null && secondaryViews.Count == 0) return;

        for (int i = 0; i < model.maxSlots; i++)
        {
            var item = service.GetItemAtSlot(i);
            view?.UpdateSlot(i, item);
        }
    }

    #endregion

    #region Cleanup

    public void Cleanup()
    {
        CancelAllActions();
        RemoveView();
        secondaryViews.Clear();
        UnsubscribeFromServiceEvents();
        HideCurrentItemDetail();
    }

    #endregion
}
