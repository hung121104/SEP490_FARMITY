
using UnityEngine;

public class HotbarTestSetup : MonoBehaviour
{
   
    [Header("References")]
    [SerializeField] private HotbarView hotbarView;
    [SerializeField] private InventoryGameView inventoryGameView;

    [Header("Drag test items here")]
    [SerializeField] private ItemDataSO[] testItems;

    [Header("Test Settings")]
    [SerializeField] private bool addItemsOnStart = true;
    [SerializeField] private int hotbarStartSlot = 27; // Slot bắt đầu của hotbar trong inventory
    [SerializeField] private Quality testItemQuality = Quality.Normal;

    private HotbarPresenter hotbarPresenter;
    private IInventoryService inventoryService;

    void Start()
    {
        if (addItemsOnStart)
        {
            Invoke(nameof(AddTestItems), 0.5f); // Wait for systems to initialize
        }
    }

    void AddTestItems()
    {
        // Find systems
        if (hotbarView == null)
            hotbarView = FindFirstObjectByType<HotbarView>();

        if (inventoryGameView == null)
            inventoryGameView = FindFirstObjectByType<InventoryGameView>();

        if (hotbarView == null || inventoryGameView == null)
        {
            Debug.LogError("❌ HotbarView or InventoryGameView not found! Make sure systems are set up.");
            return;
        }

        hotbarPresenter = hotbarView.GetPresenter();
        inventoryService = inventoryGameView.GetInventoryService();

        if (hotbarPresenter == null || inventoryService == null)
        {
            Debug.LogError("❌ Systems not ready!");
            return;
        }

        // Add test items to hotbar slots (inventory slots 27-35)
        if (testItems != null && testItems.Length > 0)
        {
            for (int i = 0; i < testItems.Length && i < 9; i++)
            {
                if (testItems[i] != null)
                {
                    int inventorySlot = hotbarStartSlot + i;
                    bool success = inventoryService.AddItem(testItems[i], 1, testItemQuality);

                    if (success)
                    {
                        Debug.Log($"✅ Added {testItems[i].itemName} to hotbar slot {i + 1} (inventory slot {inventorySlot})");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Failed to add {testItems[i].itemName}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No test items assigned!");
        }

        Debug.Log("✅ Test items added to hotbar!");
    }

    void Update()
    {
        // Debug hotbar status with spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowHotbarStatus();
        }

        // Refresh test items
        if (Input.GetKeyDown(KeyCode.R))
        {
            RefreshTestItems();
        }

        // Clear hotbar
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearHotbar();
        }

        // Add random item to current slot
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddRandomItemToCurrentSlot();
        }

        // Remove item from current slot
        if (Input.GetKeyDown(KeyCode.X))
        {
            RemoveCurrentItem();
        }
    }

    #region Debug Methods

    void ShowHotbarStatus()
    {
        if (hotbarPresenter == null)
        {
            Debug.LogError("❌ HotbarPresenter not available!");
            return;
        }

        Debug.Log("=== HOTBAR STATUS ===");

        for (int i = 0; i < 9; i++)
        {
            var item = hotbarPresenter.GetItemAt(i);
            string selectedIndicator = (item == hotbarPresenter.GetCurrentItem()) ? " [SELECTED]" : "";

            if (item != null)
            {
                Debug.Log($"Slot {i + 1}: {item.ItemName} x{item.quantity} (Quality: {item.Quality}){selectedIndicator}");
            }
            else
            {
                Debug.Log($"Slot {i + 1}: Empty{selectedIndicator}");
            }
        }
    }

    void AddRandomItemToCurrentSlot()
    {
        if (testItems == null || testItems.Length == 0)
        {
            Debug.LogWarning("⚠️ No test items available!");
            return;
        }

        var randomItem = testItems[Random.Range(0, testItems.Length)];
        if (randomItem != null)
        {
            bool success = inventoryService.AddItem(randomItem, 1, testItemQuality);
            Debug.Log(success
                ? $"✅ Added {randomItem.itemName} to inventory"
                : $"❌ Failed to add {randomItem.itemName}");
        }
    }

    void RemoveCurrentItem()
    {
        if (hotbarPresenter == null) return;

        var currentItem = hotbarPresenter.GetCurrentItem();
        if (currentItem != null)
        {
            hotbarPresenter.ConsumeCurrentItem(1);
            Debug.Log($"🗑️ Removed 1x {currentItem.ItemName}");
        }
        else
        {
            Debug.Log("❌ No item in current slot!");
        }
    }

    #endregion

    #region Context Menu Methods

    [ContextMenu("Add Test Items")]
    public void RefreshTestItems()
    {
        AddTestItems();
    }

    [ContextMenu("Clear Hotbar")]
    public void ClearHotbar()
    {
        if (inventoryService == null)
        {
            inventoryService = inventoryGameView?.GetInventoryService();
        }

        if (inventoryService != null)
        {
            // Clear hotbar slots (27-35)
            for (int i = hotbarStartSlot; i < hotbarStartSlot + 9; i++)
            {
                var item = inventoryService.GetItemAtSlot(i);
                if (item != null)
                {
                    inventoryService.RemoveItemFromSlot(i, item.quantity);
                }
            }
            Debug.Log("🗑️ Hotbar cleared");
        }
        else
        {
            Debug.LogError("❌ Inventory service not available!");
        }
    }

    [ContextMenu("Clear Entire Inventory")]
    public void ClearInventory()
    {
        if (inventoryService == null)
        {
            inventoryService = inventoryGameView?.GetInventoryService();
        }

        if (inventoryService != null)
        {
            inventoryService.ClearInventory();
            Debug.Log("🗑️ Entire inventory cleared");
        }
    }

