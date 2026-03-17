using System.Collections.Generic;

[System.Serializable]
public class ShopItemModel
{
    public string ItemId;
    public int Price; // Giá bán (có thể random chênh lệch một chút so với basePrice)
    public bool IsSoldOut; // Đánh dấu nếu người chơi đã mua hết slot này (nếu bạn muốn giới hạn số lượng)

    public ShopItemModel(string id, int price)
    {
        ItemId = id;
        Price = price;
        IsSoldOut = false;
    }
}

public class ShopModel
{
    public ItemType ShopType; // Định danh loại Shop (ví dụ: Blacksmith bán Tool)
    public List<ShopItemModel> DailyItems { get; private set; }

    public ShopModel(ItemType type)
    {
        ShopType = type;
        DailyItems = new List<ShopItemModel>();
    }

    public void SetDailyItems(List<ShopItemModel> items)
    {
        DailyItems = items;
    }
}