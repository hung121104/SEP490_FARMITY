using UnityEngine;
using System.Collections.Generic;

public class ShopSystemManager : MonoBehaviour
{
    public static ShopSystemManager Instance { get; private set; }

    /// <summary>
    /// Fires after the shop UI has been closed — regardless of whether the close was
    /// initiated by the close button, the presenter, or ShopTrigger. Used by ShopTrigger
    /// to clean up its player-input lock when the UI is closed from outside.
    /// </summary>
    public static event System.Action OnShopClosed;

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

    private void OnEnable()
    {
        ItemCatalogService.OnItemUpdated -= HandleItemCatalogUpdated;
        ItemCatalogService.OnItemUpdated += HandleItemCatalogUpdated;
    }

    private void OnDisable()
    {
        ItemCatalogService.OnItemUpdated -= HandleItemCatalogUpdated;

        if (timeManager != null)
            timeManager.OnDayChanged -= ResetAllShopsForNewDay;
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

        // Register shop panel as safe zone so items don't drop to world
        if (inventoryGameView != null && shopMainView.SafeZone != null)
            inventoryGameView.SetAdditionalSafeZone(shopMainView.SafeZone);

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

        // Unregister shop safe zone
        if (inventoryGameView != null)
            inventoryGameView.SetAdditionalSafeZone(null);

        shopMainView.ToggleHotbar(true);

        OnShopClosed?.Invoke();
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

    private void HandleItemCatalogUpdated(string itemId)
    {
        // Rebuild cached NPC stocks so buy lists reflect latest catalog fields
        // (price/name/icon/buy flags/type membership) without waiting for day reset.
        RefreshAllCachedShopsAfterItemUpdate();
    }

    private void RefreshAllCachedShopsAfterItemUpdate()
    {
        if (dailyShopsMemory.Count == 0) return;
        if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady) return;

        var refreshedShops = new Dictionary<string, IShopService>(dailyShopsMemory.Count);

        foreach (var pair in dailyShopsMemory)
        {
            var cachedService = pair.Value;
            var model = cachedService?.GetShopModel();
            var shopTypes = model?.ShopTypes;
            if (shopTypes == null)
            {
                refreshedShops[pair.Key] = cachedService;
                continue;
            }

            var refreshedService = new ShopService(shopTypes);
            refreshedService.GenerateDailyItems();
            refreshedShops[pair.Key] = refreshedService;
        }

        dailyShopsMemory = refreshedShops;

        if (isShopOpen && shopPresenter != null && !string.IsNullOrEmpty(currentShopKey)
            && dailyShopsMemory.TryGetValue(currentShopKey, out var openShopService))
        {
            shopPresenter.RefreshShopData(openShopService);
        }
    }
}