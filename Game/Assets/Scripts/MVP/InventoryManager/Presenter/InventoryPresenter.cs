using System;
using UnityEngine;

public class InventoryPresenter
{
    private readonly InventoryModel model;
    private readonly IInventoryService service;
    private IInventoryView view;

    // Events for GameView or other systems
    public event Action<InventoryItem> OnItemUsed;
    public event Action<InventoryItem> OnItemDropped;

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

    public void RemoveView()
    {
        if (view != null)
        {
            UnsubscribeFromViewEvents();
            view = null;
        }
    }

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

    private void HandleItemAdded(InventoryItem item, int slotIndex)
    {
        view?.UpdateSlot(slotIndex, item);
        view?.ShowNotification($"Added {item.ItemName} x{item.quantity}");
    }

    private void HandleItemRemoved(InventoryItem item, int slotIndex)
    {
        view?.ClearSlot(slotIndex);
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
    }

    private void HandleInventoryChanged()
    {
        RefreshView();
    }

    #endregion

    #region View Event Handlers

    private int selectedSlot = -1;
    private int draggedSlot = -1;

    private void HandleSlotClicked(int slotIndex)
    {
        var item = service.GetItemAtSlot(slotIndex);

        if (item != null)
        {
            view?.ShowItemDetails(item);
            selectedSlot = slotIndex;
        }
        else
        {
            view?.HideItemDetails();
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
        }
    }

    private void HandleSlotDrag(Vector2 position)
    {
        view?.UpdateDragPreview(position);
    }

    private void HandleSlotEndDrag()
    {
        view?.HideDragPreview();
        draggedSlot = -1;
    }

    private void HandleSlotDrop(int targetSlotIndex)
    {
        if (draggedSlot != -1 && draggedSlot != targetSlotIndex)
        {
            service.MoveItem(draggedSlot, targetSlotIndex);
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
        // Unsubscribe from service events if needed
    }

    #endregion
}
