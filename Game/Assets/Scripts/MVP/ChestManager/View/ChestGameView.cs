using UnityEngine;

/// <summary>
/// Orchestrator MonoBehaviour for the chest system.
/// Creates chest MVP stack, borrows player InventoryGameView,
/// manages dual-panel lifecycle, and subscribes to ChestSyncManager.
///
/// UI Layout:
///   ChestMainView
///   └── MainPanel
///       └── ChestView
///           ├── ChestSide → ChestInventoryView
///           └── Inventory → InventoryView (reparented here when open)
/// </summary>
public class ChestGameView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChestInventoryView chestInventoryView;
    [SerializeField] private ItemDetailView itemDetailView;
    [SerializeField] private GameObject chestMainPanel;
    [SerializeField] private Transform inventoryContainer;

    [Header("Testing")]
    [SerializeField] private int testTileX = 0;
    [SerializeField] private int testTileY = 0;
    [SerializeField] private int testStructureLevel = 1;

    // Runtime state
    private ChestData activeChestData;
    private InventoryModel chestModel;
    private InventoryService chestService;
    private ChestPresenter presenter;

    // Borrowed from player
    private InventoryGameView playerInventoryGameView;

    private void Awake()
    {
        playerInventoryGameView = FindFirstObjectByType<InventoryGameView>();

        if (chestMainPanel != null)
            chestMainPanel.SetActive(false);
    }

    private void OnEnable()
    {
        ChestSyncManager.OnChestChanged += HandleRemoteChestChanged;
    }

    private void OnDisable()
    {
        ChestSyncManager.OnChestChanged -= HandleRemoteChestChanged;
    }

    #region Public API

    /// <summary>
    /// Open the chest dual-panel UI.
    /// Called by ChestStructure.OpenUI().
    /// </summary>
    public void OpenChest(ChestData chestData)
    {
        if (chestData == null) return;

        // Already open for this chest
        if (activeChestData != null && activeChestData.ChestId == chestData.ChestId && IsChestOpen())
            return;

        CloseChest(); // cleanup previous if any

        activeChestData = chestData;

        // Build chest MVP stack
        chestModel = new InventoryModel(chestData.SlotCount);
        chestService = new InventoryService(chestModel);

        var transferService = new ChestService();

        presenter = new ChestPresenter(
            chestData,
            chestModel,
            chestService,
            playerInventoryGameView.GetInventoryModel(),
            playerInventoryGameView.GetInventoryService(),
            transferService);

        // Initialize chest view
        chestInventoryView.InitializeSlots(chestData.SlotCount);
        presenter.SetChestView(chestInventoryView);

        // Wire player inventory view so ChestPresenter can handle inventory → chest drag
        var playerInventoryView = playerInventoryGameView?.GetInventoryView();
        if (playerInventoryView != null)
            presenter.SetPlayerView(playerInventoryView);

        // Wire item detail view
        if (itemDetailView != null)
            presenter.SetItemDetailView(itemDetailView);

        // Load current state from ChestDataModule (Master's authoritative data)
        LoadChestStateFromModule();

        // Open player inventory — reparent into chest container if assigned (like crafting)
        if (playerInventoryGameView != null)
            playerInventoryGameView.OpenChestInventory(inventoryContainer);

        // Show the chest panel
        if (chestMainPanel != null)
            chestMainPanel.SetActive(true);

        // Register chest panel as safe zone so inventory doesn't drop-to-world on release
        if (playerInventoryGameView != null && chestMainPanel != null)
            playerInventoryGameView.SetAdditionalSafeZone(chestMainPanel.GetComponent<RectTransform>());

        // Notify network
        if (ChestSyncManager.Instance != null)
            ChestSyncManager.Instance.NotifyChestOpened(chestData.ChestId);

        Debug.Log($"[ChestGameView] Opened chest '{chestData.ChestId}' ({chestData.SlotCount} slots)");
    }

    /// <summary>
    /// Close the chest UI and cleanup.
    /// </summary>
    public void CloseChest()
    {
        if (activeChestData == null) return;

        // Notify network
        if (ChestSyncManager.Instance != null)
            ChestSyncManager.Instance.NotifyChestClosed(activeChestData.ChestId);

        // Cleanup presenter
        presenter?.Cleanup();
        presenter = null;

        // Hide chest panel
        if (chestMainPanel != null)
            chestMainPanel.SetActive(false);

        // Close player inventory
        if (playerInventoryGameView != null)
            playerInventoryGameView.CloseInventory();

        // Unregister safe zone
        playerInventoryGameView?.SetAdditionalSafeZone(null);

        Debug.Log($"[ChestGameView] Closed chest '{activeChestData.ChestId}'");

        activeChestData = null;
        chestModel = null;
        chestService = null;
    }

    public bool IsChestOpen() => activeChestData != null && chestMainPanel != null && chestMainPanel.activeSelf;

    public string ActiveChestId => activeChestData?.ChestId;

    #endregion

    #region Testing Helpers

    [ContextMenu("Test Open Chest UI")]
    private void TestOpenChestUI()
    {
        if (IsChestOpen())
            CloseChest();
        else
            OpenChest(new ChestData(testTileX, testTileY, testStructureLevel));
    }

    [ContextMenu("Test Close Chest UI")]
    private void TestCloseChestUI()
    {
        CloseChest();
    }

    #endregion

    #region Remote Sync

    private void HandleRemoteChestChanged(string chestId)
    {
        if (activeChestData == null || activeChestData.ChestId != chestId) return;
        if (presenter != null && !presenter.IsReadyToSync()) return;
        LoadChestStateFromModule();
    }

    private void LoadChestStateFromModule()
    {
        presenter?.LoadStateFromModule();
    }

    #endregion
}
