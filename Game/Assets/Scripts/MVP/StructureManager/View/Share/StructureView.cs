using UnityEngine;
using Photon.Pun;

/// <summary>
/// View component for structure placement following MVP pattern.
/// Manages ghost preview, player input, and holds prefab references.
///
/// MVP layer: View
/// - Renders ghost preview based on Presenter decisions
/// - Forwards placement requests to Presenter
/// - Does NOT handle item consumption (ItemUsageController handles that)
/// </summary>
public class StructureView : MonoBehaviourPunCallbacks
{
    public static StructureView Instance { get; private set; }

    [Header("Prefabs by Interaction Type")]
    [Tooltip("Storage (type = 0)")]
    public GameObject StoragePrefab;

    [Tooltip("Crafting (type = 1)")]
    public GameObject CraftingPrefab;

    [Tooltip("Smelting (type = 2)")]
    public GameObject SmeltingPrefab;

    [Tooltip("Fence (type = 3)")]
    public GameObject FencePrefab;

    [Tooltip("Decoration (type = 4)")]
    public GameObject DecorationPrefab;

    [Header("Placement Settings")]
    [Tooltip("Maximum distance from the player to place a structure")]
    [SerializeField] private float placementRange = 2f;

    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";

    [Tooltip("Tag for the player camera")]
    public string playerCameraTag = "MainCamera";

    [Header("Ghost Preview")]
    [Tooltip("Assign a dedicated GameObject here to use as ghost preview")]
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

    // Ghost preview
    private GameObject     ghostInstance;
    private SpriteRenderer ghostRenderer;

    // Active structure being placed
    private StructureData activeStructureData;

    // Active item from inventory
    private ItemModel activeItemModel;

    // Pool reference
    private StructurePool structurePool;

    // HotbarView reference
    private HotbarView hotbarView;

