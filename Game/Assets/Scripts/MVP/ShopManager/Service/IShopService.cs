using System.Collections.Generic;

public interface IShopService
{
    ShopModel GetShopModel();
    void GenerateDailyItems();
    bool TryBuyItem(int slotIndex, IInventoryService playerInventory, bool buyMaxStack = false);
    bool SellItem(string itemId, int quantity, IInventoryService playerInventory);
}