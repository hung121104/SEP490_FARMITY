using Photon.Pun;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CropPlowingView : MonoBehaviour
{
    [Header("Tile Reference")]
    [SerializeField] private TileBase tilledTile;
    
    
    [Header("Player Settings")]
    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";
    
    [Header("Plowing Settings")]
    [SerializeField] private float plowingRange = 2f;
    [SerializeField] private bool showDebugLogs = false;

    [Header("Mouse Hold to Plow")]
    [Tooltip("Hold left-click (with Hoe equipped) to keep plowing at intervals.")]
    [SerializeField] private bool allowMouseHoldToPlow = true;
    [Tooltip("Seconds between each automatic plow while holding left-click.")]
    [SerializeField] private float mouseHoldRepeatInterval = 0.3f;

    [Header("Plow Preview")]
    [Tooltip("Show a sprite preview at the target tile when the Hoe is equipped.")]
    [SerializeField] private bool showPlowPreview = true;
    [Tooltip("Sprite to show at the target tile when the Hoe is equipped (drag the tilled dirt sprite here).")]
    [SerializeField] private Sprite previewSprite;
    [Range(0f, 1f)][SerializeField] private float previewAlpha = 0.5f;
    [SerializeField] private string previewSortingLayer = "WalkInfront";
    [SerializeField] private int    previewSortingOrder = 10;

    private CropPlowingPresenter presenter;
    public CropPlowingPresenter GetPresenter() => presenter;
    private Transform playerTransform;
    private HotbarView hotbarView;
    private IUseToolService toolUseService;
    private float _mouseHoldTimer = 0f;
    private SpriteRenderer _previewSR;
    private Vector3 _lastMouseWorldPos;  // raw mouse pos before tile snap — used for anim direction    

    private void Start()
    {
        // Initialize the MVP pattern
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        ICropPlowingService service = new CropPlowingService(syncManager, showDebugLogs);
        presenter = new CropPlowingPresenter(this, service);
        toolUseService = new UseToolService();
        
        // Initialize the presenter with tilled tile reference
        presenter.Initialize(tilledTile);
        
        // Validate references
        ValidateReferences();

        // Subscribe to hoe-use event fired by UseToolService
        UseToolService.OnHoeRequested += HandleHoeUseRequested;

        // Find hotbar (for current item check + preview icon)
        hotbarView = FindAnyObjectByType<HotbarView>();

        // Build inline preview SpriteRenderer
        var previewGO = new GameObject("PlowPreview");
        previewGO.transform.SetParent(transform, false);
        _previewSR                  = previewGO.AddComponent<SpriteRenderer>();
        _previewSR.color            = new Color(1f, 1f, 1f, previewAlpha);
        _previewSR.sortingLayerName = previewSortingLayer;
        _previewSR.sortingOrder     = previewSortingOrder;
        _previewSR.enabled          = false;
    }
    
    private void Update()
    {
        // Re-check player if it becomes null
        if (playerTransform == null)
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag(playerTag))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (PhotonNetwork.IsConnected && (pv == null || !pv.IsMine))
                    continue;

                Transform centerPoint = go.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : go.transform;
                break;
            }
        }
        
        // Update preview and mouse-hold every frame
        UpdatePlowPreview();
        HandleMouseHoldPlow();
    }

    private void UpdatePlowPreview()
    {
        if (_previewSR == null || playerTransform == null || !showPlowPreview)
        {
            if (_previewSR != null) _previewSR.enabled = false;
            return;
        }

        // Show only when a Hoe is the active hotbar item
        var currentItem = hotbarView?.GetCurrentItem()?.ItemData as ToolData;
        if (currentItem == null || currentItem.toolType != ToolType.Hoe)
        {
            _previewSR.enabled = false;
            return;
        }

        Vector3 tile = GetPreviewTargetTile();
        if (tile == Vector3.zero || presenter == null || !presenter.IsTillable(tile))
        {
            _previewSR.enabled = false;
            return;
        }

        _previewSR.sprite  = previewSprite;

        _previewSR.enabled = true;
        _previewSR.transform.position = new Vector3(
            Mathf.Floor(tile.x)+.5f,
            Mathf.Floor(tile.y) + 0.5f,
            0f);
    }

    private void HandleMouseHoldPlow()
    {
        if (!allowMouseHoldToPlow || presenter == null || playerTransform == null) return;

        // Only active when a Hoe is equipped
        var currentItem = hotbarView?.GetCurrentItem()?.ItemData as ToolData;
        if (currentItem == null || currentItem.toolType != ToolType.Hoe) return;

        if (InputManager.Instance?.UseItem.IsPressed() ?? false)
        {
            _mouseHoldTimer -= Time.deltaTime;
            if (_mouseHoldTimer <= 0f)
            {
                _mouseHoldTimer = mouseHoldRepeatInterval;
                Vector3 mouseWorldPos = Camera.main != null
                    ? Camera.main.ScreenToWorldPoint(Input.mousePosition)
                    : Vector3.zero;
                mouseWorldPos.z = 0f;
                _lastMouseWorldPos = mouseWorldPos;  // store before snap

                Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
                Vector3 snappedTile = CropTileSelector.GetDirectionalTile(
                    playerTransform.position, mouseWorldPos, plowingRange, ref dummy);

                if (snappedTile != Vector3.zero && presenter.IsTillable(snappedTile))
                    toolUseService?.UseHoe(currentItem, mouseWorldPos);
            }
        }
        else
        {
            _mouseHoldTimer = 0f;  // reset so next press fires immediately
        }
    }

    // Used by UpdatePlowPreview — no deduplication, fresh each frame
    private Vector3 GetPreviewTargetTile()
    {
        if (Camera.main == null) return Vector3.zero;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
        return CropTileSelector.GetDirectionalTile(
            playerTransform.position, mouseWorldPos, plowingRange, ref dummy);
    }

    /// <summary>
    /// Called when plowing is successful
    /// </summary>
    public void OnPlowSuccess(Vector3Int tilePosition, Vector3 worldPosition)
    {
        Debug.Log($"Successfully plowed tile at {tilePosition}");
    }

    
    /// <summary>
    /// Called when plowing fails
    /// </summary>
    public void OnPlowFailed(Vector3Int tilePosition)
    {
        Debug.Log($"Failed to plow tile at {tilePosition}");
        // You can add feedback here
        // PlayErrorSound();
    }
    
    private void ValidateReferences()
    {
        if (tilledTile == null)
            Debug.LogError("TilledTile is not assigned in CropPlowingView!");
    }

    private void OnDestroy()
    {
        UseToolService.OnHoeRequested -= HandleHoeUseRequested;
    }

    // ── Hoe-use event handler ──────────────────────────────────────────────

    /// <summary>
    /// Received from UseToolService.OnHoeRequested.
    /// Plows the directional tile at the given mouse world position.
    /// </summary>
    private void HandleHoeUseRequested(ToolData tool, Vector3 mouseWorldPos)
    {
        if (presenter == null || playerTransform == null) return;

        _lastMouseWorldPos = mouseWorldPos;  // store before snap

        // Reset the mouse-hold timer so HandleMouseHoldPlow does NOT fire again
        // in the same frame — preventing the double-action that was cancelling itself out.
        _mouseHoldTimer = mouseHoldRepeatInterval;

        Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
        Vector3 snappedTile = CropTileSelector.GetDirectionalTile(
            playerTransform.position, mouseWorldPos, plowingRange, ref dummy);

        if (snappedTile == Vector3.zero) return;

        presenter.HandlePlowAction(snappedTile);
    }

    
    private void OnDrawGizmosSelected()
    {
        // Try to get player transform if not cached
        Transform targetTransform = playerTransform;
        
        if (targetTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                // Try to find CenterPoint child first
                Transform centerPoint = playerEntity.transform.Find("CenterPoint");
                targetTransform = centerPoint != null ? centerPoint : playerEntity.transform;
            }
        }
        
        // Draw the plowing range gizmo if we have a target transform
        if (targetTransform != null)
        {
            // Set gizmo color (green with transparency)
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            
            // Draw a wire sphere to show the plowing range
            Gizmos.DrawWireSphere(targetTransform.position, plowingRange);
            
            // Draw a solid disc for better visibility (optional)
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            DrawDiscGizmo(targetTransform.position, plowingRange);
            
            // Draw grid overlay to show tile boundaries
            DrawTileGrid(targetTransform.position, plowingRange);
        }
    }
    
    private void DrawDiscGizmo(Vector3 center, float radius)
    {
        // Draw a disc on the XY plane
        const int segments = 32;
        float angleStep = 360f / segments;
        
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    private void DrawTileGrid(Vector3 center, float radius)
    {
        // Draw a grid showing tile boundaries within range
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        
        int centerX = Mathf.RoundToInt(center.x);
        int centerY = Mathf.RoundToInt(center.y);
        int tileRadius = Mathf.CeilToInt(radius);
        
        // Draw vertical lines
        for (int x = centerX - tileRadius; x <= centerX + tileRadius + 1; x++)
        {
            float xPos = x - 0.5f;
            Gizmos.DrawLine(
                new Vector3(xPos, center.y - radius, 0),
                new Vector3(xPos, center.y + radius, 0)
            );
        }
        
        // Draw horizontal lines
        for (int y = centerY - tileRadius; y <= centerY + tileRadius + 1; y++)
        {
            float yPos = y - 0.5f;
            Gizmos.DrawLine(
                new Vector3(center.x - radius, yPos, 0),
                new Vector3(center.x + radius, yPos, 0)
            );
        }
    }
}