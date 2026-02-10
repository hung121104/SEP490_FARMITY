using System;
using UnityEngine;

public class HotbarModel
{
    private readonly InventoryModel inventoryModel;
    private readonly int hotbarStartIndex;
    private readonly int hotbarSize;

    public int CurrentSlotIndex { get; private set; }
    public int HotbarSize => hotbarSize;

    public event Action<int> OnSlotIndexChanged;
    public event Action OnHotbarRefreshed;

    public HotbarModel(InventoryModel inventory, int startIndex = 27, int size = 9)
    {
        inventoryModel = inventory;
        hotbarStartIndex = startIndex;
        hotbarSize = size;
        CurrentSlotIndex = 0;

        Debug.Log("HotbarModel: Mapping inventory slots " + startIndex + " to " + (startIndex + size - 1));
    }

    public bool SelectSlot(int localIndex)
    {
        if (localIndex < 0 || localIndex >= hotbarSize || localIndex == CurrentSlotIndex)
            return false;

        CurrentSlotIndex = localIndex;
        OnSlotIndexChanged?.Invoke(CurrentSlotIndex);

        var item = GetCurrentItem();
        string itemName = item != null ? item.ItemName : "Empty";
        Debug.Log("Selected Hotbar Slot " + (localIndex + 1) + ": " + itemName);
        return true;
    }

    public void SelectNextSlot()
    {
        SelectSlot((CurrentSlotIndex + 1) % hotbarSize);
    }

    public void SelectPreviousSlot()
    {
        SelectSlot((CurrentSlotIndex - 1 + hotbarSize) % hotbarSize);
    }

    public InventoryItem GetItemAt(int localIndex)
    {
        if (localIndex < 0 || localIndex >= hotbarSize)
            return null;

        int inventoryIndex = hotbarStartIndex + localIndex;
        return inventoryModel.GetItemAtSlot(inventoryIndex);
    }

    public InventoryItem GetCurrentItem()
    {
        return GetItemAt(CurrentSlotIndex);
    }

    public int GetInventoryIndex(int localIndex)
    {
        return hotbarStartIndex + localIndex;
    }

    public void RefreshHotbar()
    {
        OnHotbarRefreshed?.Invoke();
    }
}
