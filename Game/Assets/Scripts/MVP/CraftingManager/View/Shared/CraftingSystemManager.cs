using UnityEngine;
using UnityEngine.UI;

public class CraftingSystemManager : MonoBehaviour
{
    [Header("UI Views")]
    [SerializeField] private CraftingMainView craftingInventoryView;
    [SerializeField] private CraftingMainView craftingMainView;
    [SerializeField] private CookingMainView cookingMainView;

    [Header("Inventory Display")]
    [SerializeField] private CraftingInventoryAdapter craftingInventoryAdapter;
    [SerializeField] private CraftingInventoryAdapter cookingInventoryAdapter;
    [SerializeField] private CraftingInventoryAdapter craftingInInventoryAdapter;

    // Core components
    private CraftingModel craftingModel;
    private ICraftingService craftingService;
    private IInventoryService inventoryService;
    private InventoryModel inventoryModel;

    // Presenters
    private CraftingPresenter craftingPresenter;
    private CraftingPresenter craftingInInventoryPresenter; //For crafting inventory tab
    private CookingPresenter cookingPresenter;

    private bool isCraftingInInventoryActive = true;

    private void Awake()
    {
        SetupUIStructure();
    }

    private void Start()
    {
       InitializeSystem();
    }

    private void SetupUIStructure()
    {
        // Hide UIs by default
        craftingInventoryView?.Hide();
        craftingMainView?.Hide();
        cookingMainView?.Hide();
        
        Debug.Log("[CraftingSystemManager] UI structure setup complete");
    }


    #region Initialization
    private void InitializeSystem()
    {
        Debug.Log("[CraftingSystemManager] Initializing crafting system...");

        // 1. Initialize Model
        craftingModel = new CraftingModel();

        // 2. Get or Create Inventory Service
        InitializeInventoryReferences();

        // 3. Initialize Crafting Service
        craftingService = new CraftingService(craftingModel);

        // 4. Load recipes from the catalog service (JSON)
        var catalogService = RecipeCatalogService.Instance;
        if (catalogService == null)
        {
            Debug.LogError("[CraftingSystemManager] RecipeCatalogService not found in scene! Add it as a GameObject.");
        }
        else
        {
            var recipes = catalogService.GetAllRecipes();
            if (recipes.Count > 0)
            {
                craftingService.LoadRecipes(recipes);
                Debug.Log($"[CraftingSystemManager] Loaded {recipes.Count} recipes from catalog.");
            }
            else
            {
                Debug.LogWarning("[CraftingSystemManager] Recipe catalog is empty!");
            }
        }

        // 5. Initialize Crafting Presenter
        InitializeCraftingPresenter();

        // 6. Initialize Crafting In Inventory Presenter
        InitializeCraftingInInventoryPresenter();

        // 7. Initialize Cooking Presenter
        InitializeCookingPresenter();

        // 8. Connect inventory adapter last (after all systems ready)
        ConnectInventoryAdapter();

        Debug.Log("[CraftingSystemManager] System initialized successfully");
    }

    /// <summary>
    /// Gets both InventoryService AND InventoryModel from the existing InventoryGameView.
    /// </summary>
    private void InitializeInventoryReferences()
    {
        var existingInventory = FindFirstObjectByType<InventoryGameView>();

        if (existingInventory != null)
        {
            inventoryService = existingInventory.GetInventoryService();
            inventoryModel = existingInventory.GetInventoryModel();
            Debug.Log("[CraftingSystemManager] Inventory references obtained from InventoryGameView.");
        }
        else
        {
            Debug.LogError("[CraftingSystemManager] InventoryGameView not found in scene!");
        }
    }

    /// <summary>
    /// Connects the shared inventory to the CraftingInventoryAdapter.
    /// Called last to ensure inventoryModel and inventoryService are both ready.
    /// </summary>
    private void ConnectInventoryAdapter()
    {
        if (inventoryModel == null || inventoryService == null)
        {
            Debug.LogError("[CraftingSystemManager] Cannot connect adapter, inventory model or service is null.");
            return;
        }

        if (craftingInventoryAdapter != null)
            craftingInventoryAdapter.InjectInventory(inventoryModel, inventoryService);
        else
            Debug.LogWarning("[CraftingSystemManager] CraftingInventoryAdapter not assigned.");

        if (cookingInventoryAdapter != null)
            cookingInventoryAdapter.InjectInventory(inventoryModel, inventoryService);
        else
            Debug.LogWarning("[CraftingSystemManager] CookingInventoryAdapter not assigned.");

        if (craftingInInventoryAdapter != null)
            craftingInInventoryAdapter.InjectInventory(inventoryModel, inventoryService);
        else
            Debug.LogWarning("[CraftingSystemManager] CraftingInInventoryAdapter not assigned.");
    }

    private void InitializeCraftingInInventoryPresenter()
    {
        if (craftingInventoryView == null)
        {
            Debug.LogWarning("[CraftingSystemManager] Crafting in inventory view not assigned");
            return;
        }

        craftingInInventoryPresenter = new CraftingPresenter(craftingModel, craftingService, inventoryService);
        craftingInInventoryPresenter.SetView(craftingInventoryView);

        // Subscribe to events
        craftingInInventoryPresenter.OnItemCrafted += HandleItemCraftedInInventory;
        craftingInInventoryPresenter.OnCraftFailed += HandleCraftFailedInInventory;

        Debug.Log("[CraftingSystemManager] Crafting in inventory presenter initialized");
    }


