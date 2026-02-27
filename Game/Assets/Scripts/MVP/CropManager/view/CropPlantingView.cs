using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// View component for crop planting following MVP pattern.
/// Preview and planting are driven by the HotbarView:
///   • Preview is shown whenever the selected hotbar slot contains a SeedDataSO.
///   • Planting fires on HotbarView.OnUseItemInput (left-click).
/// </summary>
public class CropPlantingView : MonoBehaviourPunCallbacks
{
    public static CropPlantingView Instance { get; private set; }

    [Header("Planting Settings")]
    [Tooltip("Planting mode: at mouse, around player (1 tile radius), or far around player (2 tile radius)")]
    public PlantingMode plantingMode = PlantingMode.AroundPlayer;
    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";
    [Tooltip("Maximum distance from player to plant crops")]
    [SerializeField] private float plantingRange = 2f;
    [Tooltip("Tag to search for player camera if targetCamera is null")]
    public string playerCameraTag = "MainCamera";

    [Header("Tile Preview")]
    [Range(0f, 1f)] public float previewAlpha = 0.5f;
    public string previewSortingLayer = "WalkInfront";
    public int    previewSortingOrder = 10;

    [Header("Hold to Plant")]
    [Tooltip("Hold left-click to keep planting at intervals.")]
    public bool allowHoldToPlant = true;
    [Tooltip("Seconds between each automatic plant while holding left-click.")]
    public float plantRepeatInterval = 0.3f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // ── Runtime deps ───────────────────────────────────────────────────────
    private Camera targetCamera;
    private Transform playerTransform;
    private HotbarView hotbarView;

    // current seed derived from hotbar; null when no seed selected
    private SeedData _currentSeed;
    // TODO: Reconnect CurrentPlantId when SeedData.plantId is wired (requires PlantData refactor)
    public string CurrentPlantId => string.Empty;

    // Tile preview
    private SpriteRenderer _previewSR;

    // Hold-to-plant state
    private float _holdTimer = 0f;

    // MVP
    private CropPlantingPresenter presenter;
    private ICropPlantingService cropPlantingService;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (targetCamera == null)
            targetCamera = FindPlayerCamera();

        // Build inline preview sprite renderer (hidden by default)
        var previewGO = new GameObject("PlantingPreview");
        previewGO.transform.SetParent(transform, false);
        _previewSR                  = previewGO.AddComponent<SpriteRenderer>();
        _previewSR.color            = new Color(1f, 1f, 1f, previewAlpha);
        _previewSR.sortingLayerName = previewSortingLayer;
        _previewSR.sortingOrder     = previewSortingOrder;
        _previewSR.enabled          = false;

