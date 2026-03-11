using UnityEngine;
using Photon.Pun;

/// <summary>
/// View component for structure placement following the MVP pattern.
/// Manages the ghost preview, handles player input, and delegates
/// business logic to the StructurePresenter.
/// </summary>
public class StructureView : MonoBehaviourPunCallbacks
{
    public static StructureView Instance { get; private set; }

    [Header("Placement Settings")]
    [Tooltip("Maximum distance from the player to place a structure")]
    [SerializeField] private float placementRange = 2f;

    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";

    [Tooltip("Tag for the player camera")]
    public string playerCameraTag = "MainCamera";

    [Header("Ghost Preview")]
    [Tooltip("Assign a dedicated GameObject here to use as ghost preview instead of the object pool.")]
    [SerializeField] private GameObject ghostPreview;

    [Tooltip("Color tint when placement is valid")]
    public Color validColor   = new Color(0f, 1f, 0f, 0.5f);

    [Tooltip("Color tint when placement is invalid")]
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    // ── Runtime refs ──────────────────────────────────────────────────────
    private Camera          targetCamera;
    private Transform       playerTransform;

    // MVP
    private StructurePresenter presenter;
    private IStructureService  structureService;

    // Ghost preview
    private GameObject     ghostInstance;
    private SpriteRenderer ghostRenderer;

    // Active structure being placed (set externally via EnterPlacementMode)
    private StructureDataSO activeStructureData;

    // Active item from inventory — used to resolve the sprite via ItemCatalogService
    private ItemModel activeItemModel;

    // Pool reference
    private StructurePool structurePool;

    // HotbarView reference for item consumption (mirrors CropPlantingView pattern)
    private HotbarView hotbarView;

    // Snapped grid position for the current frame
    private Vector3 currentSnappedPos;
    private bool    currentCanPlace;

