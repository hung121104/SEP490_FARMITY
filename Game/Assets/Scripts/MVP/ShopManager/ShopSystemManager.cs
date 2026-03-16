using UnityEngine;
using System.Collections.Generic;

public class ShopSystemManager : MonoBehaviour
{
    public static ShopSystemManager Instance { get; private set; }

    [Header("Inventory References")]
    [SerializeField] private InventoryGameView inventoryGameView;
    [SerializeField] private InventoryDropZone inventoryDropZone;

    [Header("Time System")]
    [SerializeField] private TimeManagerView timeManager;

    [Header("UI Shop Views")]
    [SerializeField] private ShopView shopMainView;
    [Tooltip("Kéo object InventoryDockPanel vào đây để nhét túi đồ vào")]
    [SerializeField] private Transform shopMainPanel;

    private IInventoryService inventoryService;
    private ShopPresenter shopPresenter;
    private ItemType currentOpenShopType;
    private bool isShopOpen = false;

    private Dictionary<ItemType, IShopService> dailyShopsMemory = new Dictionary<ItemType, IShopService>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        shopMainView?.SetVisible(false);
    }

    private void Start()
    {
        InitializeInventoryReferences();

        if (timeManager == null) timeManager = FindFirstObjectByType<TimeManagerView>();

        if (timeManager != null)
        {
            timeManager.OnDayChanged -= ResetAllShopsForNewDay;
            timeManager.OnDayChanged += ResetAllShopsForNewDay;
        }
    }

    private void InitializeInventoryReferences()
    {
        if (inventoryGameView == null) inventoryGameView = FindFirstObjectByType<InventoryGameView>();
        if (inventoryGameView != null)
        {
            inventoryService = inventoryGameView.GetInventoryService();
        }
    }

    public void OpenShopUI(ItemType shopType)
    {
        currentOpenShopType = shopType;
        isShopOpen = true;

        if (!dailyShopsMemory.ContainsKey(shopType))
        {
            IShopService newShopService = new ShopService(shopType);
            newShopService.GenerateDailyItems();
            dailyShopsMemory.Add(shopType, newShopService);
        }

        IShopService currentShopService = dailyShopsMemory[shopType];
        shopPresenter = new ShopPresenter(shopMainView, currentShopService, inventoryGameView, inventoryService);

        shopMainView.SetVisible(true);

        if (inventoryGameView != null)
        {
            inventoryGameView.OpenCraftingInventory(shopMainPanel);
        }

        if (inventoryDropZone != null) inventoryDropZone.AllowDropOutside = false;
        shopMainView.ToggleHotbar(false);
    }

    public void CloseShopUI()
    {
        isShopOpen = false;
        shopPresenter?.CloseShop();
        shopPresenter = null;

        shopMainView.SetVisible(false);

        if (inventoryGameView != null)
        {
            // Open first → forces inventory back to original parent while ACTIVE
            // so Unity layout system can rebuild VerticalLayoutGroup/HorizontalLayoutGroup.
            // Then close to hide it. Without this, slots don't render next time it's opened.
            inventoryGameView.OpenInventory();
            inventoryGameView.CloseInventory();
        }

        if (inventoryDropZone != null) inventoryDropZone.AllowDropOutside = true;
        shopMainView.ToggleHotbar(true);
    }

    private void ResetAllShopsForNewDay()
    {
        dailyShopsMemory.Clear();
        if (isShopOpen && shopPresenter != null)
        {
            IShopService refreshedShopService = new ShopService(currentOpenShopType);
            refreshedShopService.GenerateDailyItems();
            dailyShopsMemory.Add(currentOpenShopType, refreshedShopService);
            shopPresenter.RefreshShopData(refreshedShopService);
        }
    }
}