        InitializeMVP();
    }

    private void Start()
    {
        hotbarView = FindAnyObjectByType<HotbarView>();
        if (hotbarView == null)
            Debug.LogWarning("[CropPlantingView] HotbarView not found — preview will not show.");

        // Subscribe to seed-use event fired by UseSeedService
        UseSeedService.OnSeedRequested += HandleSeedUseRequested;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UseSeedService.OnSeedRequested -= HandleSeedUseRequested;

        if (_previewSR != null) _previewSR.enabled = false;
    }

    // ── Update ─────────────────────────────────────────────────────────────
    private void Update()
    {
        if (targetCamera == null)
            targetCamera = FindPlayerCamera();

        if (playerTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                Transform centerPoint = playerEntity.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : playerEntity.transform;
            }
        }

        // Derive current seed from hotbar each frame
        _currentSeed = hotbarView?.GetCurrentItem()?.ItemData as SeedData;

        UpdatePlantingPreview();
        HandleHoldToPlant();
    }

    private void HandleHoldToPlant()
    {
        if (!allowHoldToPlant || _currentSeed == null || cropPlantingService == null) return;

        if (Input.GetMouseButton(0))
        {
            _holdTimer -= Time.deltaTime;
            if (_holdTimer <= 0f)
            {
                _holdTimer = plantRepeatInterval;
                bool planted = TryPlantFromItemUse();
                if (planted)
                    hotbarView?.GetPresenter()?.ConsumeCurrentItem(1);
            }
        }
        else
        {
            // Reset so the first frame of hold plants immediately
            _holdTimer = 0f;
        }
    }

    // ── Seed-use event handler ─────────────────────────────────────────────

    /// <summary>
    /// Received from ItemUsageController.OnSeedUseRequested (event — no direct coupling).
    /// Plants at the current directional tile and consumes one seed only on success.
    /// </summary>
    private void HandleSeedUseRequested(string itemId)
    {
        // Resolve SeedData from catalog — accept any seed, not just the one currently previewed
        var seedData = ItemCatalogService.Instance?.GetItemData<SeedData>(itemId);
        if (seedData != null)
            _currentSeed = seedData;

        bool planted = TryPlantFromItemUse();
        if (planted)
            hotbarView?.GetPresenter()?.ConsumeCurrentItem(1);
    }
    public bool TryPlantFromItemUse()
    {
        if (cropPlantingService == null || _currentSeed == null) return false;


        string plantId = CurrentPlantId;
        if (string.IsNullOrEmpty(plantId)) return false;

        List<Vector3> positions = CalculatePlantingPositions();
        if (positions.Count == 0) return false;

        bool anyPlanted = false;
        foreach (Vector3 pos in positions)
        {
            if (cropPlantingService.PlantCrop(pos, plantId))
            {
                anyPlanted = true;
                if (showDebugLogs)
                    Debug.Log($"[CropPlantingView] Planted '{plantId}' at {pos}.");
            }
        }
        return anyPlanted;
    }
    // ── Preview ────────────────────────────────────────────────────────────

    private void UpdatePlantingPreview()
    {
        if (_previewSR == null) return;

        // TODO: Restore preview sprite when SeedData.plantId / CropDataSo link is wired
        Sprite seedSprite = null;

        if (seedSprite == null || targetCamera == null || playerTransform == null)
        {
            _previewSR.enabled = false;
            return;
        }

        Vector3 tile = GetPreviewTargetTile();
        if (tile == Vector3.zero)
        {
            _previewSR.enabled = false;
            return;
        }

        _previewSR.sprite   = seedSprite;
        _previewSR.enabled  = true;
        _previewSR.transform.position = new Vector3(
            Mathf.Floor(tile.x),
            Mathf.Floor(tile.y) + 0.062f,
            0f);
    }

    /// <summary>Calculates target tile for preview every frame (no deduplication).</summary>
    private Vector3 GetPreviewTargetTile()
    {
        Vector3 playerPos     = playerTransform.position;
        Vector3 mouseWorldPos = ScreenToWorldPosition(Input.mousePosition);
        mouseWorldPos.z       = 0f;

        int playerTileX = Mathf.RoundToInt(playerPos.x);
        int playerTileY = Mathf.RoundToInt(playerPos.y);

        Vector2 dir = new Vector2(mouseWorldPos.x - playerPos.x, mouseWorldPos.y - playerPos.y);

        int offsetX = 0, offsetY = 0;
        if (dir.magnitude >= 0.5f)
        {
            dir.Normalize();
            if      (dir.x >  0.4f) offsetX =  1;
            else if (dir.x < -0.4f) offsetX = -1;
            if      (dir.y >  0.4f) offsetY =  1;
            else if (dir.y < -0.4f) offsetY = -1;
        }

        int maxRadius = (plantingMode == PlantingMode.FarAroundPlayer) ? 2 : 1;
        offsetX = Mathf.Clamp(offsetX, -maxRadius, maxRadius);
        offsetY = Mathf.Clamp(offsetY, -maxRadius, maxRadius);

        Vector3 target = new Vector3(playerTileX + offsetX, playerTileY + offsetY, 0f);
        return Vector3.Distance(playerPos, target) <= plantingRange ? target : Vector3.zero;
    }

    // ── Planting positions ─────────────────────────────────────────────────

    private List<Vector3> CalculatePlantingPositions()
    {
        var positions = new List<Vector3>();

        switch (plantingMode)
        {
            case PlantingMode.AtMouse:
                positions.Add(GetMouseWorldPosition());
                break;
            case PlantingMode.AroundPlayer:
            {
                Vector3 pos = GetDirectionalTileAroundPlayer(1);
                if (pos != Vector3.zero) positions.Add(pos);
                break;
            }
            case PlantingMode.FarAroundPlayer:
            {
                Vector3 farPos = GetDirectionalTileAroundPlayer(2);
                if (farPos != Vector3.zero) positions.Add(farPos);
                break;
            }
        }

        return positions;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (targetCamera == null) return Vector3.zero;
        Vector3 worldPos = ScreenToWorldPosition(Input.mousePosition);
        return new Vector3(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y), 0);
    }

    private Vector3 GetDirectionalTileAroundPlayer(int maxRadius)
    {
        if (targetCamera == null || playerTransform == null) return Vector3.zero;
        Vector3 playerPos     = playerTransform.position;
        Vector3 mouseWorldPos = ScreenToWorldPosition(Input.mousePosition);
        // No deduplication — planting is single-click, every click should evaluate fresh
        Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
        return CropTileSelector.GetDirectionalTile(playerPos, mouseWorldPos, plantingRange, ref dummy, maxRadius);
    }

    private Vector3 ScreenToWorldPosition(Vector3 screenPosition)
    {
        if (targetCamera == null) return Vector3.zero;
        Vector3 pos = screenPosition;
        pos.z = targetCamera.transform.position.z * -1;
        Vector3 world = targetCamera.ScreenToWorldPoint(pos);
        world.z = 0;
        return world;
    }

    // ── Camera search ──────────────────────────────────────────────────────

    private Camera FindPlayerCamera()
    {
        GameObject camObj = GameObject.FindGameObjectWithTag(playerCameraTag);
        if (camObj != null)
        {
            Camera cam = camObj.GetComponent<Camera>();
            if (cam != null) return cam;
        }
        if (Camera.main != null) return Camera.main;
        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        return cams.Length > 0 ? cams[0] : null;
    }

    // ── MVP init ───────────────────────────────────────────────────────────

    private void InitializeMVP()
    {
        ChunkDataSyncManager syncManager    = FindAnyObjectByType<ChunkDataSyncManager>();
        ChunkLoadingManager  loadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        if (syncManager   == null) Debug.LogWarning("[CropPlantingView] ChunkDataSyncManager not found!");
        if (loadingManager == null) Debug.LogWarning("[CropPlantingView] ChunkLoadingManager not found!");

        cropPlantingService = new CropPlantingService(syncManager, loadingManager, showDebugLogs);
        presenter           = new CropPlantingPresenter(cropPlantingService, showDebugLogs);
    }

    // ── Public API (kept for external callers) ─────────────────────────────

    /// <summary>Plant at a specific screen position with an explicit plantId (for UI buttons etc.).</summary>
    public void PlantCropAtPosition(Vector3 screenPosition, string plantId)
    {
        if (presenter == null || targetCamera == null || string.IsNullOrEmpty(plantId)) return;
        List<Vector3> positions = new List<Vector3> { ScreenToWorldPosition(screenPosition) };
        presenter.HandlePlantCrops(positions, plantId);
    }

    // ── Photon RPC ─────────────────────────────────────────────────────────

    [PunRPC]
    private void RPC_PlantCrop(Vector3 worldPosition, string plantId)
    {
        presenter?.HandleNetworkCropPlanted(worldPosition, plantId);
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Transform targetTransform = playerTransform;
        if (targetTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                Transform cp = playerEntity.transform.Find("CenterPoint");
                targetTransform = cp != null ? cp : playerEntity.transform;
            }
        }

        if (targetTransform == null || plantingRange <= 0) return;

        switch (plantingMode)
        {
            case PlantingMode.AroundPlayer:
            case PlantingMode.FarAroundPlayer:
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(targetTransform.position, plantingRange);
                break;
            case PlantingMode.AtMouse:
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawWireSphere(targetTransform.position, 0.3f);
                break;
        }
    }
}
