using UnityEngine;
using System.Reflection;

public class ShopPresenter
{
    private readonly IShopView _view;
    private readonly IShopService _shopService;
    private readonly InventoryGameView _inventoryGameView;
    private readonly IInventoryService _playerInventory;
    private readonly IInventoryView _inventoryUI;

    private ItemModel _currentItemToSell;

    public ShopPresenter(IShopView view, IShopService shopService, InventoryGameView inventoryGameView)
    {
        _view = view;
        _shopService = shopService;
        _inventoryGameView = inventoryGameView;
        _playerInventory = _inventoryGameView.GetInventoryService();

       
        FieldInfo field = typeof(InventoryGameView).GetField("inventoryView", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            _inventoryUI = (IInventoryView)field.GetValue(_inventoryGameView);
        }

        _view.OnBuyClicked += HandleBuyClicked;
        _view.OnOpenInventoryToSellClicked += HandleOpenInventoryToSell;
        _view.OnConfirmSellClicked += HandleConfirmSell;
        _view.OnCloseClicked += HandleCloseShop;

        _view.UpdateShopSlots(_shopService.GetShopModel().DailyItems);
    }

    private void HandleBuyClicked(int slotIndex)
    {
        var shopItem = _shopService.GetShopModel().DailyItems[slotIndex];

        // 1. Kiểm tra tiền
        if (WorldDataManager.Instance.Gold < shopItem.Price)
        {
            _view.FlashPriceRed(slotIndex);
            return;
        }

        // 2. Tiến hành mua
        bool success = _shopService.TryBuyItem(slotIndex, _playerInventory);

        if (success)
        {
            Debug.Log($"[Shop] Đã mua thành công {shopItem.ItemId}");

            // --- THÊM DÒNG NÀY ĐỂ REFRESH UI NGAY LẬP TỨC ---
            // Nó sẽ đọc lại trạng thái IsSoldOut từ Model và làm mờ nút bấm
            _view.UpdateShopSlots(_shopService.GetShopModel().DailyItems);
        }
    }

    private void HandleOpenInventoryToSell()
    {
        if (_inventoryUI == null) return;

        _view.ClearSellSlot();
        _view.SetVisible(false); 
        _inventoryGameView.OpenInventory(); 
        _view.ToggleExternalInventory(true); 

        _inventoryUI.OnSlotClicked -= OnInventorySlotClickedForSell;
        _inventoryUI.OnSlotClicked += OnInventorySlotClickedForSell;
    }

    private void OnInventorySlotClickedForSell(int slotIndex)
    {
        _inventoryUI.OnSlotClicked -= OnInventorySlotClickedForSell;

        _inventoryGameView.CloseInventory(); 
        _view.ToggleExternalInventory(false); 

        _view.SetVisible(true); 

        var item = _playerInventory.GetItemAtSlot(slotIndex);
        if (item != null)
        {
            var itemData = ItemCatalogService.Instance.GetItemData(item.ItemId);
            if (itemData != null && itemData.canBeSold)
            {
                _currentItemToSell = item;
                int sellPrice = itemData.GetSellPrice(item.Quality);
                _view.ShowItemToSell(item, sellPrice);
            }
        }
    }

    private void HandleConfirmSell()
    {
        if (_currentItemToSell == null) return;
        if (_shopService.SellItem(_currentItemToSell.ItemId, 1, _playerInventory))
        {
            _currentItemToSell = null;
            _view.ClearSellSlot();
        }
    }

    private void HandleCloseShop()
    {
        _currentItemToSell = null;
        _view.ClearSellSlot();
        _view.SetVisible(false);
        Dispose();
    }

    public void Dispose()
    {
        _view.OnBuyClicked -= HandleBuyClicked;
        _view.OnOpenInventoryToSellClicked -= HandleOpenInventoryToSell;
        _view.OnConfirmSellClicked -= HandleConfirmSell;
        _view.OnCloseClicked -= HandleCloseShop;

        if (_inventoryUI != null) _inventoryUI.OnSlotClicked -= OnInventorySlotClickedForSell;
    }
}