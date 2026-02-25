using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

/// <summary>
/// View component for crop planting following MVP pattern.
/// Designed to be placed on a standalone GameObject (CropPlantingManager) in the scene.
/// Responsible for: handling user input, calculating planting positions, and visual feedback.
/// Follows Single Responsibility Principle and Dependency Inversion Principle.
/// </summary>
public class CropPlantingView : MonoBehaviourPunCallbacks
{
    public static CropPlantingView Instance { get; private set; }

    [Header("Planting Settings")]
    [Tooltip("Seed ScriptableObject. The PlantId from SeedDataSO.CropDataSo is used to resolve the crop type index when planting.")]
    public SeedDataSO seedDataSO;

    /// <summary>Returns the PlantId string from the assigned SeedDataSO, or empty if none is set.</summary>
    public string CurrentPlantId => (seedDataSO != null && seedDataSO.CropDataSo != null)
        ? seedDataSO.CropDataSo.PlantId
        : string.Empty;
    [Tooltip("Planting mode: at mouse, around player (1 tile radius), or far around player (2 tile radius)")]
    public PlantingMode plantingMode = PlantingMode.AroundPlayer;
    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";
    [Tooltip("Maximum distance from player to plant crops")]
    [SerializeField] private float plantingRange = 2f;
    
    [Header("Input")]
    public KeyCode plantKey = KeyCode.E;
    public bool allowHoldToPlant = true;
    [Tooltip("How often (seconds) to attempt planting while holding the plant key.")]
    public float plantRepeatInterval = 0.25f;
    [Tooltip("Tag to search for player camera if targetCamera is null")]
    public string playerCameraTag = "MainCamera";

    [Header("Tile Preview")]
    [Range(0f, 1f)] public float previewAlpha = 0.5f;
    public string previewSortingLayer = "WalkInfront";
    public int    previewSortingOrder = 10;

    [Header("Debug")]
    public bool showDebugLogs = true;

    //player Camera
    private Camera targetCamera;
    private Transform playerTransform;
    private Collider2D playerCollider;
    
    // Preview
    private SpriteRenderer _previewSR;

    // MVP Components
    private CropPlantingPresenter presenter;
    private ICropPlantingService cropPlantingService;

    // Hold state
    private float holdTimer = 0f;
    private Vector2Int lastTriedArea = new Vector2Int(int.MinValue, int.MinValue); // Last tile attempted

    private void Awake()
    {
        // Singleton pattern for easy access from anywhere in the scene
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[CropPlantingView] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Set up camera reference
        if (targetCamera == null)
        {
            targetCamera = FindPlayerCamera();
        }

        // Build inline preview sprite renderer
        var previewGO = new GameObject("PlantingPreview");
        previewGO.transform.SetParent(transform, false);
        _previewSR                  = previewGO.AddComponent<SpriteRenderer>();
        _previewSR.color            = new Color(1f, 1f, 1f, previewAlpha);
        _previewSR.sortingLayerName = previewSortingLayer;
        _previewSR.sortingOrder     = previewSortingOrder;
        _previewSR.enabled          = false;

        InitializeMVP();
    }

    /// <summary>
    /// Finds the player camera by tag or uses Camera.main as fallback.
    /// </summary>
    private Camera FindPlayerCamera()
    {
        // Try to find by tag
        GameObject cameraObject = GameObject.FindGameObjectWithTag(playerCameraTag);
        if (cameraObject != null)
        {
            Camera cam = cameraObject.GetComponent<Camera>();
            if (cam != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[CropPlantingView] Found camera with tag '{playerCameraTag}': {cam.name}");
                }
                return cam;
            }
        }

