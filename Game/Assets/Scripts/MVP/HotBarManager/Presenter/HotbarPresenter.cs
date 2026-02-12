using System;
using UnityEngine;

public class HotbarPresenter
{
    private readonly HotbarModel model;
    private readonly HotbarView view;
    private readonly IInventoryService inventoryService;

    public event Action<ItemDataSO, Vector3, int> OnItemUsed;

    public HotbarPresenter(HotbarModel model, HotbarView view, IInventoryService inventoryService)
    {
        this.model = model;
        this.view = view;
        this.inventoryService = inventoryService;

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        view.OnSlotKeyPressed += HandleSlotSelection;
        view.OnScrollInput += HandleScrollInput;
        view.OnUseItemInput += HandleUseItem;

        model.OnSlotIndexChanged += view.UpdateSelection;
        model.OnHotbarRefreshed += RefreshAllSlots;

        inventoryService.OnItemAdded += HandleInventoryChanged;
        inventoryService.OnItemRemoved += HandleInventoryChanged;
        inventoryService.OnQuantityChanged += HandleInventoryChanged;
        inventoryService.OnInventoryChanged += RefreshAllSlots;
    }

    public void UnsubscribeEvents()
    {
        view.OnSlotKeyPressed -= HandleSlotSelection;
        view.OnScrollInput -= HandleScrollInput;
        view.OnUseItemInput -= HandleUseItem;

        model.OnSlotIndexChanged -= view.UpdateSelection;
        model.OnHotbarRefreshed -= RefreshAllSlots;

        inventoryService.OnItemAdded -= HandleInventoryChanged;
        inventoryService.OnItemRemoved -= HandleInventoryChanged;
        inventoryService.OnQuantityChanged -= HandleInventoryChanged;
        inventoryService.OnInventoryChanged -= RefreshAllSlots;
    }

    private void HandleSlotSelection(int slotIndex)
    {
        model.SelectSlot(slotIndex);
    }

    private void HandleScrollInput(float direction)
    {
        if (direction > 0f)
            model.SelectPreviousSlot();
        else
            model.SelectNextSlot();
    }

    private void HandleUseItem()
    {
        var item = model.GetCurrentItem();
        if (item == null)
        {
            Debug.Log("No item to use");
            return;
        }

        Vector3 targetPosition = view.GetMouseWorldPosition();
        int inventorySlotIndex = model.GetInventoryIndex(model.CurrentSlotIndex);

        Debug.Log("Using: " + item.ItemName + " at position: " + targetPosition);
        OnItemUsed?.Invoke(item.ItemData, targetPosition, inventorySlotIndex);
    }

    public void ConsumeCurrentItem(int amount = 1)
    {
        int inventorySlotIndex = model.GetInventoryIndex(model.CurrentSlotIndex);
        inventoryService.RemoveItemFromSlot(inventorySlotIndex, amount);
        Debug.Log("Consumed " + amount + "x item from hotbar slot " + (model.CurrentSlotIndex + 1));
    }

    public void ConsumeItemAtSlot(int localSlotIndex, int amount = 1)
    {
        int inventorySlotIndex = model.GetInventoryIndex(localSlotIndex);
        inventoryService.RemoveItemFromSlot(inventorySlotIndex, amount);
        Debug.Log("Consumed " + amount + "x item from hotbar slot " + (localSlotIndex + 1));
    }

    private void HandleInventoryChanged(ItemModel item, int slotIndex)
    {
        RefreshAllSlots();
    }

    private void HandleInventoryChanged(int slotA, int slotB)
    {
        RefreshAllSlots();
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < model.HotbarSize; i++)
        {
            var item = model.GetItemAt(i);
            view.UpdateSlotDisplay(i, item);
        }
    }

    public void Initialize()
    {
        RefreshAllSlots();
        view.UpdateSelection(model.CurrentSlotIndex);
    }

    public ItemModel GetCurrentItem() => model.GetCurrentItem();
    public ItemModel GetItemAt(int localIndex) => model.GetItemAt(localIndex);
}
