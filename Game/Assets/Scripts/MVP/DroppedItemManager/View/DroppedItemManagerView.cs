using UnityEngine;
using UnityEngine.Pool;
using Photon.Pun;
using System; 
using System.Linq; 
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// View layer for the Dropped Item system.
/// (Replaces the old DroppedItemManager monolith — same public API, proper MVP separation.)
/// </summary>
public class DroppedItemManagerView : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static DroppedItemManagerView Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Prefab")]
    [Tooltip("The DroppedItem prefab with DroppedItemView.")]
    [SerializeField] private GameObject droppedItemPrefab;

    [Header("References (auto-resolved if null)")]
    [SerializeField] private DroppedItemSyncManager syncManager;
    [SerializeField] private ChunkLoadingManager chunkLoadingManager;

    [Header("Inventory Drop Zones")]
    [SerializeField] private InventoryDropZone[] inventoryDropZones = new InventoryDropZone[0];

    [Header("Settings")]
    [Tooltip("Offset applied to player position when dropping an item.")]
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, 0f);

    [Tooltip("Enable debug logging.")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Object Pool Settings")]
    [Tooltip("Pre-warmed capacity of the dropped item pool.")]
    [SerializeField] private int poolDefaultCapacity = 20;

    [Tooltip("Maximum number of objects the pool will hold before destroying excess.")]
    [SerializeField] private int poolMaxSize = 100;

    // ── MVP Components ────────────────────────────────────────────────────────

    private DroppedItemPresenter presenter;
    private IDroppedItemService service;

    // ── Runtime State ─────────────────────────────────────────────────────────

    /// <summary>Active visual GameObjects keyed by dropId.</summary>
    private readonly Dictionary<string, GameObject> _activeVisuals = new();

    /// <summary>Object pool for DroppedItem prefabs — avoids Instantiate/Destroy GC pressure.</summary>
    private ObjectPool<GameObject> _itemPool;

    /// <summary>Cached reference to local player transform.</summary>
    private Transform _localPlayerTransform;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired after a dropped item visual is spawned in the world.</summary>
    public event Action<DroppedItemData> OnItemVisualSpawned;

    /// <summary>Fired after a dropped item visual is removed from the world.</summary>
    public event Action<string> OnItemVisualDespawned;

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Access all inventory drop zones for external checks (e.g., InventoryPresenter).</summary>
    public InventoryDropZone[] DropZones => inventoryDropZones;

    /// <summary>Returns the first assigned drop zone (backward compatibility).</summary>
    public InventoryDropZone DropZone => inventoryDropZones != null && inventoryDropZones.Length > 0 ? inventoryDropZones[0] : null;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DroppedItemManagerView] Duplicate instance destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ResolveReferences();
        InitializeMVP();
        StartCoroutine(FindLocalPlayer());
        StartCoroutine(LateResolveReferences());
    }

    private void OnDestroy()
    {
        // Unsubscribe View from Presenter events
        if (presenter != null)
        {
            presenter.OnSpawnVisualRequested -= HandleSpawnVisualRequested;
            presenter.OnDespawnVisualRequested -= HandleDespawnVisualRequested;
            presenter.OnAddToInventoryRequested -= HandleAddToInventoryRequested;
            presenter.OnClearAllVisualsRequested -= HandleClearAllVisualsRequested;
            presenter.OnPartialPickupToInventoryRequested -= HandlePartialPickupToInventoryRequested;
            presenter.OnVisualQuantityUpdateRequested -= HandleVisualQuantityUpdateRequested;
        }

        // Cleanup presenter (unsubscribes from sync + chunk events)
        presenter?.Cleanup();

        // Cleanup visuals and dispose pool
        ClearAllVisuals();
        _itemPool?.Dispose();
        _itemPool = null;

        if (Instance == this) Instance = null;
    }

    // ── Initialization ────────────────────────────────────────────────────────

    /// <summary>Auto-resolve Inspector references if not assigned.</summary>
    private void ResolveReferences()
    {
        if (syncManager == null)
            syncManager = FindAnyObjectByType<DroppedItemSyncManager>();
        if (chunkLoadingManager == null)
            chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
    }

    /// <summary>
    /// Retry finding ChunkLoadingManager if it was not available at Start().
    /// This ensures chunk-based load/unload works even if ChunkLoadingManager
    /// initializes after DroppedItemManagerView.
    /// </summary>
    private IEnumerator LateResolveReferences()
    {
        // Give other scripts time to initialize
        float maxWaitTime = 10f;
        float waited = 0f;

        while (chunkLoadingManager == null && waited < maxWaitTime)
        {
            yield return new WaitForSeconds(0.5f);
            waited += 0.5f;

            chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();

            if (chunkLoadingManager != null)
            {
                if (showDebugLogs)
                    Debug.Log($"[DroppedItemManagerView] ChunkLoadingManager found via late-init after {waited:F1}s");

                // Update presenter with the newly found reference
                presenter?.UpdateChunkLoadingManager(chunkLoadingManager);
                yield break;
            }
        }

        if (chunkLoadingManager == null)
        {
            Debug.LogWarning("[DroppedItemManagerView] ChunkLoadingManager NOT found after late-init — " +
                           "dropped items will NOT unload with chunks. Ensure ChunkLoadingManager is in the scene.");
        }
    }

    /// <summary>
    /// Build the MVP stack: Service → Presenter, then wire events.
    /// Follows the same initialization pattern as CropPlantingView / CropManagerView.
    /// </summary>
    private void InitializeMVP()
    {
        // Create Service (merged registry + business logic)
        service = new DroppedItemService(showDebugLogs);

        // Create Presenter
        presenter = new DroppedItemPresenter(
            service, syncManager, chunkLoadingManager, showDebugLogs);

        // Subscribe Presenter to external events
        presenter.SubscribeToSyncEvents();
        presenter.SubscribeToChunkEvents();

        // Subscribe View to Presenter events
        presenter.OnSpawnVisualRequested += HandleSpawnVisualRequested;
        presenter.OnDespawnVisualRequested += HandleDespawnVisualRequested;
        presenter.OnAddToInventoryRequested += HandleAddToInventoryRequested;
        presenter.OnClearAllVisualsRequested += HandleClearAllVisualsRequested;
        presenter.OnPartialPickupToInventoryRequested += HandlePartialPickupToInventoryRequested;
        presenter.OnVisualQuantityUpdateRequested += HandleVisualQuantityUpdateRequested;

        // Initialize the object pool
        InitializePool();

        if (showDebugLogs)
            Debug.Log("[DroppedItemManagerView] MVP initialized.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by InventoryGameView.HandleItemDropped() to request dropping an item.
    /// Delegates to Presenter which creates data via Service and sends through Photon.
    /// </summary>
    /// <param name="item">The ItemModel being dropped from inventory.</param>
    public void RequestDropItem(ItemModel item)
    {
        if (item == null)
        {
            Debug.LogWarning("[DroppedItemManagerView] Cannot drop null item.");
            return;
        }

        if (_localPlayerTransform == null)
        {
            Debug.LogWarning("[DroppedItemManagerView] Local player not found, cannot drop item. " +
                           "Ensure a PlayerEntity with PhotonView exists in the scene.");
            return;
        }

        if (presenter == null)
        {
            Debug.LogError("[DroppedItemManagerView] Presenter is null! MVP not initialized properly.");
            return;
        }

        presenter.RequestDropItem(item, _localPlayerTransform.position, dropOffset);
    }

    /// <summary>
    /// Called by DroppedItemView when the local player presses the pickup key.
    /// Delegates to Presenter which sends pickup request through Photon.
    /// </summary>
    /// <param name="dropId">The unique ID of the item to pick up.</param>
    public void RequestPickupItem(string dropId)
    {
        if (presenter == null) return;
        
        var allItems = presenter.GetAllDroppedItems();
        DroppedItemData data = null;
        foreach (var item in allItems) { if (item.dropId == dropId) { data = item; break; } }
        
        if (data == null) return;

        var inventoryGameView = FindAnyObjectByType<InventoryGameView>();
        if (inventoryGameView != null)
        {
            var itemData = ItemCatalogService.Instance?.GetItemData(data.itemId);
            if (itemData != null)
            {
                var invService = inventoryGameView.GetInventoryService();
                int addable = invService.GetAddableQuantity(itemData, data.quantity, data.quality);

                if (addable <= 0)
                {
                    if (showDebugLogs) Debug.Log($"[DroppedItemManagerView] Inventory full! Cannot pick up {data.itemName}");
                    return; // inventory is fully packed, do not pickup at all
                }
                else if (addable < data.quantity)
                {
                    if (showDebugLogs) Debug.Log($"[DroppedItemManagerView] Partial pickup: can fit {addable} out of {data.quantity}");
                    syncManager?.SendPartialPickupRequest(dropId, addable);
                    return;
                }
            }
        }

        presenter?.RequestPickupItem(dropId);
    }

    /// <summary>Check if a dropped item exists in the registry.</summary>
    public bool HasDroppedItem(string dropId) => presenter?.HasDroppedItem(dropId) ?? false;

    /// <summary>Get all dropped items currently in the registry.</summary>
    public IReadOnlyCollection<DroppedItemData> GetAllDroppedItems()
    {
        return presenter?.GetAllDroppedItems()
            ?? (IReadOnlyCollection<DroppedItemData>)new List<DroppedItemData>().AsReadOnly();
    }

    /// <summary>
    /// Rebuild the in-memory registry and visuals from a database snapshot.
    /// Called by DroppedItemSyncManager when this client becomes the new Master.
    /// </summary>
    public void RebuildFromDatabase(DroppedItemData[] items)
    {
        presenter?.RebuildFromDatabase(items);
    }

    /// <summary>
    /// Check if a screen position is inside ANY of the inventory drop zones.
    /// Can be used by InventoryPresenter to determine drop-to-world behavior.
    /// </summary>
    /// <param name="screenPosition">Screen-space position (e.g., Input.mousePosition).</param>
    /// <returns>True if inside at least one inventory zone.</returns>
    public bool IsInsideInventoryZone(Vector2 screenPosition)
    {
        if (inventoryDropZones == null) return false;
        foreach (var zone in inventoryDropZones)
        {
            if (zone != null && zone.IsScreenPositionInsideZone(screenPosition))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Check if an item should be dropped to the world based on screen position.
    /// Returns true only when the position is outside ALL registered inventory zones.
    /// </summary>
    /// <param name="screenPosition">Screen-space position (e.g., Input.mousePosition).</param>
    /// <returns>True if the item should be dropped to the world.</returns>
    public bool ShouldDropItemToWorld(Vector2 screenPosition)
    {
        // If no zones are configured, fall back to allowing drop (caller decides via inventoryPanel check)
        if (inventoryDropZones == null || inventoryDropZones.Length == 0) return true;
        foreach (var zone in inventoryDropZones)
        {
            // If inside ANY zone → do NOT drop to world
            if (zone != null && zone.IsScreenPositionInsideZone(screenPosition))
                return false;
        }
        return true; // outside all zones → drop to world
    }

    // ── Presenter Event Handlers ──────────────────────────────────────────────

    private void HandleSpawnVisualRequested(DroppedItemData data)
    {
        SpawnItemVisual(data);
    }

    private void HandleDespawnVisualRequested(string dropId)
    {
        DespawnItemVisual(dropId);
    }

    private void HandleAddToInventoryRequested(DroppedItemData data)
    {
        AddItemToInventory(data, data.quantity);
    }

    private void HandlePartialPickupToInventoryRequested(DroppedItemData data, int amount)
    {
        AddItemToInventory(data, amount);
    }

    private void HandleVisualQuantityUpdateRequested(DroppedItemData data)
    {
        // Find existing visual and update its quantity visual rendering
        if (_activeVisuals.TryGetValue(data.dropId, out GameObject go))
        {
            var view = go.GetComponent<DroppedItemView>();
            if (view != null)
            {
                // Re-initialize or reshow with updated data so it refreshes the sprites
                view.ShowItem(data);
            }
        }
    }

    private void HandleClearAllVisualsRequested()
    {
        ClearAllVisuals();
    }

    // ── Object Pool ───────────────────────────────────────────────────────────

    /// <summary>
    /// Initialize the ObjectPool for DroppedItem prefabs.
    /// Called once during InitializeMVP().
    /// </summary>
    private void InitializePool()
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogError("[DroppedItemManagerView] droppedItemPrefab is not assigned! ObjectPool cannot be created.");
            return;
        }

        _itemPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = Instantiate(droppedItemPrefab);
                go.SetActive(false);
                return go;
            },
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go =>
            {
                go.GetComponent<DroppedItemView>()?.Cleanup();
                go.SetActive(false);
            },
            actionOnDestroy: go => Destroy(go),
            collectionCheck: false,
            defaultCapacity: poolDefaultCapacity,
            maxSize: poolMaxSize
        );

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManagerView] ObjectPool initialized (capacity={poolDefaultCapacity}, maxSize={poolMaxSize}).");
    }

    // ── Visual Spawn / Despawn ────────────────────────────────────────────────

    /// <summary>
    /// Get a DroppedItem from the pool, position it, and initialize its View.
    /// </summary>
    private void SpawnItemVisual(DroppedItemData data)
    {
        if (_itemPool == null)
        {
            Debug.LogError("[DroppedItemManagerView] ObjectPool is not initialized! Cannot spawn item.");
            return;
        }

        if (_activeVisuals.ContainsKey(data.dropId))
            return; // Already spawned

        GameObject go = _itemPool.Get();
        go.transform.position = new Vector3(data.worldX, data.worldY, 0f);
        go.name = $"DroppedItem_{data.itemName}_{data.dropId[..Mathf.Min(8, data.dropId.Length)]}";

        // Initialize DroppedItemView on the prefab
        var view = go.GetComponent<DroppedItemView>();
        if (view == null)
        {
            Debug.LogError("[DroppedItemManagerView] Prefab missing DroppedItemView! Cannot spawn item.");
            _itemPool.Release(go);
            return;
        }

        view.Initialize(data);

        _activeVisuals[data.dropId] = go;
        OnItemVisualSpawned?.Invoke(data);
    }

    /// <summary>
    /// Release the DroppedItem visual back to the pool (instead of destroying).
    /// </summary>
    private void DespawnItemVisual(string dropId)
    {
        if (_activeVisuals.TryGetValue(dropId, out GameObject go))
        {
            _activeVisuals.Remove(dropId);

            if (go != null)
                _itemPool.Release(go); // Cleanup() + SetActive(false) handled by actionOnRelease

            OnItemVisualDespawned?.Invoke(dropId);
        }
    }

    /// <summary>Release all active visual GameObjects back to the pool.</summary>
    private void ClearAllVisuals()
    {
        foreach (var kvp in _activeVisuals)
        {
            if (kvp.Value != null)
                _itemPool?.Release(kvp.Value);
        }
        _activeVisuals.Clear();
    }

    // ── Inventory Integration ─────────────────────────────────────────────────

    /// <summary>
    /// Add the picked-up item back into the local player's inventory.
    /// Uses InventoryGameView.AddItem() which is the existing public API.
    /// </summary>
    private void AddItemToInventory(DroppedItemData data, int amount)
    {
        var inventoryGameView = FindAnyObjectByType<InventoryGameView>();
        if (inventoryGameView == null)
        {
            Debug.LogError("[DroppedItemManagerView] InventoryGameView not found — cannot add picked-up item!");
            return;
        }

        bool added = inventoryGameView.AddItem(data.itemId, amount, data.quality);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManagerView] Added to inventory: {data.itemName} x{amount} (quality={data.quality}) — success={added}");

        if (!added)
        {
            Debug.LogWarning($"[DroppedItemManagerView] Inventory full! Could not add {data.itemName} x{amount}.");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Coroutine to find the local player transform. Retries until found.
    /// Same pattern as ChunkLoadingManager.FindLocalPlayer().
    /// </summary>
    private IEnumerator FindLocalPlayer()
    {
        while (_localPlayerTransform == null)
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
            foreach (GameObject player in players)
            {
                PhotonView pv = player.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    _localPlayerTransform = player.transform;
                    if (showDebugLogs)
                        Debug.Log($"[DroppedItemManagerView] Found local player: {player.name}");
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
