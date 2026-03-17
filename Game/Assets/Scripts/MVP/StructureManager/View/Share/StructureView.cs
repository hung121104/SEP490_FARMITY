using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// View component for structure placement following MVP pattern.
/// Manages ghost preview, player input, and holds prefab references.
/// Merged functionality from StructureCatalogController.
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
    private IStructureService  structureService;

    // Ghost preview
    private GameObject     ghostInstance;
    private SpriteRenderer ghostRenderer;

    // Active structure being placed 
    private StructureData activeStructureData;

    // Active item from inventory
    private ItemModel activeItemModel;

    // Pool reference
    private StructurePool structurePool;

    // HotbarView reference for item consumption
    private HotbarView hotbarView;

    // Snapped grid position for the current frame
    private Vector3 currentSnappedPos;
    private bool    currentCanPlace;

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

    /// <summary>
    /// Get default prefab based on structure interaction type.
    /// </summary>
    public GameObject GetDefaultPrefab(StructureInteractionType interactionType)
    {
        return interactionType switch
        {
            StructureInteractionType.Storage => StoragePrefab,
            StructureInteractionType.Crafting => CraftingPrefab,
            StructureInteractionType.Smelting => SmeltingPrefab,
            StructureInteractionType.Fence => FencePrefab,
            StructureInteractionType.Decoration => DecorationPrefab,          
            _ => DecorationPrefab
        };
    }

    /// <summary>
    /// Build StructureData from ItemData.
    /// </summary>
    public StructureData BuildStructureData(StructureItemData itemData)
    {
        if (itemData == null) return null;

        StructureInteractionType interactionType = (StructureInteractionType)itemData.structureInteractionType;

        int maxHealth = itemData.maxHealth;

        // Resolve prefab
        GameObject prefab = GetDefaultPrefab(interactionType);

        if (prefab == null)
        {
            Debug.LogWarning($"[StructureView] No prefab for '{itemData.itemID}'");
            return null;
        }
 
        return new StructureData
        {
            StructureId = itemData.itemID,
            DisplayName = itemData.itemName,
            InteractionType = interactionType,
            MaxHealth = maxHealth,
            Prefab = prefab
        };
    }

    /// <summary>
    /// Get StructureData for an itemID.
    /// </summary>
    public StructureData GetStructureData(string itemID)
    {
        var itemData = ItemCatalogService.Instance?.GetItemData(itemID) as StructureItemData;
        return BuildStructureData(itemData);
    }

    // ── Auto-activate ghost when a Structure item is selected ──────────────

    private void UpdateActiveStructureFromHotbar()
    {
        var currentItemModel = hotbarView?.GetCurrentItem();
        var currentItem      = currentItemModel?.ItemData;

        if (currentItem != null && currentItem.itemType == ItemType.Structure)
        {
            var data = GetStructureData(currentItem.itemID);
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

        structureService = new StructureService(syncManager, loadingManager, showDebugLogs);
    }

    // ── Public API ───────────────────────────────────────────────────────

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

        Sprite itemSprite = activeItemModel?.Icon
            ?? ItemCatalogService.Instance?.GetCachedSprite(data.StructureId);
        if (itemSprite != null)
            ghostRenderer.sprite = itemSprite;

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
        OnStructurePlaced  = null;
    }

    public bool IsInPlacementMode => activeStructureData != null;

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

        currentSnappedPos = new Vector3(Mathf.Floor(tile.x) + 0.5f, Mathf.Floor(tile.y) + 0.5f, 0f);
        ghostInstance.transform.position = currentSnappedPos;
        ghostInstance.SetActive(true);

        currentCanPlace = structureService.CanPlaceStructure(currentSnappedPos, activeStructureData);
        if (ghostRenderer != null)
            ghostRenderer.color = currentCanPlace ? validColor : invalidColor;
    }

    // ── Input Handling ────────────────────────────────────────────────────

    private void HandlePlacementInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!currentCanPlace) return;
        if (activeStructureData == null) return;

        bool placed = structureService.PlaceStructure(currentSnappedPos, activeStructureData);
        if (placed)
        {
            if (showDebugLogs)
                Debug.Log($"[StructureView] Structure placed at {currentSnappedPos}");

            hotbarView?.GetPresenter()?.ConsumeCurrentItem(1);
            OnStructurePlaced?.Invoke();
        }
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
