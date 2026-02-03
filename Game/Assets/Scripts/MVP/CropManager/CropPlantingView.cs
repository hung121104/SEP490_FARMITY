using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public enum PlantingMode
{
    AtMouse,           // Plant at exact mouse position
    AroundPlayer,      // Plant in direction of mouse within radius around player (3x3 = 1 tile away)
    FarAroundPlayer    // Plant in direction of mouse within larger radius (5x5 = 2 tiles away)
}

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
    [Tooltip("Crop type ID to plant (1=Wheat, 2=Corn, etc.)")]
    public int currentCropTypeID = 1;
    [Tooltip("Planting mode: at mouse, around player (1 tile radius), or far around player (2 tile radius)")]
    public PlantingMode plantingMode = PlantingMode.AroundPlayer;
    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";
    
    [Header("Input")]
    public KeyCode plantKey = KeyCode.E;
    public bool allowHoldToPlant = true;
    [Tooltip("How often (seconds) to attempt planting while holding the plant key.")]
    public float plantRepeatInterval = 0.25f;
    [Tooltip("Tag to search for player camera if targetCamera is null")]
    public string playerCameraTag = "MainCamera";

    [Header("Visual Feedback")]
    public GameObject cropPrefab;

    [Header("Debug")]
    public bool showDebugLogs = true;

    //player Camera
    private Camera targetCamera;
    private Transform playerTransform;
    private Collider2D playerCollider;
    
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
        Camera[] cameras = FindObjectsOfType<Camera>();
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
    }

    /// <summary>
    /// Allows external scripts to trigger crop planting programmatically.
    /// </summary>
    public void PlantCropAtPosition(Vector3 screenPosition, int cropTypeID)
    {
        if (presenter != null && targetCamera != null)
        {
            List<Vector3> positions = new List<Vector3> { ScreenToWorldPosition(screenPosition) };
            presenter.HandlePlantCrops(positions, cropTypeID);
        }
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
    /// </summary>
    private void TriggerPlanting()
    {
        List<Vector3> positions = CalculatePlantingPositions();
        if (positions.Count > 0)
        {
            presenter.HandlePlantCrops(positions, currentCropTypeID);
        }
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
    /// Returns the specific tile based on 8-directional input (or player's own tile).
    /// </summary>
    private Vector3 GetDirectionalTileAroundPlayer(int maxRadius)
    {
        if (targetCamera == null || playerTransform == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[CropPlantingView] Camera or Player not found. Cannot calculate tile.");
            }
            return Vector3.zero;
        }

        Vector3 playerPos = playerTransform.position;
        Vector3 mouseWorldPos = ScreenToWorldPosition(Input.mousePosition);

        // Get player tile position
        int playerTileX = Mathf.RoundToInt(playerPos.x);
        int playerTileY = Mathf.RoundToInt(playerPos.y);

        // Calculate direction from player to mouse
        Vector2 direction = new Vector2(mouseWorldPos.x - playerPos.x, mouseWorldPos.y - playerPos.y);
        float distance = direction.magnitude;

        // If mouse is very close to player (within 0.5 units), plant at player's tile
        if (distance < 0.5f)
        {
            Vector2Int playerTileCoords = new Vector2Int(playerTileX, playerTileY);
            
            if (playerTileCoords == lastTriedArea)
            {
                return Vector3.zero;
            }
            
            lastTriedArea = playerTileCoords;
            
            if (showDebugLogs)
            {
                Debug.Log($"[CropPlantingView] Planting at player tile ({playerTileX}, {playerTileY})");
            }
            
            return new Vector3(playerTileX, playerTileY, 0);
        }

        // Normalize direction
        direction.Normalize();

        // Determine offset based on 8-directional input
        int offsetX = 0;
        int offsetY = 0;

        // Determine horizontal component
        if (direction.x > 0.4f) offsetX = 1;
        else if (direction.x < -0.4f) offsetX = -1;

        // Determine vertical component
        if (direction.y > 0.4f) offsetY = 1;
        else if (direction.y < -0.4f) offsetY = -1;

        // Clamp to max radius
        offsetX = Mathf.Clamp(offsetX, -maxRadius, maxRadius);
        offsetY = Mathf.Clamp(offsetY, -maxRadius, maxRadius);

        int targetX = playerTileX + offsetX;
        int targetY = playerTileY + offsetY;
        Vector2Int targetTile = new Vector2Int(targetX, targetY);

        // Prevent replanting same tile
        if (targetTile == lastTriedArea)
        {
            return Vector3.zero;
        }

        lastTriedArea = targetTile;

        Vector3 plantPosition = new Vector3(targetX, targetY, 0);

        if (showDebugLogs)
        {
            string directionName = GetDirectionName(offsetX, offsetY);
            Debug.Log($"[CropPlantingView] Planting {directionName} of player at ({targetX}, {targetY})");
        }

        return plantPosition;
    }

    /// <summary>
    /// Gets a human-readable direction name for debugging.
    /// </summary>
    private string GetDirectionName(int offsetX, int offsetY)
    {
        if (offsetX == 0 && offsetY == 1) return "above";
        if (offsetX == 0 && offsetY == -1) return "below";
        if (offsetX == 1 && offsetY == 0) return "right";
        if (offsetX == -1 && offsetY == 0) return "left";
        if (offsetX == 1 && offsetY == 1) return "top-right";
        if (offsetX == -1 && offsetY == 1) return "top-left";
        if (offsetX == 1 && offsetY == -1) return "bottom-right";
        if (offsetX == -1 && offsetY == -1) return "bottom-left";
        return "at player";
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
    private void RPC_PlantCrop(Vector3 worldPosition, int cropTypeID)
    {
        if (presenter != null)
        {
            presenter.HandleNetworkCropPlanted(worldPosition, cropTypeID);
        }
        else
        {
            Debug.LogError("[CropPlantingView] Cannot handle network crop planted: Presenter is null!");
        }
    }
}
