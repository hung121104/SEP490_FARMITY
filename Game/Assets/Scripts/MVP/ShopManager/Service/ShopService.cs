using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ShopService : IShopService
{
    private readonly ShopModel _model;
    private const int ITEMS_PER_DAY = 5;

    public ShopService(ItemType shopType)
    {
        _model = new ShopModel(shopType);
        GenerateDailyItems(); 
    }

    public ShopModel GetShopModel() => _model;

    /// <summary>
    /// 
    /// </summary>
    public void GenerateDailyItems()
    {
        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady) return;

        FieldInfo fieldInfo = typeof(ItemCatalogService).GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo == null) return;

        var hiddenCatalog = (Dictionary<string, ItemData>)fieldInfo.GetValue(ItemCatalogService.Instance);
        var validItems = hiddenCatalog.Values
            .Where(item => item.canBeBought && item.itemType == _model.ShopType)
            .ToList();

        var newShopItems = new List<ShopItemModel>();

        foreach (var itemData in validItems)
        {
          
            int finalPrice = itemData.buyPrice;
            newShopItems.Add(new ShopItemModel(itemData.itemID, finalPrice));
        }

        _model.SetDailyItems(newShopItems);
        Debug.Log($"[ShopService] Load all Item {newShopItems.Count} item {_model.ShopType}");
    }
    public bool TryBuyItem(int slotIndex, IInventoryService playerInventory)
    {
        if (slotIndex < 0 || slotIndex >= _model.DailyItems.Count) return false;

        var shopItem = _model.DailyItems[slotIndex];
        if (shopItem.IsSoldOut) return false;

       
        if (!playerInventory.HasSpace())
        {
            Debug.LogWarning("[ShopService] Túi đồ đã đầy!");
            return false;
        }

        
        if (WorldDataManager.Instance.TrySpendGold(shopItem.Price))
        {
            // 
            playerInventory.AddItem(shopItem.ItemId, 1);
            // just by 1 item at a time, if you want to buy more, you can call this method multiple times
            shopItem.IsSoldOut = true; 
            return true;
        }

        return false; 
    }

    public bool SellItem(string itemId, int quantity, IInventoryService playerInventory)
    {
        var itemData = ItemCatalogService.Instance.GetItemData(itemId);
        if (itemData == null || !itemData.canBeSold) return false;

        
        if (playerInventory.RemoveItem(itemId, quantity))
        {
            
            int totalEarned = itemData.GetSellPrice() * quantity;
            WorldDataManager.Instance.AddGold(totalEarned);
            return true;
        }

        return false;
    }
}