        // Fallback to Camera.main
        if (Camera.main != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[CropPlantingView] Using Camera.main: {Camera.main.name}");
            }
            return Camera.main;
        }

        // Last resort: find any camera in scene
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (cameras.Length > 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[CropPlantingView] Using first found camera: {cameras[0].name}");
            }
            return cameras[0];
        }

        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Initializes the MVP components (Service and Presenter).
    /// Follows Dependency Injection principle.
    /// </summary>
    private void InitializeMVP()
    {
        // Find required managers
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        ChunkLoadingManager loadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        if (syncManager == null)
        {
            Debug.LogWarning("[CropPlantingView] ChunkDataSyncManager not found in scene!");
        }
        if (loadingManager == null)
        {
            Debug.LogWarning("[CropPlantingView] ChunkLoadingManager not found in scene!");
        }

        // Create service (business logic layer)
        cropPlantingService = new CropPlantingService(syncManager, loadingManager, showDebugLogs);

        // Create presenter (coordination layer)
        presenter = new CropPlantingPresenter(cropPlantingService, showDebugLogs);
    }

    private void Update()
    {
        // Re-check camera if it becomes null (e.g., player respawn)
        if (targetCamera == null)
        {
            targetCamera = FindPlayerCamera();
        }

        // Re-check player if it becomes null
        if (playerTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                // Try to find CenterPoint child first
                Transform centerPoint = playerEntity.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : playerEntity.transform;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[CropPlantingView] Using transform: {playerTransform.name}");
                }
            }
        }

        HandleInput();
        UpdatePlantingPreview();
    }

    /// <summary>
    /// Allows external scripts to trigger crop planting programmatically.
    /// Uses the PlantId string to resolve the crop index in the plant database.
    /// </summary>
    public void PlantCropAtPosition(Vector3 screenPosition, string plantId)
    {
        if (presenter == null || targetCamera == null) return;
        if (string.IsNullOrEmpty(plantId)) return;
        List<Vector3> positions = new List<Vector3> { ScreenToWorldPosition(screenPosition) };
        presenter.HandlePlantCrops(positions, plantId);
    }

    /// <summary>
    /// Handles user input for planting crops.
    /// View responsibility - captures input, calculates positions, forwards to presenter.
    /// </summary>
    private void HandleInput()
    {
        if (presenter == null)
        {
            Debug.LogError("[CropPlantingView] Presenter is null!");
            return;
        }

        if (allowHoldToPlant)
        {
            if (Input.GetKeyDown(plantKey))
            {
                // Immediate plant on key down
                TriggerPlanting();
                holdTimer = plantRepeatInterval;
            }

            if (Input.GetKey(plantKey))
            {
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f)
                {
                    TriggerPlanting();
                    holdTimer = plantRepeatInterval;
                }
            }

            if (Input.GetKeyUp(plantKey))
            {
                // Reset timer and area tracking
                holdTimer = 0f;
                lastTriedArea = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
        else
        {
            if (Input.GetKeyDown(plantKey))
            {
                TriggerPlanting();
            }
        }
    }

    /// <summary>
    /// Triggers planting by calculating positions and sending to presenter.
    /// Derives the crop PlantId from SeedDataSO.CropDataSo.PlantId and passes it as a string.
    /// </summary>
    private void TriggerPlanting()
    {
        string plantId = CurrentPlantId;
        if (string.IsNullOrEmpty(plantId))
        {
            if (showDebugLogs)
                Debug.LogWarning("[CropPlantingView] No PlantId available. Assign a SeedDataSO with a valid CropDataSo.");
            return;
        }

        List<Vector3> positions = CalculatePlantingPositions();
        if (positions.Count > 0)
        {
            presenter.HandlePlantCrops(positions, plantId);
        }
    }

    // ── Preview ──────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the tile preview every frame based on current camera / player / mouse state.
    /// Called at the end of Update().
    /// </summary>
    private void UpdatePlantingPreview()
    {
        if (_previewSR == null) return;

        // Get the stage-0 sprite from the current seed
        Sprite seedSprite = (seedDataSO?.CropDataSo?.GrowthStages?.Count > 0)
            ? seedDataSO.CropDataSo.GrowthStages[0].stageSprite
            : null;

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

    /// <summary>
    /// Calculates the target tile for preview WITHOUT deduplication, so it refreshes every frame.
    /// </summary>
    private Vector3 GetPreviewTargetTile()
    {
        Vector3 playerPos      = playerTransform.position;
        Vector3 mouseWorldPos  = ScreenToWorldPosition(Input.mousePosition);
        mouseWorldPos.z        = 0f;

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

    /// <summary>
    /// Calculates planting positions based on current planting mode.
    /// View responsibility - determines WHERE to plant based on input.
    /// </summary>
    private List<Vector3> CalculatePlantingPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        switch (plantingMode)
        {
            case PlantingMode.AtMouse:
                positions.Add(GetMouseWorldPosition());
                break;

            case PlantingMode.AroundPlayer:
                Vector3 pos = GetDirectionalTileAroundPlayer(1);
                if (pos != Vector3.zero) positions.Add(pos);
                break;

            case PlantingMode.FarAroundPlayer:
                Vector3 farPos = GetDirectionalTileAroundPlayer(2);
                if (farPos != Vector3.zero) positions.Add(farPos);
                break;
        }

        return positions;
    }

    /// <summary>
    /// Gets world position at mouse cursor.
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        if (targetCamera == null) return Vector3.zero;
        
        Vector3 worldPos = ScreenToWorldPosition(Input.mousePosition);
        int tileX = Mathf.RoundToInt(worldPos.x);
        int tileY = Mathf.RoundToInt(worldPos.y);
        
        return new Vector3(tileX, tileY, 0);
    }

    /// <summary>
    /// Gets the tile in the direction of the mouse, within radius around player.
    /// Delegates to the shared <see cref="CropTileSelector"/> utility.
    /// </summary>
    private Vector3 GetDirectionalTileAroundPlayer(int maxRadius)
    {
        if (targetCamera == null || playerTransform == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[CropPlantingView] Camera or Player not found. Cannot calculate tile.");
            return Vector3.zero;
        }

        Vector3 playerPos = playerTransform.position;
        Vector3 mouseWorldPos = ScreenToWorldPosition(Input.mousePosition);

        Vector3 result = CropTileSelector.GetDirectionalTile(
            playerPos,
            mouseWorldPos,
            plantingRange,
            ref lastTriedArea,
            maxRadius);

        if (showDebugLogs && result != Vector3.zero)
            Debug.Log($"[CropPlantingView] Planting at tile ({result.x}, {result.y})");

        return result;
    }

    /// <summary>
    /// Converts screen position to world position.
    /// </summary>
    private Vector3 ScreenToWorldPosition(Vector3 screenPosition)
    {
        if (targetCamera == null) return Vector3.zero;

        Vector3 mouseScreenPos = screenPosition;
        mouseScreenPos.z = targetCamera.transform.position.z * -1;

        Vector3 mouseWorldPos = targetCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;

        return mouseWorldPos;
    }

    /// <summary>
    /// Photon RPC: Sync crop planting to other clients.
    /// Receives network events and forwards to presenter.
    /// </summary>
    [PunRPC]
    private void RPC_PlantCrop(Vector3 worldPosition, string plantId)
    {
        if (presenter != null)
        {
            presenter.HandleNetworkCropPlanted(worldPosition, plantId);
        }
        else
        {
            Debug.LogError("[CropPlantingView] Cannot handle network crop planted: Presenter is null!");
        }
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
        
        // Draw the planting range gizmo based on planting mode
        if (targetTransform != null)
        {
            Color gizmoColor = Color.green;
            
            switch (plantingMode)
            {
                case PlantingMode.AroundPlayer:
                case PlantingMode.FarAroundPlayer:
                    gizmoColor = new Color(0f, 1f, 0f, 0.3f); // Green
                    break;
                    
                case PlantingMode.AtMouse:
                    // No range limit for AtMouse mode, draw a small indicator
                    Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow
                    Gizmos.DrawWireSphere(targetTransform.position, 0.3f);
                    return;
            }
            
            if (plantingRange > 0)
            {
                // Draw wire sphere to show the planting range
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(targetTransform.position, plantingRange);
                
                // Draw a solid disc for better visibility
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.1f);
                DrawDiscGizmo(targetTransform.position, plantingRange);
                
                // Draw grid overlay to show tile boundaries
                DrawTileGrid(targetTransform.position, plantingRange);
            }
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
