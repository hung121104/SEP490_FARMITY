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

        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManagerView>();
        }

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
        if (inventoryGameView != null) inventoryGameView.OpenInventory();

        shopMainView.ToggleExternalInventory(true);
        shopMainView.ToggleInventoryTabs(false);
        shopMainView.ToggleHotbar(false);

        if (inventoryDropZone != null)
        {
            inventoryDropZone.AllowDropOutside = false;
        }
       
    }

    public void CloseShopUI()
    {
        
        isShopOpen = false;

        shopPresenter?.CloseShop();
        shopPresenter = null;
        shopMainView.SetVisible(false);
        inventoryGameView?.CloseInventory();
        shopMainView.ToggleExternalInventory(false);
        shopMainView.ToggleInventoryTabs(true);
        shopMainView.ToggleHotbar(true);

        if (inventoryDropZone != null)
        {
            inventoryDropZone.AllowDropOutside = true;
        }
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

        Debug.Log("[Shop] Shop reset.");
    }
}