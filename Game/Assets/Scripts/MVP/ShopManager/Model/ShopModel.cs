using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopItemModel
{
    public string ItemId;
    public int Price; 
    public bool IsSoldOut; 

    public ShopItemModel(string id, int price)
    {
        ItemId = id;
        Price = price;
        IsSoldOut = false;
    }
}

public class ShopModel
{
    public List<ItemType> ShopTypes;
    public List<ShopItemModel> DailyItems { get; private set; }

    public ShopModel(List<ItemType> types)
    {
        ShopTypes = types;
        DailyItems = new List<ShopItemModel>();
    }

    public void SetDailyItems(List<ShopItemModel> items)
    {
        DailyItems = items;
    }
}