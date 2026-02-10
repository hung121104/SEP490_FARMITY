using UnityEngine;

public class InventoryGameView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int inventorySlots = 36;

    [Header("References")]
    [SerializeField] private InventoryView inventoryView;

    private InventoryModel model;
    private IInventoryService service;
    private InventoryPresenter presenter;

    private void Awake()
    {
        InitializeInventorySystem();
    }

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

        // Subscribe to presenter events
        presenter.OnItemUsed += HandleItemUsed;
        presenter.OnItemDropped += HandleItemDropped;
    }

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

    private void HandleItemUsed(InventoryItem item)
    {
        Debug.Log($"Using item: {item.ItemName}");

        // TODO: Implement item usage logic
        // Example: Apply consumable effects, equip tool, etc.
    }

    private void HandleItemDropped(InventoryItem item)
    {
        Debug.Log($"Dropping item: {item.ItemName}");

        // TODO: Implement item drop logic
        // Example: Spawn item in world at player position
    }

    #endregion

    private void OnDestroy()
    {
        if (presenter != null)
        {
            presenter.OnItemUsed -= HandleItemUsed;
            presenter.OnItemDropped -= HandleItemDropped;
            presenter.Cleanup();
        }
    }
}
