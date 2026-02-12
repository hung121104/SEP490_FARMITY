using System;
using UnityEngine;

public class InventoryPresenter
{
    private readonly InventoryModel model;
    private readonly IInventoryService service;
    private IInventoryView view;

    // Item detail system integration
    private ItemDetailView itemDetailView;
    private ItemPresenter currentItemPresenter;
    
    // Track which slot is currently showing tooltip
    private int currentTooltipSlot = -1;

    // Events for GameView or other systems
    public event Action<ItemModel> OnItemUsed;
    public event Action<ItemModel> OnItemDropped;

    #region Initialization

    public InventoryPresenter(InventoryModel inventoryModel, IInventoryService inventoryService)
    {
        model = inventoryModel;
        service = inventoryService;

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

    private void HandleItemAdded(ItemModel item, int slotIndex)
    {
        view?.UpdateSlot(slotIndex, item);
        view?.ShowNotification($"Added {item.ItemName} x{item.Quantity}");
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
        view?.UpdateDragPreview(position);
    }

    private void HandleSlotEndDrag()
    {
        view?.HideDragPreview();
    }

    private void HandleSlotDrop(int targetSlotIndex)
    {
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
        var item = service.GetItemAtSlot(slotIndex);
        if (item != null && !item.IsQuestItem)
        {
            OnItemDropped?.Invoke(item);
            service.RemoveItemFromSlot(slotIndex, 1);
        }
        else if (item != null && item.IsQuestItem)
        {
            view?.ShowNotification("Cannot drop quest items!");
        }
    }

    private void HandleSort()
    {
        service.SortInventory();
        HideCurrentItemDetail();
    }

    #endregion

    #region Delete Event Handler

    private void HandleItemDelete(int slotIndex)
    {
        var item = service.GetItemAtSlot(slotIndex);

        if (item == null)
        {
            Debug.LogWarning($"[InventoryPresenter] No item at slot {slotIndex} to delete");
            return;
        }

        // Prevent deletion of quest items and artifacts
        if (item.IsQuestItem)
        {
            view?.ShowNotification("Cannot delete quest items!");
            Debug.LogWarning($"[InventoryPresenter] Cannot delete quest item: {item.ItemName}");
            return;
        }

        if (item.IsArtifact)
        {
            view?.ShowNotification("Cannot delete artifact items!");
            Debug.LogWarning($"[InventoryPresenter] Cannot delete artifact: {item.ItemName}");
            return;
        }

        // Delete the entire stack
        int quantity = item.Quantity;
        string itemName = item.ItemName;

        bool success = service.RemoveItemFromSlot(slotIndex, quantity);

        if (success)
        {
            view?.ShowNotification($"Deleted {itemName} x{quantity}");
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
            view?.ShowNotification("Failed to delete item!");
            Debug.LogError($"[InventoryPresenter] Failed to delete item from slot {slotIndex}");
        }
    }

    #endregion

    #region Item Detail Hover Handlers

    private void HandleSlotHoverEnter(int slotIndex, Vector2 screenPosition)
    {
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

        // Get current mouse position
        Vector2 mousePosition = Input.mousePosition;

        // Show tooltip at current mouse position
        ShowTooltipForSlot(slotIndex, mousePosition);
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

        // Get current mouse position
        Vector2 mousePosition = Input.mousePosition;

        // Show updated tooltip
        ShowTooltipForSlot(slotIndex, mousePosition);
    }

    #endregion

    #region Public API for external systems

    public bool TryAddItem(ItemDataSO itemData, int quantity = 1, Quality quality = Quality.Normal)
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

    #endregion

    #region Helper Methods

    private void RefreshView()
    {
        if (view == null) return;

        for (int i = 0; i < model.maxSlots; i++)
        {
            var item = service.GetItemAtSlot(i);
            view.UpdateSlot(i, item);
        }
    }

    public void Cleanup()
    {
        RemoveView();
        HideCurrentItemDetail();
    }

    #endregion
}
