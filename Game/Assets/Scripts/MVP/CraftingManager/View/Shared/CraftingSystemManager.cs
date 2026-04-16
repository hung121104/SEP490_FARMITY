using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CraftingSystemManager : MonoBehaviour
{
    [SerializeField] private InventoryGameView inventoryGameView;

    [Header("UI Crafting Views")]
    [SerializeField] private CraftingMainView craftingInventoryView;
    [SerializeField] private CraftingMainView craftingMainView;
    [SerializeField] private CookingMainView cookingMainView;

    [Header("Inventory Display")]
    [SerializeField] private Transform craftingMainPanel;
    [SerializeField] private Transform cookingMainPanel;

    // Core components
    private CraftingModel craftingModel;
    private ICraftingService craftingService;
    private IInventoryService inventoryService;
    private int inventorySlotCount;

    // Presenters
    private CraftingPresenter craftingPresenter;
    private CraftingPresenter craftingInInventoryPresenter; //For crafting inventory tab
    private CookingPresenter cookingPresenter;

    // Track which station is currently open (for close broadcast)
    private Vector2Int? activeStationPosition;
    private ChunkDataSyncManager _syncManager;
    private ChunkDataSyncManager SyncManager => _syncManager ??= FindAnyObjectByType<ChunkDataSyncManager>();

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
        craftingService = new CraftingService(craftingModel, inventoryService);

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

        // 8. Connect inventory to sub-UIs (after all systems ready)
        InitializeInventoryForSubUIs();

        // 9. Subscribe to catalog events for real-time sync
        RecipeCatalogService.OnRecipeRemoved  += HandleOrphanedRecipeRemoved;
        RecipeCatalogService.OnCatalogReloaded += HandleRecipeCatalogReloaded;
        CatalogSyncManager.OnCatalogChanged   += HandleCatalogChanged;

        Debug.Log("[CraftingSystemManager] System initialized successfully");
    }

    private void HandleOrphanedRecipeRemoved(string recipeId)
    {
        craftingService?.RemoveRecipe(recipeId);
    }

    /// <summary>Full catalog reload — rebuild CraftingModel from RecipeCatalogService.</summary>
    private void HandleRecipeCatalogReloaded()
    {
        if (RecipeCatalogService.Instance == null) return;
        var recipes = RecipeCatalogService.Instance.GetAllRecipes();
        craftingService?.LoadRecipes(recipes);
        Debug.Log($"[CraftingSystemManager] Catalog reloaded — {recipes.Count} recipe(s) synced.");
    }

    /// <summary>Real-time single recipe add/update from SSE.</summary>
    private void HandleCatalogChanged(string changeType, string entityType, string entityName, string typeName)
    {
        if (RecipeCatalogService.Instance == null) return;

        if (entityType == "recipe")
        {
            // Recipe added, updated, or deleted — reload all recipes into CraftingModel.
            var recipes = RecipeCatalogService.Instance.GetAllRecipes();
            craftingService?.LoadRecipes(recipes);
        }
        else if (changeType == "delete" && entityType == "item")
        {
            // Item deleted → recipes using that item were cascade-removed by
            // CatalogDeleteHandler.PostItemDelete(). Reload to reflect changes.
            var recipes = RecipeCatalogService.Instance.GetAllRecipes();
            craftingService?.LoadRecipes(recipes);
        }
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
            inventorySlotCount = inventoryService.MaxSlots;

            // Cache the InventoryGameView reference if not assigned in Inspector
            if (inventoryGameView == null)
                inventoryGameView = existingInventory;

            Debug.Log("[CraftingSystemManager] Inventory references obtained from InventoryGameView.");
        }
        else
        {
            Debug.LogError("[CraftingSystemManager] InventoryGameView not found in scene!");
        }
    }

    /// <summary>
    /// Ensures the shared InventoryView's slots are initialized.
    /// No secondary views or adapters needed — the single InventoryView is reused.
    /// </summary>
    private void InitializeInventoryForSubUIs()
    {
        if (inventoryGameView == null)
        {
            Debug.LogError("[CraftingSystemManager] inventoryGameView not assigned — inventory will not show for sub-UIs.");
            return;
        }
        Debug.Log("[CraftingSystemManager] Inventory ready for sub-UI usage.");
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
    /// Open crafting UI at a specific station position.
    /// </summary>
    public void OpenCraftingUI(int stationLevel = 0, int worldX = int.MinValue, int worldY = int.MinValue)
    {
        craftingPresenter?.OpenCraftingUI(stationLevel);
        inventoryGameView?.OpenCraftingInventory(craftingMainPanel);

        if (worldX != int.MinValue)
        {
            activeStationPosition = new Vector2Int(worldX, worldY);
            SyncManager?.NotifyStationOpened(worldX, worldY);
        }
    }

    /// <summary>
    /// Close crafting UI
    /// </summary>
    public void CloseCraftingUI()
    {
        NotifyStationClosedIfActive();
        craftingPresenter?.CloseCraftingUI();
        inventoryGameView?.CloseInventory();
    }

    /// <summary>
    /// Open cooking UI at a specific station position.
    /// </summary>
    public void OpenCookingUI(int stationLevel = 0, int worldX = int.MinValue, int worldY = int.MinValue)
    {
        cookingPresenter?.OpenCookingUI(stationLevel);
        inventoryGameView?.OpenCookingInventory(cookingMainPanel);

        if (worldX != int.MinValue)
        {
            activeStationPosition = new Vector2Int(worldX, worldY);
            SyncManager?.NotifyStationOpened(worldX, worldY);
        }
    }

    /// <summary>
    /// Close cooking UI
    /// </summary>
    public void CloseCookingUI()
    {
        NotifyStationClosedIfActive();
        cookingPresenter?.CloseCookingUI();
        inventoryGameView?.CloseInventory();
    }

    /// <summary>
    /// Open crafting in inventory UI
    /// </summary>
    public void OpenCraftingInInventory()
    {
        craftingInInventoryPresenter?.OpenCraftingUI();
        inventoryGameView?.OpenCraftingInInventory();
    }

    /// <summary>
    /// Close crafting in inventory UI
    /// </summary>
    public void CloseCraftingInInventory()
    {
        craftingInInventoryPresenter?.CloseCraftingUI();
    }

    /// <summary>
    /// Unlock a recipe by ID
    /// </summary>
    public void UnlockRecipe(string recipeID)
    {
        craftingService?.UnlockRecipe(recipeID);
    }

    /// <summary>Returns true if the standalone Crafting UI is currently open.</summary>
    public bool IsCraftingUIOpen() => craftingPresenter != null && craftingPresenter.IsUIOpen();

    /// <summary>Returns true if the Cooking UI is currently open.</summary>
    public bool IsCookingUIOpen() => cookingPresenter != null && cookingPresenter.IsUIOpen();

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
        RecipeCatalogService.OnRecipeRemoved  -= HandleOrphanedRecipeRemoved;
        RecipeCatalogService.OnCatalogReloaded -= HandleRecipeCatalogReloaded;
        CatalogSyncManager.OnCatalogChanged   -= HandleCatalogChanged;

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
        if (craftingPresenter != null && craftingPresenter.IsUIOpen())
            CloseCraftingUI();
        else
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
        if (cookingPresenter != null && cookingPresenter.IsUIOpen())
            CloseCookingUI();
        else
            OpenCookingUI();
    }

    [ContextMenu("Test Close Cooking UI")]
    private void TestCloseCookingUI()
    {
        CloseCookingUI();
    }

    #endregion

    #region Station Badge Helper

    private void NotifyStationClosedIfActive()
    {
        if (!activeStationPosition.HasValue) return;
        SyncManager?.NotifyStationClosed(activeStationPosition.Value.x, activeStationPosition.Value.y);
        activeStationPosition = null;
    }

    #endregion
}
