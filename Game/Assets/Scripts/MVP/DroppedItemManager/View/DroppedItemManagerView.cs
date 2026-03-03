using UnityEngine;
using Photon.Pun;
using System;
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
    [SerializeField] private Vector2 dropOffset = new Vector2(0f, -0.5f);

    [Tooltip("Enable debug logging.")]
    [SerializeField] private bool showDebugLogs = true;

    // ── MVP Components ────────────────────────────────────────────────────────

    private DroppedItemPresenter presenter;
    private IDroppedItemService service;

    // ── Runtime State ─────────────────────────────────────────────────────────

    /// <summary>Active visual GameObjects keyed by dropId.</summary>
    private readonly Dictionary<string, GameObject> _activeVisuals = new();

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
        }

        // Cleanup presenter (unsubscribes from sync + chunk events)
        presenter?.Cleanup();

        // Cleanup visuals
        ClearAllVisuals();

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
            Debug.LogWarning("[DroppedItemManagerView] Local player not found, cannot drop item.");
            return;
        }

        presenter?.RequestDropItem(item, _localPlayerTransform.position, dropOffset);
    }

    /// <summary>
    /// Called by DroppedItemView when the local player presses the pickup key.
    /// Delegates to Presenter which sends pickup request through Photon.
    /// </summary>
    /// <param name="dropId">The unique ID of the item to pick up.</param>
    public void RequestPickupItem(string dropId)
    {
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
        AddItemToInventory(data);
    }

    private void HandleClearAllVisualsRequested()
    {
        ClearAllVisuals();
    }

    // ── Visual Spawn / Despawn ────────────────────────────────────────────────

    /// <summary>
    /// Instantiate the DroppedItem prefab and initialize its View.
    /// </summary>
    private void SpawnItemVisual(DroppedItemData data)
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogError("[DroppedItemManagerView] droppedItemPrefab is not assigned!");
            return;
        }

        if (_activeVisuals.ContainsKey(data.dropId))
            return; // Already spawned

        Vector3 spawnPos = new Vector3(data.worldX, data.worldY, 0f);
        GameObject go = Instantiate(droppedItemPrefab, spawnPos, Quaternion.identity);
        go.name = $"DroppedItem_{data.itemName}_{data.dropId[..Mathf.Min(8, data.dropId.Length)]}";

        // Initialize DroppedItemView on the prefab
        var view = go.GetComponent<DroppedItemView>();
        if (view == null)
        {
            Debug.LogError("[DroppedItemManagerView] Prefab missing DroppedItemView! Cannot spawn item.");
            Destroy(go);
            return;
        }

        view.Initialize(data);

        _activeVisuals[data.dropId] = go;
        OnItemVisualSpawned?.Invoke(data);
    }

    /// <summary>
    /// Destroy the visual GameObject for a dropped item.
    /// </summary>
    private void DespawnItemVisual(string dropId)
    {
        if (_activeVisuals.TryGetValue(dropId, out GameObject go))
        {
            _activeVisuals.Remove(dropId);

            if (go != null)
            {
                var view = go.GetComponent<DroppedItemView>();
                view?.Cleanup();
                Destroy(go);
            }

            OnItemVisualDespawned?.Invoke(dropId);
        }
    }

    /// <summary>Clear all active visual GameObjects.</summary>
    private void ClearAllVisuals()
    {
        foreach (var kvp in _activeVisuals)
        {
            if (kvp.Value != null)
            {
                var view = kvp.Value.GetComponent<DroppedItemView>();
                view?.Cleanup();
                Destroy(kvp.Value);
            }
        }
        _activeVisuals.Clear();
    }

    // ── Inventory Integration ─────────────────────────────────────────────────

    /// <summary>
    /// Add the picked-up item back into the local player's inventory.
    /// Uses InventoryGameView.AddItem() which is the existing public API.
    /// </summary>
    private void AddItemToInventory(DroppedItemData data)
    {
        var inventoryGameView = FindAnyObjectByType<InventoryGameView>();
        if (inventoryGameView == null)
        {
            Debug.LogError("[DroppedItemManagerView] InventoryGameView not found — cannot add picked-up item!");
            return;
        }

        bool added = inventoryGameView.AddItem(data.itemId, data.quantity, data.quality);

        if (showDebugLogs)
            Debug.Log($"[DroppedItemManagerView] Added to inventory: {data.itemName} x{data.quantity} (quality={data.quality}) — success={added}");

        if (!added)
        {
            Debug.LogWarning($"[DroppedItemManagerView] Inventory full! Could not add {data.itemName} x{data.quantity}. " +
                             "Item was already removed from world — consider re-dropping.");
            // TODO: Optionally re-drop the item if inventory is full
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
