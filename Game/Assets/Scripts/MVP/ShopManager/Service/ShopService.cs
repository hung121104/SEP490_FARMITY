using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ShopService : IShopService
{
    private readonly ShopModel _model;
    private const int ITEMS_PER_DAY = 5;

    public ShopService(List<ItemType> shopTypes)
    {
        _model = new ShopModel(shopTypes);
        GenerateDailyItems();
    }

    public ShopModel GetShopModel() => _model;

    public void GenerateDailyItems()
    {
        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady) return;

        FieldInfo fieldInfo = typeof(ItemCatalogService).GetField("_catalog", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo == null) return;

        var hiddenCatalog = (Dictionary<string, ItemData>)fieldInfo.GetValue(ItemCatalogService.Instance);
        // use contains to filter items 
        var validItems = hiddenCatalog.Values
            .Where(item => item.canBeBought && _model.ShopTypes.Contains(item.itemType))
            .ToList();

        var newShopItems = new List<ShopItemModel>();

        foreach (var itemData in validItems)
        {
            int finalPrice = itemData.buyPrice;
            newShopItems.Add(new ShopItemModel(itemData.itemID, finalPrice));
        }

        _model.SetDailyItems(newShopItems);
        Debug.Log($"[ShopService] Load all Item {newShopItems.Count} item for multiple types");
    }

    public bool TryBuyItem(int slotIndex, IInventoryService playerInventory, bool buyMaxStack = false)
    {
        if (slotIndex < 0 || slotIndex >= _model.DailyItems.Count) return false;

        var shopItem = _model.DailyItems[slotIndex];
        var itemData = ItemCatalogService.Instance.GetItemData(shopItem.ItemId);
        if (itemData == null) return false;

        int amountToBuy = 1;

        if (buyMaxStack)
        {
            // 1. Lấy giới hạn stack của item
            int maxStack = itemData.maxStack > 0 ? itemData.maxStack : 1;

            // 2. Tính số lượng tối đa có thể mua bằng số tiền hiện có
            int affordableAmount = WorldDataManager.Instance.Gold / shopItem.Price;

            // 3. Tính khoảng trống thực tế trong túi đồ có thể chứa bao nhiêu item này
            int spaceAvailable = playerInventory.GetAddableQuantity(itemData, maxStack);

            // Chốt số lượng mua: Lấy con số NHỎ NHẤT trong 3 điều kiện trên
            amountToBuy = Mathf.Min(maxStack, affordableAmount);
            amountToBuy = Mathf.Min(amountToBuy, spaceAvailable);
        }
        else
        {
            // Nếu mua lẻ (không đè Shift), kiểm tra xem túi có chứa nổi 1 cái không
            if (playerInventory.GetAddableQuantity(itemData, 1) < 1)
            {
                Debug.LogWarning("[ShopService] Túi đồ không còn chỗ trống cho item này!");
                return false;
            }
        }

        // Nếu không thể mua cái nào (do hết tiền hoặc đầy túi)
        if (amountToBuy <= 0)
        {
            if (WorldDataManager.Instance.Gold < shopItem.Price)
                Debug.LogWarning("[ShopService] Không đủ tiền!");
            else
                Debug.LogWarning("[ShopService] Túi đồ đã đầy!");

            return false;
        }

        // Trừ tiền tổng và thêm đồ vào túi
        int totalCost = amountToBuy * shopItem.Price;
        if (WorldDataManager.Instance.TrySpendGold(totalCost))
        {
            playerInventory.AddItem(itemData, amountToBuy);
            Debug.Log($"[ShopService] Đã mua {amountToBuy}x {itemData.itemName} với giá {totalCost}G");
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