    // Snapped grid position for the current frame
    private Vector3 currentSnappedPos;
    private bool    currentCanPlace;

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
    }

    /// <summary>
    /// Get default prefab based on structure interaction type.
    /// Pure View concern: maps interaction types to Inspector-assigned prefab assets.
    /// </summary>
    public GameObject GetDefaultPrefab(StructureInteractionType interactionType)
    {
        return interactionType switch
        {
            StructureInteractionType.Storage    => StoragePrefab,
            StructureInteractionType.Crafting   => CraftingPrefab,
            StructureInteractionType.Smelting   => SmeltingPrefab,
            StructureInteractionType.Fence      => FencePrefab,
            StructureInteractionType.Decoration => DecorationPrefab,
            _                                   => DecorationPrefab
        };
    }

    // ── Auto-activate ghost when a Structure item is selected ──────────────

    private void UpdateActiveStructureFromHotbar()
    {
        var currentItemModel = hotbarView?.GetCurrentItem();
        var currentItem      = currentItemModel?.ItemData;

        if (currentItem != null && currentItem.itemType == ItemType.Structure)
        {
            // Delegate data-building to Presenter
            var data = presenter.GetStructureData(currentItem.itemID, GetDefaultPrefab);
            if (data != null)
            {
                if (activeStructureData != null && activeStructureData.StructureId == data.StructureId)
                    return;

                activeItemModel = currentItemModel;
                EnterPlacementMode(data);
                return;
            }
        }

        if (activeStructureData != null)
            ExitPlacementMode();
    }

    // ── MVP Initialization ────────────────────────────────────────────────

    private void InitializeMVP()
    {
        var syncManager    = FindAnyObjectByType<ChunkDataSyncManager>();
        var loadingManager = FindAnyObjectByType<ChunkLoadingManager>();
        var pool           = FindAnyObjectByType<StructurePool>();

        IStructureService structureService = new StructureService(syncManager, loadingManager, pool, showDebugLogs);
        presenter = new StructurePresenter(structureService, showDebugLogs);
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Called by ItemUsageController when player clicks to use a structure item.
    /// Returns true if placement was successful (caller handles item consumption).
    /// </summary>
    public bool TryPlaceActiveStructure(string itemId)
    {
        if (activeStructureData == null || activeStructureData.StructureId != itemId) return false;
        if (!currentCanPlace) return false;

        bool placed = presenter.HandlePlaceStructure(currentSnappedPos, activeStructureData);
        if (placed && showDebugLogs)
            Debug.Log($"[StructureView] Structure placed at {currentSnappedPos}");

        return placed;
    }

    public void EnterPlacementMode(StructureData data)
    {
        if (data == null) return;

        if (activeStructureData != null)
            ExitPlacementMode();

        activeStructureData = data;

        if (ghostPreview == null)
        {
            Debug.LogWarning("[StructureView] ghostPreview is not assigned.");
            return;
        }

        ghostInstance = ghostPreview;

        ghostRenderer = ghostInstance.GetComponentInChildren<SpriteRenderer>(true)
            ?? ghostInstance.AddComponent<SpriteRenderer>();

        Sprite original = activeItemModel?.Icon
            ?? ItemCatalogService.Instance?.GetCachedSprite(data.StructureId);
        if (original != null)
        {
            ghostRenderer.sprite = Sprite.Create(
                original.texture,
                original.rect,
                new Vector2(0.5f, 0f),
                original.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
        }

        foreach (var col in ghostInstance.GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        ghostInstance.SetActive(true);

        if (showDebugLogs)
            Debug.Log($"[StructureView] Entered placement mode: {data.DisplayName}");
    }

    public void ExitPlacementMode()
    {
        if (ghostInstance != null)
        {
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
    }

    public bool IsInPlacementMode => activeStructureData != null;

    // ── Ghost Preview ─────────────────────────────────────────────────────

    private void UpdateGhostPreview()
    {
        if (ghostInstance == null || targetCamera == null || playerTransform == null)
            return;

        if (!TryGetTargetTile(out Vector3 tile))
        {
            ghostInstance.SetActive(false);
            currentCanPlace = false;
            return;
        }

        currentSnappedPos = new Vector3(Mathf.Floor(tile.x) + 0.5f, Mathf.Floor(tile.y) + 0.25f, 0f);
        ghostInstance.transform.position = currentSnappedPos;
        ghostInstance.SetActive(true);

        currentCanPlace = presenter.CanPlace(currentSnappedPos, activeStructureData);
        if (ghostRenderer != null)
            ghostRenderer.color = currentCanPlace ? validColor : invalidColor;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private bool TryGetTargetTile(out Vector3 tileCenter)
    {
        tileCenter = Vector3.zero;
        if (playerTransform == null)
            return false;

        Vector3 mouseWorld = ScreenToWorld(Input.mousePosition);
        mouseWorld.z = 0f;

        int targetX = Mathf.FloorToInt(mouseWorld.x);
        int targetY = Mathf.FloorToInt(mouseWorld.y);
        tileCenter = new Vector3(targetX, targetY, 0f);

        float distance = Vector3.Distance(playerTransform.position, tileCenter);
        return distance <= placementRange;
    }

    private void CachePlayerTransform()
    {
        if (playerTransform != null) return;
        TryResolvePlayerTransform(out playerTransform);
    }

    private bool TryResolvePlayerTransform(out Transform resolvedTransform)
    {
        resolvedTransform = null;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        if (players == null || players.Length == 0)
            return false;

        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (PhotonNetwork.IsConnected && (pv == null || !pv.IsMine))
                continue;

            Transform center = player.transform.Find("CenterPoint");
            resolvedTransform = center != null ? center : player.transform;
            return true;
        }

        if (!PhotonNetwork.IsConnected)
        {
            Transform center = players[0].transform.Find("CenterPoint");
            resolvedTransform = center != null ? center : players[0].transform;
            return true;
        }

        return false;
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
