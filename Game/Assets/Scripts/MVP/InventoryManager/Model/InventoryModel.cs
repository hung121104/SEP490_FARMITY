using System.Collections.Generic;
using System.Linq;

public class InventoryModel
{
    private InventoryItem[] items;
    public int maxSlots { get; private set; }

    public InventoryModel(int slots = 20)
    {
        maxSlots = slots;
        items = new InventoryItem[maxSlots];
    }

    // READ-ONLY operations - for Presenter to use
    public IReadOnlyList<InventoryItem> Slots => items;

    public InventoryItem GetItemAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots)
            return null;
        return items[slotIndex];
    }

    public List<InventoryItem> GetNonEmptyItems()
    {
        return items.Where(item => item != null).ToList();
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < maxSlots && items[slotIndex] == null;
    }

    public bool IsSlotValid(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < maxSlots;
    }

    // INTERNAL operations - for Service to use
    internal void SetItemAtSlot(int slotIndex, InventoryItem item)
    {
        if (IsSlotValid(slotIndex))
        {
            items[slotIndex] = item;
            if (item != null)
                item.slotIndex = slotIndex;
        }
    }

    internal void ClearSlot(int slotIndex)
    {
        if (IsSlotValid(slotIndex))
            items[slotIndex] = null;
    }

    internal void SwapSlots(int slotA, int slotB)
    {
        if (IsSlotValid(slotA) && IsSlotValid(slotB))
        {
            var temp = items[slotA];
            items[slotA] = items[slotB];
            items[slotB] = temp;

            if (items[slotA] != null)
                items[slotA].slotIndex = slotA;
            if (items[slotB] != null)
                items[slotB].slotIndex = slotB;
        }
    }

    // Query operations - for Service to use
    internal int FindEmptySlot()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] == null) return i;
        }
        return -1;
    }

    internal int FindItemSlot(string itemId, Quality quality)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] != null &&
                items[i].ItemId == itemId &&
                items[i].Quality == quality)
                return i;
        }
        return -1;
    }

    internal List<int> GetSlotsWithItem(string itemId, Quality? specificQuality = null)
    {
        var slots = new List<int>();
        for (int i = 0; i < maxSlots; i++)
        {
            if (items[i] != null &&
                items[i].ItemId == itemId &&
                (specificQuality == null || items[i].Quality == specificQuality))
                slots.Add(i);
        }
        return slots;
    }
}
