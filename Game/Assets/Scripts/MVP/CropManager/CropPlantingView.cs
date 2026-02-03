using UnityEngine;
using Photon.Pun;

/// <summary>
/// View component for crop planting following MVP pattern.
/// Designed to be placed on a standalone GameObject (CropPlantingManager) in the scene.
/// Responsible only for handling user input and displaying visual feedback.
/// Follows Single Responsibility Principle and Dependency Inversion Principle.
/// </summary>
public class CropPlantingView : MonoBehaviourPunCallbacks
{
    public static CropPlantingView Instance { get; private set; }

    [Header("Planting Settings")]
    [Tooltip("Crop type ID to plant (1=Wheat, 2=Corn, etc.)")]
    public int currentCropTypeID = 1;
    
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
    // MVP Components
    private CropPlantingPresenter presenter;
    private ICropPlantingService cropPlantingService;

    // Hold state
    private float holdTimer = 0f;

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

        // Create presenter (presentation logic layer)
        presenter = new CropPlantingPresenter(cropPlantingService, targetCamera, showDebugLogs);
    }

    private void Update()
    {
        // Re-check camera if it becomes null (e.g., player respawn)
        if (targetCamera == null)
        {
            targetCamera = FindPlayerCamera();
            if (targetCamera != null && presenter != null)
            {
                // Recreate presenter with new camera
                ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
                ChunkLoadingManager loadingManager = FindAnyObjectByType<ChunkLoadingManager>();
                presenter = new CropPlantingPresenter(cropPlantingService, targetCamera, showDebugLogs);
            }
        }

        HandleInput();
    }

    /// <summary>
    /// Allows external scripts to trigger crop planting programmatically.
    /// </summary>
    public void PlantCropAtPosition(Vector3 screenPosition, int cropTypeID)
    {
        if (presenter != null)
        {
            presenter.HandlePlantAtMousePosition(screenPosition, cropTypeID);
        }
    }

    /// <summary>
    /// Handles user input for planting crops.
    /// View responsibility - captures and forwards user input to presenter.
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
                presenter.HandlePlantAtMousePosition(Input.mousePosition, currentCropTypeID);
                holdTimer = plantRepeatInterval;
            }

            if (Input.GetKey(plantKey))
            {
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f)
                {
                    presenter.HandlePlantAtMousePosition(Input.mousePosition, currentCropTypeID);
                    holdTimer = plantRepeatInterval;
                }
            }

            if (Input.GetKeyUp(plantKey))
            {
                // Reset timer so next press plants immediately
                holdTimer = 0f;
                presenter.ResetLastTriedTile();
            }
        }
        else
        {
            if (Input.GetKeyDown(plantKey))
            {
                presenter.HandlePlantAtMousePosition(Input.mousePosition, currentCropTypeID);
            }
        }
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
