using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class ShopPresenter
{
    private readonly IShopView _view;
    private  IShopService _shopService;
    private readonly InventoryGameView _inventoryGameView;
    private readonly IInventoryService _playerInventory;
    private readonly IInventoryView _inventoryUI;

    private List<ItemModel> _sellCart = new List<ItemModel>();
    private int _totalSellPrice = 0;

    public ShopPresenter(IShopView view, IShopService shopService, InventoryGameView inventoryGameView, IInventoryService inventoryService)
    {
        _view = view;
        _shopService = shopService;
        _inventoryGameView = inventoryGameView;
        _playerInventory = inventoryService;

        FieldInfo field = typeof(InventoryGameView).GetField("inventoryView", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) _inventoryUI = (IInventoryView)field.GetValue(_inventoryGameView);

     
        _view.OnBuyClicked += HandleBuyClicked;
        _view.OnConfirmSellClicked += HandleConfirmSell;
        _view.OnCloseClicked += HandleCloseShop;

     
        _view.OnItemDroppedToSell += HandleItemDroppedToSell;

    
        if (_inventoryUI != null) _inventoryUI.OnSlotClicked += HandleInventorySlotClicked;

        _view.OnSellSlotClicked += ReturnItemToInventory;

        _view.UpdateShopSlots(_shopService.GetShopModel().DailyItems);
        RefreshSellAreaUI();
    }

    private void HandleBuyClicked(int slotIndex)
    {
        var shopList = _shopService.GetShopModel().DailyItems;
        if (slotIndex < 0 || slotIndex >= shopList.Count) return;
        if (_shopService.TryBuyItem(slotIndex, _playerInventory))
        {
            _view.UpdateShopSlots(shopList);
        }
        else
        {
            _view.FlashPriceRed(slotIndex);
        }
    }
    private void HandleItemDroppedToSell(GameObject draggedObj, int sellSlotIndex)
    {
        var invSlot = draggedObj.GetComponent<InventorySlotView>();
        if (invSlot != null) MoveItemToCart(invSlot.GetSlotIndex(), moveWholeStack: true);
    }

    private void HandleInventorySlotClicked(int invSlotIndex)
    {
        MoveItemToCart(invSlotIndex, moveWholeStack: false);
    }

    
    private void MoveItemToCart(int invSlotIndex, bool moveWholeStack)
    {
        var item = _playerInventory.GetItemAtSlot(invSlotIndex);
        if (item == null) return;

        var itemData = ItemCatalogService.Instance.GetItemData(item.ItemId);
        if (itemData == null || !itemData.canBeSold) return;

      
        int amountToMove = moveWholeStack ? item.Quantity : 1;
        int originalAmountToMove = amountToMove;
        int maxStack = itemData.maxStack > 0 ? itemData.maxStack : 1;

      
        for (int i = 0; i < _sellCart.Count; i++)
        {
            var cartItem = _sellCart[i];
            if (cartItem.ItemId == item.ItemId && cartItem.Quality == item.Quality && cartItem.Quantity < maxStack)
            {
                int spaceLeft = maxStack - cartItem.Quantity;
                int amountToAdd = Mathf.Min(spaceLeft, amountToMove);

                _sellCart[i] = new ItemModel(itemData, cartItem.Quality, cartItem.Quantity + amountToAdd);
                amountToMove -= amountToAdd;

                if (amountToMove <= 0) break;
            }
        }

       
        while (amountToMove > 0 && _sellCart.Count < 6)
        {
            int amountToAdd = Mathf.Min(maxStack, amountToMove);
            ItemModel clonedItem = new ItemModel(itemData, item.Quality, amountToAdd);
            _sellCart.Add(clonedItem);
            amountToMove -= amountToAdd;
        }

      
        int amountSuccessfullyMoved = originalAmountToMove - amountToMove;
        if (amountSuccessfullyMoved > 0)
        {
            _playerInventory.RemoveItem(item.ItemId, amountSuccessfullyMoved);
            RecalculateTotalPrice(); 
            RefreshSellAreaUI();
        }
       
    }

    private void ReturnItemToInventory(int sellSlotIndex)
    {
        if (sellSlotIndex < 0 || sellSlotIndex >= _sellCart.Count) return;

        var cartItem = _sellCart[sellSlotIndex];
        var itemData = ItemCatalogService.Instance.GetItemData(cartItem.ItemId);

        if (itemData != null)
        {
            int amountToReturn = 1; 

            _playerInventory.AddItem(itemData, amountToReturn);


            if (cartItem.Quantity > amountToReturn)
            {
                _sellCart[sellSlotIndex] = new ItemModel(itemData, cartItem.Quality, cartItem.Quantity - amountToReturn);
            }
            else
            {
                _sellCart.RemoveAt(sellSlotIndex);
            }

            RecalculateTotalPrice(); 
            RefreshSellAreaUI();
        }
    }

    private void RecalculateTotalPrice()
    {
        _totalSellPrice = 0;
        foreach (var cartItem in _sellCart)
        {
            var itemData = ItemCatalogService.Instance.GetItemData(cartItem.ItemId);
            if (itemData != null)
            {
                _totalSellPrice += itemData.GetSellPrice(cartItem.Quality) * cartItem.Quantity;
            }
        }
    }

    private void HandleConfirmSell()
    {
        if (_sellCart.Count == 0) return;

        WorldDataManager.Instance.AddGold(_totalSellPrice);
        Debug.Log($"[Shop] sell {_totalSellPrice}G.");

        _sellCart.Clear();
        _totalSellPrice = 0;
        RefreshSellAreaUI();
    }

    public void CloseShop()
    {
        if (_sellCart.Count > 0)
        {
            foreach (var item in _sellCart)
            {
                var itemData = ItemCatalogService.Instance.GetItemData(item.ItemId);
                if (itemData != null) _playerInventory.AddItem(itemData, item.Quantity);
            }
        }

        _sellCart.Clear();
        _totalSellPrice = 0;

        _view.OnBuyClicked -= HandleBuyClicked;
        _view.OnConfirmSellClicked -= HandleConfirmSell;
        _view.OnCloseClicked -= HandleCloseShop;
        _view.OnItemDroppedToSell -= HandleItemDroppedToSell;
        _view.OnSellSlotClicked -= ReturnItemToInventory;
        if (_inventoryUI != null) _inventoryUI.OnSlotClicked -= HandleInventorySlotClicked;

        Debug.Log("[ShopPresenter] Unsubscribed from all inventory events");
    }

    private void HandleCloseShop() { ShopSystemManager.Instance.CloseShopUI(); }

    private void RefreshSellAreaUI()
    {
        if (_view is ShopView mainView) mainView.UpdateSellArea(_sellCart, _totalSellPrice);
    }
    public void RefreshShopData(IShopService newShopService)
    {
        _shopService = newShopService;
        _view.UpdateShopSlots(_shopService.GetShopModel().DailyItems);
    }
}