using UnityEngine;

public class CraftingInventoryAdapter : MonoBehaviour
{
    #region Serialized Fields

    [Header("Core Reference")]
    [Tooltip("Drag the existing InventoryView component here")]
    [SerializeField] private InventoryView inventoryView;

    [Header("Inventory Settings")]
    [Tooltip("Number of inventory slots to display")]
    [SerializeField] private int inventorySlots = 36;

    [Header("Optional")]
    [Tooltip("Assign to enable item detail tooltip on hover (optional)")]
    [SerializeField] private ItemDetailView itemDetailView;

    [Tooltip("Assign to enable delete zone (drag item here to delete) (optional)")]
    [SerializeField] private ItemDeleteView itemDeleteView;

    #endregion

    #region Private Fields

    private InventoryModel inventoryModel;
    private IInventoryService inventoryService;

    // Presenter that wires up drag/drop, hover tooltip, and all slot interactions
    private InventoryPresenter inventoryPresenter;

    // Track whether the adapter has been initialized
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInventoryEvents();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Validates that required references are assigned in the Inspector.
    /// </summary>
    private void ValidateReferences()
    {
        if (inventoryView == null)
        {
            Debug.LogError("[CraftingInventoryAdapter] InventoryView reference is missing! " +
                           "Please assign it in the Inspector.");
        }
    }

    /// <summary>
    /// Injects the shared inventory model and service from the main InventoryGameView.
    /// Call this from CraftingGameView after getting references from InventoryGameView.
    /// </summary>
    /// <param name="model">The shared InventoryModel</param>
    /// <param name="service">The shared IInventoryService</param>
    public void InjectInventory(InventoryModel model, IInventoryService service)
    {
        if (model == null || service == null)
        {
            Debug.LogError("[CraftingInventoryAdapter] Cannot inject null model or service.");
            return;
        }

        // Unsubscribe from previous service if re-injecting
        UnsubscribeFromInventoryEvents();

        inventoryModel = model;
        inventoryService = service;

        // Initialize the slot grid
        inventoryView?.InitializeSlots(inventoryModel.maxSlots);

        // Create and wire up a presenter so drag/drop and item detail tooltips work
        inventoryPresenter = new InventoryPresenter(inventoryModel, inventoryService);
        inventoryPresenter.SetView(inventoryView);

        // Wire item detail view if one is assigned
        if (itemDetailView != null)
        {
            inventoryPresenter.SetItemDetailView(itemDetailView);
        }

        // Wire delete zone if one is assigned
        if (itemDeleteView != null)
        {
            inventoryView.SetDeleteZone(itemDeleteView);
            itemDeleteView.Show();
        }

        isInitialized = true;

        Debug.Log("[CraftingInventoryAdapter] Inventory injected and initialized.");
    }

    public void OpenInventoryCrafting()
    {
        if (itemDeleteView != null)
        {
            itemDeleteView.EnableDrops();
            Debug.Log("[CraftingInventoryAdapter] Delete zone enabled");
        }
    }

    public void CloseInventoryCrafting()
    {
        if (inventoryPresenter != null)
        {
            inventoryPresenter.CancelAllActions();
        }
        Debug.Log("[CraftingInventoryAdapter] Inventory closed");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Updates a single slot with the given item.
    /// </summary>
    /// <param name="slotIndex">Index of the slot to update</param>
    /// <param name="item">Item to display, or null to clear the slot</param>
    public void UpdateSlot(int slotIndex, ItemModel item)
    {
        inventoryView?.UpdateSlot(slotIndex, item);
    }

    /// <summary>
    /// Clears a single slot, removing any displayed item.
    /// </summary>
    /// <param name="slotIndex">Index of the slot to clear</param>
    public void ClearSlot(int slotIndex)
    {
        inventoryView?.ClearSlot(slotIndex);
    }

    /// <summary>
    /// Refreshes all slots to match the current inventory state.
    /// </summary>
    public void RefreshAllSlots()
    {
        if (!ValidateInventoryReferences()) return;

        for (int i = 0; i < inventoryModel.maxSlots; i++)
        {
            var item = inventoryService.GetItemAtSlot(i);
            inventoryView.UpdateSlot(i, item);
        }
    }

    /// <summary>
    /// Returns the underlying IInventoryView for use by external presenters.
    /// </summary>
    public IInventoryView GetInventoryView() => inventoryView;

    /// <summary>
    /// Returns whether this adapter has been initialized with an inventory.
    /// </summary>
    public bool IsInitialized => isInitialized;

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleans up the presenter and unsubscribes all events.
    /// </summary>
    private void UnsubscribeFromInventoryEvents()
    {
        if (inventoryPresenter != null)
        {
            inventoryPresenter.Cleanup();
            inventoryPresenter = null;
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Checks that both inventoryModel and inventoryService are valid before use.
    /// </summary>
    private bool ValidateInventoryReferences()
    {
        if (inventoryModel == null || inventoryService == null)
        {
            Debug.LogWarning("[CraftingInventoryAdapter] Inventory model or service is not set. " +
                             "Call InjectInventory() first.");
            return false;
        }
        return true;
    }

    #endregion
}
