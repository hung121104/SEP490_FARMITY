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
        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady)
        {
            Debug.LogWarning("[ShopService] ItemCatalog chưa sẵn sàng!");
            return;
        }

        // 1. use reflection to access the private _catalog field in ItemCatalogService
        FieldInfo fieldInfo = typeof(ItemCatalogService).GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo == null) return;

        var hiddenCatalog = (Dictionary<string, ItemData>)fieldInfo.GetValue(ItemCatalogService.Instance);

        // 2. Filter by ItemType and canBeBought
        var validItems = hiddenCatalog.Values
            .Where(item => item.canBeBought && item.itemType == _model.ShopType)
            .ToList();

        if (validItems.Count == 0)
        {
            Debug.LogWarning($"[ShopService] Không tìm thấy vật phẩm nào cho loại {_model.ShopType}");
            return;
        }

        // 3. Random 5 item 
        var newDailyItems = new List<ShopItemModel>();
        int itemsToPick = Mathf.Min(ITEMS_PER_DAY, validItems.Count);

        System.Random rng = new System.Random();
        var shuffledItems = validItems.OrderBy(a => rng.Next()).ToList();

        for (int i = 0; i < itemsToPick; i++)
        {
            ItemData itemData = shuffledItems[i];

            
            float randomMultiplier = UnityEngine.Random.Range(0.9f, 1.2f);
            int finalPrice = Mathf.Max(1, Mathf.RoundToInt(itemData.buyPrice * randomMultiplier));
            // original price
            //int finalPrice = itemData.buyPrice;
            newDailyItems.Add(new ShopItemModel(itemData.itemID, finalPrice));
        }

        _model.SetDailyItems(newDailyItems);
        Debug.Log($"[ShopService] Đã tạo mới {newDailyItems.Count} vật phẩm cho {_model.ShopType}");
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