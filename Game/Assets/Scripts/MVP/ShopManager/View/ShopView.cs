using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopView : MonoBehaviour, IShopView, IDropHandler
{
    public static ShopView Instance { get; private set; }

    [Header("Main UI Panels")]
    [SerializeField] private GameObject shopPanelGameObject;
    [SerializeField] private GameObject mainInventoryPanel;
    [SerializeField] private GameObject inventoryTabsObject;
    [SerializeField] private GameObject hotbarObject;

    [Header("Buy Area")]
    [SerializeField] private Transform buyContentContainer;
    [SerializeField] private ShopSlotView shopSlotPrefab;

    [Header("Sell Area (Auto Spawn 3x2)")]
    [Tooltip("Kéo object Grid Layout Group vào đây")]
    [SerializeField] private RectTransform sellSlotsContainer;
    [Tooltip("Kéo Prefab SellSlotView vào đây")]
    [SerializeField] private SellSlotView sellSlotPrefab;
    [SerializeField] private TextMeshProUGUI totalSellPriceText;
    [SerializeField] private Button confirmSellButton;

    [Header("General")]
    [SerializeField] private Button closeButton;

    public bool IsVisible => shopPanelGameObject != null && shopPanelGameObject.activeSelf;

    private List<ShopSlotView> _spawnedBuySlots = new List<ShopSlotView>();

    private List<SellSlotView> _spawnedSellSlots = new List<SellSlotView>();
    private const int MAX_SELL_SLOTS = 6;

    public event Action<int> OnBuyClicked;
    public event Action OnConfirmSellClicked;
    public event Action OnCloseClicked;
    public event Action<GameObject, int> OnItemDroppedToSell;
    public event Action<int> OnSellSlotClicked;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (confirmSellButton != null) confirmSellButton.onClick.AddListener(() => OnConfirmSellClicked?.Invoke());
        if (closeButton != null) closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());

        
        InitializeSellSlots();

        SetVisible(false);
    }

    public void SetVisible(bool isVisible)
    {
        if (shopPanelGameObject != null) shopPanelGameObject.SetActive(isVisible);
    }

    public void ToggleExternalInventory(bool isVisible)
    {
        if (mainInventoryPanel != null) mainInventoryPanel.SetActive(isVisible);
    }

    public void UpdateShopSlots(List<ShopItemModel> items)
    {
        if (shopSlotPrefab == null || buyContentContainer == null) return;

       
        foreach (var slot in _spawnedBuySlots) Destroy(slot.gameObject);
        _spawnedBuySlots.Clear();

      
        for (int i = 0; i < items.Count; i++)
        {
            var itemModel = items[i];
            var data = ItemCatalogService.Instance.GetItemData(itemModel.ItemId);
            var icon = ItemCatalogService.Instance.GetCachedSprite(itemModel.ItemId);

            ShopSlotView newSlot = Instantiate(shopSlotPrefab, buyContentContainer);
            newSlot.Setup(i, icon, data.itemName, itemModel.Price, itemModel.IsSoldOut);

            int slotIndex = i;
            newSlot.OnBuyClicked += (idx) => OnBuyClicked?.Invoke(idx);

            _spawnedBuySlots.Add(newSlot);
        }
    }

    public void FlashPriceRed(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _spawnedBuySlots.Count)
        {
            _spawnedBuySlots[slotIndex].FlashRed();
        }
    }
    private void InitializeSellSlots()
    {
        if (sellSlotPrefab == null || sellSlotsContainer == null) return;

       
        foreach (Transform child in sellSlotsContainer) Destroy(child.gameObject);
        _spawnedSellSlots.Clear();

       
        for (int i = 0; i < MAX_SELL_SLOTS; i++)
        {
            SellSlotView slot = Instantiate(sellSlotPrefab, sellSlotsContainer);
            slot.Setup(i);

            slot.OnItemDropped += (draggedObj, slotIndex) => OnItemDroppedToSell?.Invoke(draggedObj, slotIndex);
            slot.OnSlotClicked += (slotIndex) => OnSellSlotClicked?.Invoke(slotIndex);

            _spawnedSellSlots.Add(slot);
        }
    }

    public void UpdateSellArea(List<ItemModel> itemsInSellCart, int totalPrice)
    {
        for (int i = 0; i < _spawnedSellSlots.Count; i++)
        {
            if (i < itemsInSellCart.Count)
            {
               
                _spawnedSellSlots[i].SetItem(itemsInSellCart[i].Icon, itemsInSellCart[i].Quantity);
            }
            else
            {
                _spawnedSellSlots[i].ClearItem();
            }
        }

        if (totalSellPriceText != null) totalSellPriceText.text = $"Total: {totalPrice} G";
        if (confirmSellButton != null) confirmSellButton.interactable = itemsInSellCart.Count > 0;
    }
    public void ToggleInventoryTabs(bool isVisible)
    {
        if (inventoryTabsObject != null)
        {
            inventoryTabsObject.SetActive(isVisible);
        }
    }
    public void ToggleHotbar(bool isVisible)
    {
        if (hotbarObject != null)
        {
            hotbarObject.SetActive(isVisible);
        }
    }
   
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

      
        if (sellSlotsContainer != null &&
            RectTransformUtility.RectangleContainsScreenPoint(sellSlotsContainer, eventData.position, eventData.pressEventCamera))
        {
           
            OnItemDroppedToSell?.Invoke(eventData.pointerDrag, -1);
        }
    }
}