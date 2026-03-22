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

    private List<ItemType> currentOpenShopTypes;
    private string currentShopKey;
    private bool isShopOpen = false;

    private Dictionary<string, IShopService> dailyShopsMemory = new Dictionary<string, IShopService>();

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

    private string GetShopKey(List<ItemType> types)
    {
        return string.Join("_", types);
    }

    public void OpenShopUI(List<ItemType> shopTypes)
    {
        currentOpenShopTypes = shopTypes;
        currentShopKey = GetShopKey(shopTypes);
        isShopOpen = true;

        if (!dailyShopsMemory.ContainsKey(currentShopKey))
        {
            IShopService newShopService = new ShopService(shopTypes);
            newShopService.GenerateDailyItems();
            dailyShopsMemory.Add(currentShopKey, newShopService);
        }

        IShopService currentShopService = dailyShopsMemory[currentShopKey];
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
            inventoryGameView.OpenInventory();
            inventoryGameView.CloseInventory();
        }

        if (inventoryDropZone != null) inventoryDropZone.AllowDropOutside = true;
        shopMainView.ToggleHotbar(true);
    }

    private void ResetAllShopsForNewDay()
    {
        dailyShopsMemory.Clear();
        if (isShopOpen && shopPresenter != null && currentOpenShopTypes != null)
        {
            IShopService refreshedShopService = new ShopService(currentOpenShopTypes);
            refreshedShopService.GenerateDailyItems();
            dailyShopsMemory.Add(currentShopKey, refreshedShopService);
            shopPresenter.RefreshShopData(refreshedShopService);
        }
    }
}