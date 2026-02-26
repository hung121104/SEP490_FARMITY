using UnityEngine;
using UnityEngine.UI;

public class CraftingSystemManager : MonoBehaviour
{
    [Header("Recipe Data")]
    [SerializeField] private RecipeDataSO[] allRecipes;

    [Header("UI Views")]
    [SerializeField] private CraftingMainView craftingMainView;
    [SerializeField] private CookingMainView cookingMainView;

    [Header("Crafting Toggle Buttons")]
    [SerializeField] private Button craftingOpenButton;
    [SerializeField] private Button craftingCloseButton;

    [Header("Cooking Toggle Buttons")]
    [SerializeField] private Button cookingOpenButton;
    [SerializeField] private Button cookingCloseButton;

    [Header("Inventory Display")]
    [SerializeField] private CraftingInventoryAdapter craftingInventoryAdapter;
    [SerializeField] private CraftingInventoryAdapter cookingInventoryAdapter;

    // Core components
    private CraftingModel craftingModel;
    private ICraftingService craftingService;
    private IInventoryService inventoryService;
    private InventoryModel inventoryModel;

    // Presenters
    private CraftingPresenter craftingPresenter;
    private CookingPresenter cookingPresenter;

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
        craftingMainView?.Hide();
        cookingMainView?.Hide();

        // Setup toggle buttons
        SetupToggleButtons();

        Debug.Log("[CraftingSystemManager] UI structure setup complete");
    }

    private void SetupToggleButtons()
    {
        // Crafting buttons
        craftingOpenButton?.onClick.AddListener(OpenCraftingUI);
        craftingCloseButton?.onClick.AddListener(CloseCraftingUI);

        // Cooking buttons
        cookingOpenButton?.onClick.AddListener(OpenCookingUI);
        cookingCloseButton?.onClick.AddListener(CloseCookingUI);
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

        // 4. Load recipes
        if (allRecipes != null && allRecipes.Length > 0)
        {
            craftingService.LoadRecipes(allRecipes);
            Debug.Log($"[CraftingSystemManager] Loaded {allRecipes.Length} recipes");
        }
        else
        {
            Debug.LogWarning("[CraftingSystemManager] No recipes assigned!");
        }

        // 5. Initialize Crafting Presenter
        InitializeCraftingPresenter();

        // 6. Initialize Cooking Presenter
        InitializeCookingPresenter();

        // 7. Connect inventory adapter last (after all systems ready)
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
        // Cleanup toggle buttons
        craftingOpenButton?.onClick.RemoveAllListeners();
        craftingCloseButton?.onClick.RemoveAllListeners();
        cookingOpenButton?.onClick.RemoveAllListeners();
        cookingCloseButton?.onClick.RemoveAllListeners();

        // Cleanup presenters
        if (craftingPresenter != null)
        {
            craftingPresenter.OnItemCrafted -= HandleItemCrafted;
            craftingPresenter.OnCraftFailed -= HandleCraftFailed;
            craftingPresenter.Cleanup();
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

    [ContextMenu("Test Open Cooking UI")]
    private void TestOpenCookingUI()
    {
        OpenCookingUI();
    }

    [ContextMenu("Test Add Test Items to Inventory")]
    private void TestAddItemsToInventory()
    {
        if (inventoryService == null || allRecipes == null || allRecipes.Length == 0)
        {
            Debug.LogWarning("Cannot add test items - service or recipes not available");
            return;
        }

        // Add ingredients from first recipe
        var firstRecipe = allRecipes[0];
        foreach (var ingredient in firstRecipe.ingredients)
        {
            inventoryService.AddItem(ingredient.item, ingredient.quantity * 10);
            Debug.Log($"Added {ingredient.item.itemName} x{ingredient.quantity * 10}");
        }
    }

    #endregion
}
