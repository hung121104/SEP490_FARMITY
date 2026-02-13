using UnityEngine;

public class InventoryGameView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int inventorySlots = 36;

    [Header("References")]
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private ItemDetailView itemDetailView;

    private InventoryModel model;
    private IInventoryService service;
    private InventoryPresenter presenter;

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeInventorySystem();
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
        service = new InventoryService(model);

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
    #endregion

    #region Public API for Player/Other Systems

    public bool AddItem(ItemDataSO itemData, int quantity = 1, Quality quality = Quality.Normal)
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

        // TODO: Implement item drop logic
        // Example: Spawn item in world at player position
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
