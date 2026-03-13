using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabActive;
    public GameObject[] pages;
    
    [Header("Crafting Integration")]
    [SerializeField] private CraftingSystemManager craftingSystemManager;
    [SerializeField] private int craftingTabIndex = 2; // Index tab inventory/crafting
    [SerializeField] private int inventoryTabIndex = 0; // Index tab inventory

    private int currentTabIndex = 0;

    // Start is called before the first frame update
    void Start() {         
        if (craftingSystemManager == null)
        {
            craftingSystemManager = FindFirstObjectByType<CraftingSystemManager>();
        }
        
        ActivateTab(0);
    }

    public void ActivateTab(int tabNo) {
        // Close crafting tab
        if (currentTabIndex == craftingTabIndex && craftingSystemManager != null)
        {
            craftingSystemManager.CloseCraftingInInventory();
        }

        // Move tab
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
            tabActive[i].enabled = false;
        }
        pages[tabNo].SetActive(true);
        tabActive[tabNo].enabled = true;

        if (tabNo == craftingTabIndex && inventoryTabIndex >= 0 && inventoryTabIndex < pages.Length)
        {
            pages[inventoryTabIndex].SetActive(true);
        }

        currentTabIndex = tabNo;

        // Open crafting tab.
        if (tabNo == craftingTabIndex && craftingSystemManager != null)
        {
            craftingSystemManager.OpenCraftingInInventory();
        }
    }
}