    [ContextMenu("Fill Hotbar with Test Items")]
    public void FillHotbarWithTestItems()
    {
        if (testItems == null || testItems.Length == 0)
        {
            Debug.LogWarning("⚠️ No test items assigned!");
            return;
        }

        if (inventoryService == null)
        {
            inventoryService = inventoryGameView?.GetInventoryService();
        }

        if (inventoryService != null)
        {
            for (int i = 0; i < 9; i++)
            {
                var item = testItems[i % testItems.Length]; // Loop through test items
                inventoryService.AddItem(item, Random.Range(1, 5), testItemQuality);
            }
            Debug.Log("✅ Hotbar filled with test items");
        }
    }

    [ContextMenu("Test Current Item Usage")]
    public void TestCurrentItemUsage()
    {
        if (hotbarPresenter == null)
        {
            hotbarPresenter = hotbarView?.GetPresenter();
        }

        var currentItem = hotbarPresenter?.GetCurrentItem();
        if (currentItem != null)
        {
            Debug.Log($"🎮 Testing item: {currentItem.ItemName}");
            Debug.Log($"   Type: {currentItem.ItemType}");
            Debug.Log($"   Category: {currentItem.ItemCategory}");
            Debug.Log($"   Quality: {currentItem.Quality}");
            Debug.Log($"   Quantity: {currentItem.quantity}");
        }
        else
        {
            Debug.Log("❌ No item in current hotbar slot!");
        }
    }

    [ContextMenu("Show System Info")]
    public void ShowSystemInfo()
    {
        Debug.Log("=== HOTBAR & INVENTORY SYSTEM INFO ===");

        // Hotbar info
        if (hotbarView != null)
        {
            Debug.Log($"✅ HotbarView: Found");
            var presenter = hotbarView.GetPresenter();
            Debug.Log($"   Presenter: {(presenter != null ? "Ready" : "Not Ready")}");

            if (presenter != null)
            {
                var currentItem = presenter.GetCurrentItem();
                Debug.Log($"   Current Item: {(currentItem != null ? currentItem.ItemName : "None")}");
            }
        }
        else
        {
            Debug.LogError("❌ HotbarView not found!");
        }

        // Inventory info
        if (inventoryGameView != null)
        {
            Debug.Log($"✅ InventoryGameView: Found");
            var service = inventoryGameView.GetInventoryService();
            Debug.Log($"   Service: {(service != null ? "Ready" : "Not Ready")}");

            if (service != null)
            {
                int emptySlots = service.GetEmptySlotCount();
                var allItems = service.GetAllItems();
                Debug.Log($"   Total Items: {allItems.Count}");
                Debug.Log($"   Empty Slots: {emptySlots}");
            }
        }
        else
        {
            Debug.LogError("❌ InventoryGameView not found!");
        }

        // Integration info
        Debug.Log($"=== INTEGRATION ===");
        Debug.Log($"Hotbar Start Slot: {hotbarStartSlot}");
        Debug.Log($"Hotbar End Slot: {hotbarStartSlot + 8}");
        Debug.Log($"Test Item Quality: {testItemQuality}");
    }

    [ContextMenu("Show Inventory Slots")]
    public void ShowInventorySlots()
    {
        if (inventoryService == null)
        {
            inventoryService = inventoryGameView?.GetInventoryService();
        }

        if (inventoryService != null)
        {
            Debug.Log("=== FULL INVENTORY STATUS ===");

            for (int i = 0; i < 36; i++)
            {
                var item = inventoryService.GetItemAtSlot(i);
                bool isHotbarSlot = i >= hotbarStartSlot && i < hotbarStartSlot + 9;
                string hotbarIndicator = isHotbarSlot ? $" [HOTBAR {i - hotbarStartSlot + 1}]" : "";

                if (item != null)
                {
                    Debug.Log($"Slot {i}: {item.ItemName} x{item.quantity}{hotbarIndicator}");
                }
                else if (isHotbarSlot)
                {
                    Debug.Log($"Slot {i}: Empty{hotbarIndicator}");
                }
            }
        }
    }

    [ContextMenu("Add Stack of Current Item")]
    public void AddStackOfCurrentItem()
    {
        if (hotbarPresenter == null)
        {
            hotbarPresenter = hotbarView?.GetPresenter();
        }

        var currentItem = hotbarPresenter?.GetCurrentItem();
        if (currentItem != null && inventoryService != null)
        {
            inventoryService.AddItem(currentItem.itemData, 10, currentItem.Quality);
            Debug.Log($"✅ Added 10x {currentItem.ItemName}");
        }
        else
        {
            Debug.Log("❌ No item selected or service not available!");
        }
    }

    #endregion

    #region Debug Help

    [ContextMenu("Show Debug Controls")]
    public void ShowDebugControls()
    {
        Debug.Log("=== HOTBAR TEST DEBUG CONTROLS ===");
        Debug.Log("SPACEBAR - Show hotbar status");
        Debug.Log("R - Refresh test items");
        Debug.Log("C - Clear hotbar");
        Debug.Log("T - Add random test item to inventory");
        Debug.Log("X - Remove item from current hotbar slot");
        Debug.Log("");
        Debug.Log("NUMBER KEYS (1-9) - Select hotbar slot");
        Debug.Log("SCROLL WHEEL - Navigate hotbar slots");
        Debug.Log("LEFT CLICK - Use current item");
    }

    #endregion
}
  

