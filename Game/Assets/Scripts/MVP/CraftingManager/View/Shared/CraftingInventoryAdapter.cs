using UnityEngine;

public class CraftingInventoryAdapter : MonoBehaviour
{
    #region Serialized Fields

    [Header("Core Reference")]
    [Tooltip("Drag the existing InventoryView component here")]
    [SerializeField] private InventoryView inventoryView;

    [Header("Optional")]
    [SerializeField] private ItemDetailView itemDetailView;
    [SerializeField] private ItemDeleteView itemDeleteView;

    #endregion

    #region Private Fields

    private InventoryModel inventoryModel;
    private IInventoryService inventoryService;
    private InventoryPresenter inventoryPresenter;
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    #endregion

    #region Initialization

    private void ValidateReferences()
    {
        if (inventoryView == null)
            Debug.LogError($"[{gameObject.name}] InventoryView reference is missing! " +
                           "Please assign it in the Inspector.");
    }

    public void InjectInventory(InventoryModel model, IInventoryService service)
    {
        if (model == null || service == null)
        {
            Debug.LogError($"[{gameObject.name}] Cannot inject null model or service.");
            return;
        }

        Cleanup();

        inventoryModel = model;
        inventoryService = service;

        inventoryView?.InitializeSlots(inventoryModel.maxSlots);

        inventoryPresenter = new InventoryPresenter(inventoryModel, inventoryService);
        inventoryPresenter.SetView(inventoryView);

        if (itemDetailView != null)
            inventoryPresenter.SetItemDetailView(itemDetailView);

        if (itemDeleteView != null)
        {
            inventoryView.SetDeleteZone(itemDeleteView);
            itemDeleteView.Show();
        }

        isInitialized = true;
        Debug.Log($"[{gameObject.name}] Inventory injected and initialized.");
    }

    #endregion

    #region Open / Close API

    public void OnOpen()
    {
        if (itemDeleteView != null)
            itemDeleteView.EnableDrops();

        Debug.Log($"[{gameObject.name}] Opened.");
    }

    public void OnClose()
    {
        inventoryPresenter?.CancelAllActions();
        Debug.Log($"[{gameObject.name}] Closed.");
    }

    #endregion

    #region Public API

    public void UpdateSlot(int slotIndex, ItemModel item)
        => inventoryView?.UpdateSlot(slotIndex, item);

    public void ClearSlot(int slotIndex)
        => inventoryView?.ClearSlot(slotIndex);

    public void RefreshAllSlots()
    {
        if (!ValidateInventoryReferences()) return;

        for (int i = 0; i < inventoryModel.maxSlots; i++)
            inventoryView.UpdateSlot(i, inventoryService.GetItemAtSlot(i));
    }

    public IInventoryView GetInventoryView() => inventoryView;

    public bool IsInitialized => isInitialized;

    #endregion

    #region Cleanup

    private void Cleanup()
    {
        if (inventoryPresenter != null)
        {
            inventoryPresenter.Cleanup();
            inventoryPresenter = null;
        }
    }

    #endregion

    #region Private Helpers

    private bool ValidateInventoryReferences()
    {
        if (inventoryModel == null || inventoryService == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Model or service not set. Call InjectInventory() first.");
            return false;
        }
        return true;
    }

    #endregion
}
