using System;
using System.Collections.Generic;
using UnityEngine;

public interface IShopView
{
    void UpdateShopSlots(List<ShopItemModel> items);
    void FlashPriceRed(int slotIndex);
    void SetVisible(bool isVisible);
    void ToggleExternalInventory(bool isVisible);
    void ToggleInventoryTabs(bool isVisible);
    void ToggleHotbar(bool isVisible);

    void UpdateSellArea(List<ItemModel> itemsInSellCart, int totalPrice);

    event Action<int> OnBuyClicked;
    event Action OnConfirmSellClicked;
    event Action OnCloseClicked;
    event Action<GameObject, int> OnItemDroppedToSell;
    event Action<int> OnSellSlotClicked;
}