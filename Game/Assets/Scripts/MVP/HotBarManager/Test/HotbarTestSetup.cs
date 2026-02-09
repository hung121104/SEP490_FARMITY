
using UnityEngine;

public class HotbarTestSetup : MonoBehaviour
{
    [Header("Drag test items here")]
    [SerializeField] private ItemDataSO[] additionalTestItems; // For more test items

    [Header("Test Settings")]
    [SerializeField] private bool addItemsOnStart = true;

    private HotbarBootstrap hotbarBootstrap;

    void Start()
    {
        if (addItemsOnStart)
        {
            Invoke(nameof(AddTestItems), 0.5f); // Wait for MVP system to initialize
        }
    }

    void AddTestItems()
    {
        // Find the new MVP system instead of HotbarManager
        hotbarBootstrap = FindObjectOfType<HotbarBootstrap>();

        if (hotbarBootstrap == null)
        {
            Debug.LogError("❌ HotbarBootstrap not found! Make sure MVP system is set up.");
            return;
        }

        var presenter = hotbarBootstrap.GetPresenter();
        if (presenter == null)
        {
            Debug.LogError("❌ HotbarPresenter not ready!");
            return;
        }

        // Add additional test items if available
        if (additionalTestItems != null)
        {
            for (int i = 0; i < additionalTestItems.Length && i < 8; i++) // Max 8 more items (slots 2-9)
            {
                if (additionalTestItems[i] != null)
                {
                    bool success = presenter.AddItem(i, additionalTestItems[i], 1);
                    Debug.Log($"✅ Added {additionalTestItems[i].itemName} to slot {i + 1}: {(success ? "Success" : "Failed")}");
                }
            }
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

        // Additional debug keys
        if (Input.GetKeyDown(KeyCode.R))
        {
            RefreshTestItems();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearHotbar();
        }
    }

    void ShowHotbarStatus()
    {
        if (hotbarBootstrap == null)
            hotbarBootstrap = FindObjectOfType<HotbarBootstrap>();

        if (hotbarBootstrap == null)
        {
            Debug.LogError("❌ HotbarBootstrap not found for status check!");
            return;
        }

        var model = hotbarBootstrap.GetModel();
        if (model == null)
        {
            Debug.LogError("❌ HotbarModel not available!");
            return;
        }

        Debug.Log("=== HOTBAR STATUS (MVP System) ===");
        Debug.Log($"Current Selected Slot: {model.CurrentSlotIndex + 1}");

        for (int i = 0; i < model.HotbarSize; i++)
        {
            HotbarSlot slot = model.GetSlot(i);
            if (slot != null && !slot.IsEmpty)
            {
                string selectedIndicator = (i == model.CurrentSlotIndex) ? " [SELECTED]" : "";
                Debug.Log($"Slot {i + 1}: {slot.item.itemName} x{slot.quantity}{selectedIndicator}");
            }
            else
            {
                string selectedIndicator = (i == model.CurrentSlotIndex) ? " [SELECTED]" : "";
                Debug.Log($"Slot {i + 1}: Empty{selectedIndicator}");
            }
        }
    }

    [ContextMenu("Add Test Items")]
    public void RefreshTestItems()
    {
        AddTestItems();
    }

    [ContextMenu("Clear Hotbar")]
    public void ClearHotbar()
    {
        if (hotbarBootstrap == null)
            hotbarBootstrap = FindObjectOfType<HotbarBootstrap>();

        if (hotbarBootstrap != null)
        {
            var presenter = hotbarBootstrap.GetPresenter();
            presenter?.ClearHotbar();
            Debug.Log("🗑️ Hotbar cleared via test setup");
        }
    }

    [ContextMenu("Test Current Item Usage")]
    public void TestCurrentItemUsage()
    {
        if (hotbarBootstrap == null)
            hotbarBootstrap = FindObjectOfType<HotbarBootstrap>();

        if (hotbarBootstrap != null)
        {
            var model = hotbarBootstrap.GetModel();
            model?.UseCurrentItem();
            Debug.Log("🎮 Tested current item usage");
        }
    }

    [ContextMenu("Show System Info")]
    public void ShowSystemInfo()
    {
        if (hotbarBootstrap == null)
            hotbarBootstrap = FindObjectOfType<HotbarBootstrap>();

        if (hotbarBootstrap != null)
        {
            Debug.Log("=== MVP SYSTEM INFO ===");
            Debug.Log($"✅ HotbarBootstrap: Found");
            Debug.Log($"✅ HotbarModel: {(hotbarBootstrap.GetModel() != null ? "Ready" : "Not Ready")}");
            Debug.Log($"✅ HotbarPresenter: {(hotbarBootstrap.GetPresenter() != null ? "Ready" : "Not Ready")}");
            Debug.Log($"✅ HotbarView: {(hotbarBootstrap.GetView() != null ? "Ready" : "Not Ready")}");
        }
        else
        {
            Debug.LogError("❌ MVP System not found!");
        }
    }
  
}

