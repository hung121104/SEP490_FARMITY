using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopView : MonoBehaviour, IShopView
{
    public static ShopView Instance { get; private set; }

    [Header("Main UI Panel")]
    [Tooltip("Kéo cái ShopPanel ở trong Canvas vào đây")]
    [SerializeField] private GameObject shopPanelGameObject; 

    [Header("Shop Slots")]
    [SerializeField] private List<ShopSlotView> shopSlots;

    [Header("Sell Area")]
    [SerializeField] private Button selectItemToSellButton;
    [SerializeField] private Image sellItemIcon;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private Button confirmSellButton;

    [Header("General")]
    [SerializeField] private Button closeButton;
    public bool IsVisible => shopPanelGameObject != null && shopPanelGameObject.activeSelf;

    // Events
    public event Action<int> OnBuyClicked;
    public event Action OnOpenInventoryToSellClicked;
    public event Action OnConfirmSellClicked;
    public event Action OnCloseClicked;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < shopSlots.Count; i++)
        {
            shopSlots[i].OnBuyClicked += (slotIndex) => OnBuyClicked?.Invoke(slotIndex);
        }

        selectItemToSellButton.onClick.AddListener(() => OnOpenInventoryToSellClicked?.Invoke());
        confirmSellButton.onClick.AddListener(() => OnConfirmSellClicked?.Invoke());
        closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());

        ClearSellSlot();
        SetVisible(false);
    }

   
    public void SetVisible(bool isVisible)
    {
        if (shopPanelGameObject != null)
        {
            shopPanelGameObject.SetActive(isVisible);
        }
        else
        {
            Debug.LogError("[ShopView] Shop panel null!");
        }
    }

    public void UpdateShopSlots(List<ShopItemModel> items)
    {
        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (i < items.Count)
            {
                var itemModel = items[i];
                var data = ItemCatalogService.Instance.GetItemData(itemModel.ItemId);
                var icon = ItemCatalogService.Instance.GetCachedSprite(itemModel.ItemId);

                shopSlots[i].gameObject.SetActive(true);
                shopSlots[i].Setup(i, icon, data.itemName, itemModel.Price, itemModel.IsSoldOut);
            }
            else
            {
                shopSlots[i].gameObject.SetActive(false);
            }
        }
    }

    public void FlashPriceRed(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < shopSlots.Count)
        {
            shopSlots[slotIndex].FlashRed();
        }
    }

    public void ShowItemToSell(ItemModel item, int sellPrice)
    {
        sellItemIcon.sprite = item.Icon;
        sellItemIcon.color = Color.white;
        sellPriceText.text = $"Sell: {sellPrice}G";
        confirmSellButton.interactable = true;
    }

    public void ClearSellSlot()
    {
        sellItemIcon.sprite = null;
        sellItemIcon.color = new Color(1, 1, 1, 0);
        sellPriceText.text = "Select Item";
        confirmSellButton.interactable = false;
    }
}