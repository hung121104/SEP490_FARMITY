using System;
using UnityEngine;

public class HotbarPresenter
{
    private readonly HotbarView view;
    private readonly IInventoryService inventoryService;
    private readonly int hotbarStartIndex;
    private readonly int hotbarSize;

    private int currentSlotIndex;

    public event Action<ItemData, Vector3, int> OnItemUsed;

    /// <summary>Fired when the selected hotbar slot changes. Parameter is the new item's ItemData (null if empty).</summary>
    public event Action<ItemData> OnSelectedItemChanged;

    public HotbarPresenter(HotbarView view, IInventoryService inventoryService, int startIndex = 27, int size = 9)
    {
        this.view = view;
        this.inventoryService = inventoryService;
        this.hotbarStartIndex = startIndex;
        this.hotbarSize = size;
        this.currentSlotIndex = 0;

        SubscribeToEvents();
    }

    #region Event Subscriptions

    private void SubscribeToEvents()
    {
        view.OnSlotKeyPressed += HandleSlotSelection;
        view.OnScrollInput += HandleScrollInput;
        view.OnUseItemInput += HandleUseItem;

        inventoryService.OnItemAdded += HandleItemAddedOrRemoved;
        inventoryService.OnItemRemoved += HandleItemAddedOrRemoved;
        inventoryService.OnItemsMoved += HandleItemsMoved;
        inventoryService.OnSlotChanged += HandleSlotChanged;
        inventoryService.OnInventoryChanged += RefreshAllSlots;
    }

    public void UnsubscribeEvents()
    {
        view.OnSlotKeyPressed -= HandleSlotSelection;
        view.OnScrollInput -= HandleScrollInput;
        view.OnUseItemInput -= HandleUseItem;

        inventoryService.OnItemAdded -= HandleItemAddedOrRemoved;
        inventoryService.OnItemRemoved -= HandleItemAddedOrRemoved;
        inventoryService.OnItemsMoved -= HandleItemsMoved;
        inventoryService.OnSlotChanged -= HandleSlotChanged;
        inventoryService.OnInventoryChanged -= RefreshAllSlots;
    }

    #endregion

    #region Selection

    private bool SelectSlot(int localIndex)
    {
        if (localIndex < 0 || localIndex >= hotbarSize || localIndex == currentSlotIndex)
            return false;

        currentSlotIndex = localIndex;
        view.UpdateSelection(currentSlotIndex);
        return true;
    }

    private void HandleSlotSelection(int slotIndex)
    {
        SelectSlot(slotIndex);
        NotifySelectedItemChanged();
    }

    private void HandleScrollInput(float direction)
    {
        if (direction > 0f)
            SelectSlot((currentSlotIndex - 1 + hotbarSize) % hotbarSize);
        else
            SelectSlot((currentSlotIndex + 1) % hotbarSize);
        NotifySelectedItemChanged();
    }

    private void NotifySelectedItemChanged()
    {
        var item = GetCurrentItem();
        OnSelectedItemChanged?.Invoke(item?.ItemData);
    }

    #endregion

    #region Item Usage

    private void HandleUseItem()
    {
        var item = GetCurrentItem();
        if (item == null)
        {
            Debug.Log("No item to use");
            return;
        }

        Vector3 targetPosition = view.GetMouseWorldPosition();
        int inventorySlotIndex = GetInventoryIndex(currentSlotIndex);

        Debug.Log("Using: " + item.ItemName + " at position: " + targetPosition);
        OnItemUsed?.Invoke(item.ItemData, targetPosition, inventorySlotIndex);
    }

    public void ConsumeCurrentItem(int amount = 1)
    {
        int inventorySlotIndex = GetInventoryIndex(currentSlotIndex);
        inventoryService.RemoveItemFromSlot(inventorySlotIndex, amount);
        Debug.Log("Consumed " + amount + "x item from hotbar slot " + (currentSlotIndex + 1));
    }

    public void ConsumeItemAtSlot(int localSlotIndex, int amount = 1)
    {
        int inventorySlotIndex = GetInventoryIndex(localSlotIndex);
        inventoryService.RemoveItemFromSlot(inventorySlotIndex, amount);
        Debug.Log("Consumed " + amount + "x item from hotbar slot " + (localSlotIndex + 1));
    }

    #endregion

    #region Inventory Event Handlers

    private void HandleItemAddedOrRemoved(ItemModel _, int inventorySlotIndex)
    {
        RefreshSlotIfHotbar(inventorySlotIndex);
    }

    private void HandleItemsMoved(int fromSlot, int toSlot)
    {
        RefreshSlotIfHotbar(fromSlot);
        RefreshSlotIfHotbar(toSlot);
    }

    private void HandleSlotChanged(int inventorySlotIndex)
    {
        RefreshSlotIfHotbar(inventorySlotIndex);
    }

    private void RefreshSlotIfHotbar(int inventorySlotIndex)
    {
        if (!TryGetLocalIndex(inventorySlotIndex, out int localIndex)) return;
        var item = GetItemAt(localIndex);
        view.UpdateSlotDisplay(localIndex, item);
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < hotbarSize; i++)
        {
            var item = GetItemAt(i);
            view.UpdateSlotDisplay(i, item);
        }
    }

    #endregion

    #region Index Mapping

    public int GetInventoryIndex(int localIndex)
    {
        return hotbarStartIndex + localIndex;
    }

    public bool TryGetLocalIndex(int inventoryIndex, out int localIndex)
    {
        localIndex = inventoryIndex - hotbarStartIndex;
        return localIndex >= 0 && localIndex < hotbarSize;
    }

    #endregion

    #region Public API

    public void Initialize()
    {
        RefreshAllSlots();
        view.UpdateSelection(currentSlotIndex);
    }

    public ItemModel GetCurrentItem()
    {
        return GetItemAt(currentSlotIndex);
    }

    public ItemModel GetItemAt(int localIndex)
    {
        if (localIndex < 0 || localIndex >= hotbarSize)
            return null;

        int inventoryIndex = hotbarStartIndex + localIndex;
        return inventoryService.GetItemAtSlot(inventoryIndex);
    }

    #endregion
}