    /// <summary>
    /// Optional hook invoked after a structure is successfully placed.
    /// StructureView handles consumption internally; this is for external listeners only.
    /// </summary>
    public System.Action OnStructurePlaced;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        targetCamera = FindPlayerCamera();
        InitializeMVP();
    }

    private void Start()
    {
        structurePool = FindAnyObjectByType<StructurePool>();
        hotbarView    = FindAnyObjectByType<HotbarView>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ExitPlacementMode();
    }

    private void Update()
    {
        EnsureRuntimeReferences();
        CachePlayerTransform();

        UpdateActiveStructureFromHotbar();

        if (activeStructureData == null) return;

        UpdateGhostPreview();
        HandlePlacementInput();
    }

    // ── Auto-activate ghost when a Structure item is selected in hotbar ──────
    // Mirrors CropPlowingView / CropPlantingView: preview shows while the item
    // is held, and disappears when the player switches to a different slot.

    private void UpdateActiveStructureFromHotbar()
    {
        var currentItemModel = hotbarView?.GetCurrentItem();
        var currentItem      = currentItemModel?.ItemData;

        if (currentItem != null && currentItem.itemType == ItemType.Structure)
        {
            if (structurePool == null)
                structurePool = FindAnyObjectByType<StructurePool>();

            var so = structurePool?.GetStructureData(currentItem.itemID);
            if (so != null)
            {
                // Already showing ghost for this structure
                if (activeStructureData != null && activeStructureData.StructureId == so.StructureId)
                    return;

                activeItemModel = currentItemModel;
                EnterPlacementMode(so);
                return;
            }
        }

        // Current hotbar item is not a structure — exit placement mode
        if (activeStructureData != null)
            ExitPlacementMode();
    }

    // ── MVP Initialization ────────────────────────────────────────────────

    private void InitializeMVP()
    {
        var syncManager    = FindAnyObjectByType<ChunkDataSyncManager>();
        var loadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        structureService = new StructureService(syncManager, loadingManager, showDebugLogs);
        presenter        = new StructurePresenter(structureService, showDebugLogs);
    }

    // ── Public API (called by HotbarView or Inventory) ────────────────────

    /// <summary>
    /// Enter placement mode for the given structure type.
    /// Shows the ghost preview (ghostPreview) and starts listening for placement clicks.
    /// </summary>
    public void EnterPlacementMode(StructureDataSO data)
    {
        if (data == null) return;

        // Exit previous mode if active
        if (activeStructureData != null)
            ExitPlacementMode();

        activeStructureData = data;

        if (ghostPreview == null)
        {
            Debug.LogWarning("[StructureView] ghostPreview is not assigned — ghost preview will not display.");
            return;
        }

        ghostInstance = ghostPreview;

        ghostRenderer = ghostInstance.GetComponentInChildren<SpriteRenderer>(true)
            ?? ghostInstance.AddComponent<SpriteRenderer>();

        // Update sprite to match current item
        Sprite itemSprite = activeItemModel?.Icon
            ?? ItemCatalogService.Instance?.GetCachedSprite(data.StructureId);
        if (itemSprite != null)
            ghostRenderer.sprite = itemSprite;

        // Disable colliders so ghost does not interfere with physics
        foreach (var col in ghostInstance.GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        ghostInstance.SetActive(true);

        if (showDebugLogs)
            Debug.Log($"[StructureView] Entered placement mode: {data.name}");
    }

    /// <summary>
    /// Exit placement mode — hide the ghost preview object.
    /// </summary>
    public void ExitPlacementMode()
    {
        if (ghostInstance != null)
        {
            // Reset tint and hide — never pool or destroy the override object
            if (ghostRenderer != null)
                ghostRenderer.color = Color.white;

            foreach (var col in ghostInstance.GetComponentsInChildren<Collider2D>())
                col.enabled = true;

            ghostInstance.SetActive(false);

            ghostInstance = null;
            ghostRenderer = null;
        }

        activeStructureData = null;
        activeItemModel    = null;
        OnStructurePlaced  = null;
    }

    /// <summary>Whether we are currently in placement mode.</summary>
    public bool IsInPlacementMode => activeStructureData != null;

    /// <summary>Provides <see cref="StructurePresenter"/> so external code (ChunkDataSyncManager) can relay network events.</summary>
    public StructurePresenter Presenter => presenter;

    // ── Ghost Preview ─────────────────────────────────────────────────────

    private void UpdateGhostPreview()
    {
        if (ghostInstance == null || targetCamera == null || playerTransform == null)
            return;

        Vector3 tile = GetTargetTile();
        if (tile == Vector3.zero)
        {
            ghostInstance.SetActive(false);
            return;
        }

        // Snap to grid
        currentSnappedPos = new Vector3(Mathf.Floor(tile.x), Mathf.Floor(tile.y), 0f);
        ghostInstance.transform.position = currentSnappedPos;
        ghostInstance.SetActive(true);

        // Validate and tint
        currentCanPlace = presenter.CanPlace(currentSnappedPos, activeStructureData);
        if (ghostRenderer != null)
            ghostRenderer.color = currentCanPlace ? validColor : invalidColor;
    }

    // ── Input Handling ────────────────────────────────────────────────────

    private void HandlePlacementInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!currentCanPlace) return;
        if (activeStructureData == null) return;

        bool placed = presenter.HandlePlaceStructure(currentSnappedPos, activeStructureData);
        if (placed)
        {
            if (showDebugLogs)
                Debug.Log($"[StructureView] Structure placed at {currentSnappedPos}");

            // Spawn the real structure GameObject on the map (View responsibility)
            SpawnPlacedStructure(activeStructureData, currentSnappedPos);

            // Consume one item from hotbar — mirrors CropPlantingView pattern
            hotbarView?.GetPresenter()?.ConsumeCurrentItem(1);

            // Notify external listeners (optional)
            OnStructurePlaced?.Invoke();
        }
    }

    // ── Structure Spawning (Pool) ─────────────────────────────────────────

    /// <summary>
    /// Spawns a real (non-ghost) structure GameObject from the pool at the given position.
    /// </summary>
    public void SpawnPlacedStructure(StructureDataSO data, Vector3 worldPosition)
    {
        if (data == null || data.Prefab == null) return;

        GameObject obj;
        if (structurePool != null)
            obj = structurePool.Get(data.StructureId);
        else
            obj = Instantiate(data.Prefab);

        obj.transform.position = worldPosition;
        SpriteRenderer sr = obj.GetComponentInChildren<SpriteRenderer>(true)
            ?? obj.AddComponent<SpriteRenderer>();

        // Apply sprite from item catalog so the placed structure uses the inventory item image
        Sprite itemSprite = ItemCatalogService.Instance?.GetCachedSprite(data.StructureId);
        if (itemSprite != null)
            sr.sprite = itemSprite;

        sr.sortingLayerName = "WalkInfront";

        obj.SetActive(true);

        // Re-enable colliders for interaction
        foreach (var col in obj.GetComponentsInChildren<Collider2D>())
            col.enabled = true;
    }

    /// <summary>
    /// Returns a placed structure to the pool (called on demolish or chunk unload).
    /// </summary>
    public void DespawnStructure(StructureDataSO data, GameObject obj)
    {
        if (obj == null) return;

        if (structurePool != null && data != null)
            structurePool.Release(data.StructureId, obj);
        else
            Destroy(obj);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Vector3 GetTargetTile()
    {
        if (playerTransform == null)
            return Vector3.zero;

        Vector3 mouseWorld = ScreenToWorld(Input.mousePosition);
        Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
        return CropTileSelector.GetDirectionalTile(
            playerTransform.position,
            mouseWorld,
            placementRange,
            ref dummy);
    }

    private void CachePlayerTransform()
    {
        if (playerTransform != null) return;
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        Transform center = player.transform.Find("CenterPoint");
        playerTransform = center != null ? center : player.transform;
    }

    private Camera FindPlayerCamera()
    {
        GameObject camObj = GameObject.FindGameObjectWithTag(playerCameraTag);
        return camObj != null ? camObj.GetComponent<Camera>() : Camera.main;
    }

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        if (targetCamera == null) return Vector3.zero;
        screenPos.z = -targetCamera.transform.position.z;
        Vector3 world = targetCamera.ScreenToWorldPoint(screenPos);
        world.z = 0f;
        return world;
    }

    private void EnsureRuntimeReferences()
    {
        if (targetCamera == null)
            targetCamera = FindPlayerCamera();

        if (structurePool == null)
            structurePool = FindAnyObjectByType<StructurePool>();
    }
}
