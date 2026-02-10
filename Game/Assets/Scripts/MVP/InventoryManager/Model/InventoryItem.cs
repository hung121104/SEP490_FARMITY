using UnityEngine;

public class InventoryItem
{
    public ItemDataSO itemData; // Reference to your ScriptableObject
    public int quantity;
    public int slotIndex; // Position in inventory
    public Quality itemQuality; // Individual quality for each item instance

    public InventoryItem(ItemDataSO data, int qty, int slot = -1, Quality quality = Quality.Normal)
    {
        itemData = data;
        quantity = qty;
        slotIndex = slot;
        itemQuality = quality;
    }

    // Properties for easy access using your ItemDataSO structure
    public string ItemId => itemData.itemID;
    public string ItemName => itemData.itemName;
    public string Description => itemData.description;
    public Sprite Icon => itemData.icon;
    public ItemType ItemType => itemData.GetItemType();
    public ItemCategory ItemCategory => itemData.GetItemCategory();
    public int MaxStack => itemData.MaxStack;
    public bool IsStackable => itemData.IsStackable;
    public Quality Quality => itemQuality;

    // Economic properties
    public int BasePrice => itemData.basePrice;
    public int BuyPrice => itemData.buyPrice;
    public int SellPrice => itemData.GetSellPrice(itemQuality);
    public bool CanBeSold => itemData.CanBeSold;
    public bool CanBeBought => itemData.CanBeBought;

    // Special properties
    public bool IsQuestItem => itemData.isQuestItem;
    public bool IsArtifact => itemData.isArtifact;
    public bool IsRareItem => itemData.isRareItem;

    // Gift system
    public bool IsValidGift => itemData.IsValidGift();
    public NPCPreference[] NPCPreferences => itemData.npcPreferences;
}
