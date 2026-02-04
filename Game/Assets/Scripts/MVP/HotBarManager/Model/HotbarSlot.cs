using UnityEngine;
[System.Serializable]
public class HotbarSlot
{
    public ItemDataSO item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public bool CanAddItem(ItemDataSO newItem)
    {
        if (IsEmpty) return true;
        if (!newItem.isStackable) return false;
        return item.itemID == newItem.itemID && quantity < item.maxStack;
    }


    // Add item to the slot, return remaining amount that couldn't be added
    public int AddItem(ItemDataSO newItem, int amount = 1)
    {
        if (IsEmpty)
        {
            item = newItem;
            quantity = Mathf.Min(amount, newItem.maxStack);
            return amount - quantity;   
        }
        else if (item.itemID == newItem.itemID && newItem.isStackable)
        {
            int addable = Mathf.Min(amount, item.maxStack - quantity);
            quantity += addable;
            return amount - addable;
        }
        return amount;
    }

    public void RemoveItem(int amount = 1)
    {
        quantity -= amount;
        if (quantity <= 0)
        {
            Clear();
        }
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
    }

    public HotbarSlot Clone()
    {
        return new HotbarSlot
        {
            item = this.item,
            quantity = this.quantity
        };
    }
}
