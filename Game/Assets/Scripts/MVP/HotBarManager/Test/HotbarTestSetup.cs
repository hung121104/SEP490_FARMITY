using UnityEngine;

public class HotbarTestSetup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotbarView hotbarView;
    [SerializeField] private InventoryGameView inventoryGameView;

    [Header("Test Items (Data-Driven)")]
    [Tooltip("Item IDs from the catalog to add on start.")]
    [SerializeField] private string[] testItemIds;

    [Header("Settings")]
    [SerializeField] private bool addItemsOnStart = true;
    [SerializeField] private int hotbarStartSlot = 27;
    [SerializeField] private Quality testItemQuality = Quality.Normal;

    private HotbarPresenter hotbarPresenter;
    private IInventoryService inventoryService;

    void Start()
    {
        if (addItemsOnStart)
            Invoke(nameof(AddTestItems), 1f);
    }

    void AddTestItems()
    {
        if (hotbarView == null)
            hotbarView = FindFirstObjectByType<HotbarView>();

        if (inventoryGameView == null)
            inventoryGameView = FindFirstObjectByType<InventoryGameView>();

        if (hotbarView == null || inventoryGameView == null)
        {
            Debug.LogError("Cannot find HotbarView or InventoryGameView");
            return;
        }

        if (!hotbarView.IsInitialized())
        {
            Invoke(nameof(AddTestItems), 0.5f);
            return;
        }

        hotbarPresenter = hotbarView.GetPresenter();
        inventoryService = inventoryGameView.GetInventoryService();

        if (hotbarPresenter == null || inventoryService == null)
        {
            Debug.LogError("Systems not ready");
            return;
        }

        if (testItemIds != null && testItemIds.Length > 0)
        {
            for (int i = 0; i < testItemIds.Length && i < 9; i++)
            {
                if (!string.IsNullOrEmpty(testItemIds[i]))
                {
                    bool success = inventoryService.AddItem(testItemIds[i], 1, testItemQuality);
                    if (success)
                        Debug.Log("Added " + testItemIds[i] + " to hotbar slot " + (i + 1));
                }
            }
            Debug.Log("Test items added");
        }
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ShowStatus();

        if (Input.GetKeyDown(KeyCode.R))
            AddTestItems();

        if (Input.GetKeyDown(KeyCode.C))
            ClearHotbar();
    }

    void ShowStatus()
    {
        if (hotbarPresenter == null)
            hotbarPresenter = hotbarView?.GetPresenter();

        if (hotbarPresenter == null)
        {
            Debug.LogError("HotbarPresenter not available");
            return;
        }

        Debug.Log("=== HOTBAR STATUS ===");
        for (int i = 0; i < 9; i++)
        {
            var item = hotbarPresenter.GetItemAt(i);
            string selected = (item == hotbarPresenter.GetCurrentItem()) ? " [SELECTED]" : "";

            if (item != null)
            {
                Debug.Log("Slot " + (i + 1) + ": " + item.ItemName + " x" + item.Quantity + selected);
            }
            else
            {
                Debug.Log("Slot " + (i + 1) + ": Empty" + selected);
            }
        }
    }

    [ContextMenu("Clear Hotbar")]
    public void ClearHotbar()
    {
        if (inventoryService == null)
            inventoryService = inventoryGameView?.GetInventoryService();

        if (inventoryService != null)
        {
            for (int i = hotbarStartSlot; i < hotbarStartSlot + 9; i++)
            {
                var item = inventoryService.GetItemAtSlot(i);
                if (item != null)
                {
                    inventoryService.RemoveItemFromSlot(i, item.Quantity);
                }
            }
            Debug.Log("Hotbar cleared");
        }
    }
}
  

