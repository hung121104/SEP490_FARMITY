using System;
using System.Collections.Generic;

public interface IShopView
{
    void UpdateShopSlots(List<ShopItemModel> items);

    // effect
    void FlashPriceRed(int slotIndex);

    void SetVisible(bool isVisible);
    void ShowItemToSell(ItemModel item, int sellPrice);
   
    void ClearSellSlot();

    
    event Action<int> OnBuyClicked; 
    event Action OnOpenInventoryToSellClicked; 
    event Action OnConfirmSellClicked; 
    event Action OnCloseClicked; 
}