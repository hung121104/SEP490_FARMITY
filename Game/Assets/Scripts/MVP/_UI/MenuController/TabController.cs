using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    public Image[] tabActive;
    public GameObject[] pages;
    
    [Header("Crafting Integration")]
    [SerializeField] private CraftingSystemManager craftingSystemManager;
    [SerializeField] private int craftingTabIndex = 2; // Index của tab inventory/crafting, -1 = không sử dụng

    private int currentTabIndex = 0;

    // Start is called before the first frame update
    void Start() {
        // Auto-assign craftingSystemManager nếu chưa được gán trong Inspector
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

        currentTabIndex = tabNo;

        // Open crafting tab.
        if (tabNo == craftingTabIndex && craftingSystemManager != null)
        {
            craftingSystemManager.OpenCraftingInInventory();
        }
    }
}
