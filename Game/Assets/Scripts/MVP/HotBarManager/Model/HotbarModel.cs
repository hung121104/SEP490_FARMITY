using System;
using UnityEngine;

public class HotbarModel
{
    // Data
    public HotbarSlot[] Slots { get; private set; }
    public int CurrentSlotIndex { get; private set; }
    public int HotbarSize { get; private set; }

    // Events - for Presenter to subscribe
    public event Action<int> OnSlotIndexChanged;
    public event Action<int, HotbarSlot> OnSlotContentChanged;
    public event Action<ItemDataSO, int> OnItemUsed;
    public event Action<ItemDataSO, int> OnItemUsedAlternate;

    public HotbarModel(int size)
    {
        HotbarSize = size;
        CurrentSlotIndex = 0;
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        Slots = new HotbarSlot[HotbarSize];
        for (int i = 0; i < HotbarSize; i++)
        {
            Slots[i] = new HotbarSlot();
        }
        Debug.Log($"✅ Hotbar model initialized with {HotbarSize} slots");
    }

    // Slot selection methods
    public bool SelectSlot(int index)
    {
        if (index < 0 || index >= HotbarSize || index == CurrentSlotIndex)
            return false;

        CurrentSlotIndex = index;
        OnSlotIndexChanged?.Invoke(CurrentSlotIndex);

        string itemName = Slots[CurrentSlotIndex].IsEmpty ? "Empty" : Slots[CurrentSlotIndex].item.itemName;
        Debug.Log($"🎯 Selected Slot {index + 1}: {itemName}");
        return true;
    }

    public void SelectNextSlot()
    {
        SelectSlot((CurrentSlotIndex + 1) % HotbarSize);
    }

    public void SelectPreviousSlot()
    {
        SelectSlot((CurrentSlotIndex - 1 + HotbarSize) % HotbarSize);
    }

    // Item management methods
    public bool AddItemToSlot(int slotIndex, ItemDataSO item, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= HotbarSize || item == null)
            return false;

        var slot = Slots[slotIndex];
        int remaining = slot.AddItem(item, quantity);

        OnSlotContentChanged?.Invoke(slotIndex, slot);
        Debug.Log($"➕ Added {quantity - remaining}x {item.itemName} to Slot {slotIndex + 1}");

        return remaining == 0;
    }

    public bool RemoveItemFromSlot(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= HotbarSize) return false;

        var slot = Slots[slotIndex];
        if (slot.IsEmpty) return false;

        string itemName = slot.item.itemName;
        slot.RemoveItem(quantity);

        OnSlotContentChanged?.Invoke(slotIndex, slot);
        Debug.Log($"➖ Removed {quantity}x {itemName} from Slot {slotIndex + 1}");

        return true;
    }

    public void UseCurrentItem()
    {
        var currentSlot = Slots[CurrentSlotIndex];
        if (!currentSlot.IsEmpty)
        {
            OnItemUsed?.Invoke(currentSlot.item, CurrentSlotIndex);
        }
        else
        {
            Debug.Log("❌ No item to use!");
        }
    }

    public void UseCurrentItemAlternate()
    {
        var currentSlot = Slots[CurrentSlotIndex];
        if (!currentSlot.IsEmpty)
        {
            OnItemUsedAlternate?.Invoke(currentSlot.item, CurrentSlotIndex);
        }
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= HotbarSize || indexB < 0 || indexB >= HotbarSize)
            return;

        var temp = Slots[indexA].Clone();
        Slots[indexA] = Slots[indexB].Clone();
        Slots[indexB] = temp;

        OnSlotContentChanged?.Invoke(indexA, Slots[indexA]);
        OnSlotContentChanged?.Invoke(indexB, Slots[indexB]);

        Debug.Log($"🔄 Swapped Slot {indexA + 1} ↔ Slot {indexB + 1}");
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= HotbarSize) return;

        Slots[slotIndex].Clear();
        OnSlotContentChanged?.Invoke(slotIndex, Slots[slotIndex]);
        Debug.Log($"🗑️ Cleared Slot {slotIndex + 1}");
    }

    public void ClearHotbar()
    {
        for (int i = 0; i < HotbarSize; i++)
        {
            Slots[i].Clear();
            OnSlotContentChanged?.Invoke(i, Slots[i]);
        }
        Debug.Log("🗑️ Cleared entire hotbar");
    }

    public HotbarSlot GetSlot(int index)
    {
        if (index < 0 || index >= HotbarSize) return null;
        return Slots[index];
    }

    public HotbarSlot GetCurrentSlot() => Slots[CurrentSlotIndex];
}
