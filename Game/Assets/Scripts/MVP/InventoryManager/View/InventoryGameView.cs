using UnityEngine;
using Photon.Pun;

public class InventoryGameView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int inventorySlots = 36;

    [Header("References")]
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private ItemDetailView itemDetailView;
    [SerializeField] private ItemDeleteView itemDeleteView;

    private InventoryModel model;
    private IInventoryService service;
    private InventoryPresenter presenter;

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeInventorySystem();
    }

    private void Start()
    {
        StartCoroutine(RegisterWithNetworkWhenReady());
    }

    private void OnEnable()
    {
        InventorySyncManager.OnInventoryChanged += HandleRemoteInventoryChanged;
    }

    private void OnDisable()
    {
        InventorySyncManager.OnInventoryChanged -= HandleRemoteInventoryChanged;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region Initialization
    private void InitializeInventorySystem()
    {
        // Create Model
        model = new InventoryModel(inventorySlots);

        // Create Service
        var inventoryService = new InventoryService(model);
        service = inventoryService;

        // Create Presenter
        presenter = new InventoryPresenter(model, service);

        // Initialize View
        if (inventoryView != null)
        {
            inventoryView.InitializeSlots(inventorySlots);
            presenter.SetView(inventoryView);
        }

        // Set Item Detail View
        if (itemDetailView != null)
        {
            presenter.SetItemDetailView(itemDetailView);
        }

        // Subscribe to presenter events
        presenter.OnItemUsed += HandleItemUsed;
        presenter.OnItemDropped += HandleItemDropped;
    }

    /// <summary>
    /// Wait for InventorySyncManager to be ready, then register.
    /// Retry logic is handled inside RegisterLocalPlayerInventory itself.
    /// </summary>
    private System.Collections.IEnumerator RegisterWithNetworkWhenReady()
    {
        yield return new WaitUntil(() => InventorySyncManager.Instance != null);
        RegisterWithNetwork();
    }

    /// <summary>
    /// Register local player's inventory with InventorySyncManager and enable network sync.
    /// Follows the same late-init pattern as CropPlantingService.
    /// </summary>
    private void RegisterWithNetwork()
    {
        if (InventorySyncManager.Instance == null)
        {
            Debug.LogWarning("[InventoryGameView] InventorySyncManager not available — network sync disabled.");
            return;
        }

        // Register character on Master
        InventorySyncManager.Instance.RegisterLocalPlayerInventory((byte)inventorySlots);

        // Enable auto-sync in the service layer
        if (service is InventoryService concreteService)
        {
            concreteService.NetworkSyncEnabled = true;
        }

        Debug.Log("[InventoryGameView] Network inventory sync enabled.");
    }
    #endregion

    /// <summary>
    /// Called when InventorySyncManager receives a remote slot change.
    /// Reads the authoritative data from InventoryDataModule and refreshes local InventoryModel.
    /// </summary>
    private void HandleRemoteInventoryChanged()
    {
        if (InventorySyncManager.Instance == null) return;
        string charId = InventorySyncManager.Instance.LocalCharacterId;
        if (string.IsNullOrEmpty(charId)) return;

        var module = WorldDataManager.Instance?.InventoryData;
        if (module == null) return;

        var inv = module.GetInventory(charId);
        if (inv == null) return;

        // ═══ ACTION COOLDOWN CHECK ═══
        // Skip sync if user is currently performing actions (drag, drop, sort)
        // This prevents race conditions where remote changes conflict with local UI updates
        if (presenter != null && !presenter.IsReadyToSync())
        {
            Debug.Log("[InventoryGameView] User performing action, deferring remote sync...");
            return; // Sync will be retried on next OnInventoryChanged event
        }

        // Delegate all Model mutations to Service — GameView never touches Model directly
        if (service is InventoryService concreteService)
        {
            concreteService.ApplyRemoteInventoryState(inv, inventorySlots);
        }

        Debug.Log("[InventoryGameView] ✓ Remote inventory sync applied successfully");
    }

    public void OpenInventory()
    {
        if (inventoryView != null)
        {
            //Re-enable drops when opening inventory
            if (itemDeleteView != null)
            {
                itemDeleteView.EnableDrops();
            }
            Debug.Log("[InventoryGameView] Inventory opened");
        }
    }

    public void CloseInventory()
    {
        if (inventoryView != null)
        {
            presenter?.CancelAllActions();
            Debug.Log("[InventoryGameView] Inventory closed");
        }
    }

    #region Public API for Player/Other Systems

    public bool AddItem(string itemId, int quantity = 1, Quality quality = Quality.Normal)
    {
        return presenter.TryAddItem(itemId, quantity, quality);
    }

    public bool AddItem(ItemData itemData, int quantity = 1, Quality quality = Quality.Normal)
    {
        return presenter.TryAddItem(itemData, quantity, quality);
    }

    public bool RemoveItem(string itemId, int quantity)
    {
        return presenter.TryRemoveItem(itemId, quantity);
    }

    public bool HasItem(string itemId, int quantity = 1)
    {
        return presenter.HasItem(itemId, quantity);
    }

    public int GetItemCount(string itemId)
    {
        return presenter.GetItemCount(itemId);
    }

    public IInventoryService GetInventoryService() => service;
    public InventoryModel GetInventoryModel() => model;
    public int GetInventorySlotCount() => model.maxSlots;

    /// <summary>
    /// Register a secondary InventoryView (e.g. crafting panel) that receives
    /// data-only updates from the single InventoryPresenter.
    /// The secondary view still handles its own input (drag/drop) locally.
    /// </summary>
    public void RegisterSecondaryView(IInventoryView secondaryView)
    {
        presenter?.AddSecondaryView(secondaryView);
    }

    /// <summary>
    /// Unregister a secondary InventoryView.
    /// </summary>
    public void UnregisterSecondaryView(IInventoryView secondaryView)
    {
        presenter?.RemoveSecondaryView(secondaryView);
    }

    /// <summary>
    /// Called by CraftingInventoryAdapter (or other secondary views) when the user
    /// performs an action (drag, drop, sort, delete) on that view.
    /// Resets the main presenter's action cooldown so HandleRemoteInventoryChanged
    /// correctly defers the echo broadcast and doesn't revert local changes.
    /// </summary>
    public void NotifyExternalAction()
    {
        presenter?.NotifyExternalAction();
    }
    #endregion
    
    #region Event Handlers

    private void HandleItemUsed(ItemModel item)
    {
        Debug.Log($"Using item: {item.ItemName}");

        // TODO: Implement item usage logic
        // Example: Apply consumable effects, equip tool, etc.
    }

    private void HandleItemDropped(ItemModel item)
    {
        Debug.Log($"Dropping item: {item.ItemName}");

        // Delegate to DroppedItemManagerView which handles Photon sync + DB persistence
        if (DroppedItemManagerView.Instance != null)
        {
            DroppedItemManagerView.Instance.RequestDropItem(item);
        }
        else
        {
            Debug.LogError("[InventoryGameView] DroppedItemManagerView.Instance is null — cannot drop item!");
        }
    }

    #endregion

    #region Item Usage Implementations

    private void UseConsumableItem(ItemModel item)
    {
        Debug.Log($"Consuming: {item.ItemName}");
        presenter.TryRemoveItem(item.ItemId, 1);
    }

    #endregion

    #region Cleanup

    private void Cleanup()
    {
        if (presenter != null)
        {
            presenter.OnItemUsed -= HandleItemUsed;
            presenter.OnItemDropped -= HandleItemDropped;
            presenter.Cleanup();
        }
    }

    #endregion
}