    private void InitializeCraftingPresenter()
    {
        if (craftingMainView == null)
        {
            Debug.LogWarning("[CraftingSystemManager] Crafting view not assigned");
            return;
        }

        craftingPresenter = new CraftingPresenter(craftingModel, craftingService, inventoryService);
        craftingPresenter.SetView(craftingMainView);

        // Subscribe to events
        craftingPresenter.OnItemCrafted += HandleItemCrafted;
        craftingPresenter.OnCraftFailed += HandleCraftFailed;

        Debug.Log("[CraftingSystemManager] Crafting presenter initialized");
    }

    private void InitializeCookingPresenter()
    {
        if (cookingMainView == null)
        {
            Debug.LogWarning("[CraftingSystemManager] Cooking view not assigned");
            return;
        }

        cookingPresenter = new CookingPresenter(craftingModel, craftingService, inventoryService);
        cookingPresenter.SetView(cookingMainView);

        // Subscribe to events
        cookingPresenter.OnItemCooked += HandleItemCooked;
        cookingPresenter.OnCookFailed += HandleCookFailed;

        Debug.Log("[CraftingSystemManager] Cooking presenter initialized");
    }

    #endregion

    #region Event Handlers

    private void HandleItemCrafted(RecipeModel recipe, int amount)
    {
        Debug.Log($"[CraftingSystemManager] Crafted: {recipe.RecipeName} x{amount}");
        // Add additional logic here (achievements, statistics, etc.)
    }

    private void HandleItemCooked(RecipeModel recipe, int amount)
    {
        Debug.Log($"[CraftingSystemManager] Cooked: {recipe.RecipeName} x{amount}");
        // Add additional logic here
    }

    private void HandleCraftFailed(string reason)
    {
        Debug.LogWarning($"[CraftingSystemManager] Craft failed: {reason}");
    }

    private void HandleCookFailed(string reason)
    {
        Debug.LogWarning($"[CraftingSystemManager] Cook failed: {reason}");
    }

    private void HandleItemCraftedInInventory(RecipeModel recipe, int amount)
    {
        Debug.Log($"[CraftingSystemManager] Crafted in inventory: {recipe.RecipeName} x{amount}");
        // Add additional logic here (achievements, statistics, etc.)
    }

    private void HandleCraftFailedInInventory(string reason)
    {
        Debug.LogWarning($"[CraftingSystemManager] Craft in inventory failed: {reason}");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Open crafting UI
    /// </summary>
    public void OpenCraftingUI()
    {
        craftingPresenter?.OpenCraftingUI();
        craftingInventoryAdapter?.OnOpen();
    }

    /// <summary>
    /// Close crafting UI
    /// </summary>
    public void CloseCraftingUI()
    {
        craftingPresenter?.CloseCraftingUI();
        craftingInventoryAdapter?.OnClose();
    }

    /// <summary>
    /// Open cooking UI
    /// </summary>
    public void OpenCookingUI()
    {
        cookingPresenter?.OpenCookingUI();
        cookingInventoryAdapter?.OnOpen();
    }

    /// <summary>
    /// Close cooking UI
    /// </summary>
    public void CloseCookingUI()
    {
        cookingPresenter?.CloseCookingUI();
        cookingInventoryAdapter?.OnClose();
    }

    /// <summary>
    /// Open crafting in inventory UI
    /// </summary>
    public void OpenCraftingInInventory()
    {
        craftingInInventoryPresenter?.OpenCraftingUI();
        craftingInInventoryAdapter?.OnOpen();
    }

    /// <summary>
    /// Close crafting in inventory UI
    /// </summary>
    public void CloseCraftingInInventory()
    {
        craftingInInventoryPresenter?.CloseCraftingUI();
        craftingInInventoryAdapter?.OnClose();
    }

    /// <summary>
    /// Unlock a recipe by ID
    /// </summary>
    public void UnlockRecipe(string recipeID)
    {
        craftingService?.UnlockRecipe(recipeID);
    }

    /// <summary>
    /// Get crafting service for external use
    /// </summary>
    public ICraftingService GetCraftingService()
    {
        return craftingService;
    }

    /// <summary>
    /// Get inventory service for external use
    /// </summary>
    public IInventoryService GetInventoryService()
    {
        return inventoryService;
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        // Cleanup presenters
        if (craftingPresenter != null)
        {
            craftingPresenter.OnItemCrafted -= HandleItemCrafted;
            craftingPresenter.OnCraftFailed -= HandleCraftFailed;
            craftingPresenter.Cleanup();
        }

        if (craftingInInventoryPresenter != null)
        {
            craftingInInventoryPresenter.OnItemCrafted -= HandleItemCraftedInInventory;
            craftingInInventoryPresenter.OnCraftFailed -= HandleCraftFailedInInventory;
            craftingInInventoryPresenter.Cleanup();
        }

        if (cookingPresenter != null)
        {
            cookingPresenter.OnItemCooked -= HandleItemCooked;
            cookingPresenter.OnCookFailed -= HandleCookFailed;
            cookingPresenter.Cleanup();
        }

        Debug.Log("[CraftingSystemManager] Cleaned up");
    }

    #endregion

    #region Testing Helpers

    [ContextMenu("Test Open Crafting UI")]
    private void TestOpenCraftingUI()
    {
        OpenCraftingUI();
    }

    [ContextMenu("Test Close Crafting UI")]
    private void TestCLoseCraftingUI()
    {
        CloseCraftingUI();
    }

    [ContextMenu("Test Open Cooking UI")]
    private void TestOpenCookingUI()
    {
        OpenCookingUI();
    }

    [ContextMenu("Test Close Cooking UI")]
    private void TestCloseCookingUI()
    {
        CloseCookingUI();
    }

    #endregion
}
