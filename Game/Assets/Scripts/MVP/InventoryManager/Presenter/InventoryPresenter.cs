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

    #region Item Detail Hover Handlers

    private void HandleSlotHoverEnter(int slotIndex, Vector2 screenPosition)
    {
        var itemModel = service.GetItemAtSlot(slotIndex);
        if (itemModel == null || itemDetailView == null) return;

        IItemService itemService = new ItemService(itemModel);
        currentItemPresenter = new ItemPresenter(itemModel, itemService);
        currentItemPresenter.SetView(itemDetailView);

        // Subscribe to item interactions if needed
        currentItemPresenter.OnItemInteracted += HandleItemDetailInteraction;

        // Show details at cursor position
        currentItemPresenter.ShowItemDetailsAtPosition(screenPosition);
    }

    private void HandleSlotHoverExit(int slotIndex)
    {
        HideCurrentItemDetail();
    }

    private void HideCurrentItemDetail()
    {
        if (currentItemPresenter != null)
        {
            currentItemPresenter.OnItemInteracted -= HandleItemDetailInteraction;
            currentItemPresenter.HideItemDetails();
            currentItemPresenter.RemoveView();
            currentItemPresenter = null;
        }
    }

    //Need for checking
    private void HandleItemDetailInteraction(ItemModel itemModel)
    {
        Debug.Log($"[InventoryPresenter] Item detail interaction: {itemModel.ItemName}");
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
