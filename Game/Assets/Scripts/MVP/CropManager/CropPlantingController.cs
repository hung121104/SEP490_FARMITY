using UnityEngine;
using Photon.Pun;

public class CropPlantingController : MonoBehaviourPunCallbacks
{
    [Header("Planting Settings")]
    [Tooltip("Crop type ID to plant (1=Wheat, 2=Corn, etc.)")]
    public int currentCropTypeID = 1; // Changed from ushort to int
    
    [Header("Input")]
    public KeyCode plantKey = KeyCode.E;
    public bool allowHoldToPlant = true;
    [Tooltip("How often (seconds) to attempt planting while holding the plant key.")]
    public float plantRepeatInterval = 0.25f;

    [Header("Visual Feedback")]
    public GameObject cropPrefab;

    [Header("Debug")]
    public bool showDebugLogs = true;


    private ChunkDataSyncManager syncManager;
    private ChunkLoadingManager loadingManager;

    // Hold state
    private float holdTimer = 0f;
    private Vector2Int lastTriedTile = new Vector2Int(int.MinValue, int.MinValue);

    private void Awake()
    {
        syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        loadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        if (syncManager == null)
        {
            Debug.LogWarning("[CropPlanting] ChunkDataSyncManager not found in scene!");
        }
        if (loadingManager == null)
        {
            Debug.LogWarning("[CropPlanting] ChunkLoadingManager not found in scene!");
        }
    }

    private void Update()
    {
        if (allowHoldToPlant)
        {
            if (Input.GetKeyDown(plantKey))
            {
                // immediate plant on key down
                PlantCropAtMousePosition();
                holdTimer = plantRepeatInterval;
            }

            if (Input.GetKey(plantKey))
            {
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f)
                {
                    PlantCropAtMousePosition();
                    holdTimer = plantRepeatInterval;
                }
            }

            if (Input.GetKeyUp(plantKey))
            {
                // reset timer so next press plants immediately
                holdTimer = 0f;
                lastTriedTile = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
        else
        {
            if (Input.GetKeyDown(plantKey))
            {
                PlantCropAtMousePosition();
            }
        }
    }

    private void PlantCropAtMousePosition()
    {
        // Get mouse position and SET Z BEFORE conversion
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Camera.main.transform.position.z * -1; // Use camera distance from world

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0; // Keep this for safety

        int tileX = Mathf.RoundToInt(mouseWorldPos.x);
        int tileY = Mathf.RoundToInt(mouseWorldPos.y);
        Vector3 tilePosition = new Vector3(tileX, tileY, 0);
        Vector2Int tileCoords = new Vector2Int(tileX, tileY);

        // If we're holding the key, avoid repeating the same tile check/logs
        if (tileCoords == lastTriedTile)
        {
            return;
        }

        // Debug to verify correct position
        if (showDebugLogs)
        {
            Debug.Log($"Mouse Screen: {Input.mousePosition}, World: ({mouseWorldPos.x:F1}, {mouseWorldPos.y:F1}), Tile: ({tileX}, {tileY})");
        }

        // Early checks for out-of-bounds / existing crop
        if (!WorldDataManager.Instance.IsPositionInActiveSection(tilePosition))
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"Cannot plant at ({tileX}, {tileY}): position not in any section");
            }
            lastTriedTile = tileCoords;
            return;
        }

        if (WorldDataManager.Instance.HasCropAtWorldPosition(tilePosition))
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"Crop already exists at ({tileX}, {tileY})");
            }
            lastTriedTile = tileCoords;
            return;
        }

        PlantCrop(tilePosition);
    }


    private void PlantCrop(Vector3 worldPosition)
    {
        // Convert to int once at the beginning
        int worldX = Mathf.FloorToInt(worldPosition.x);
        int worldY = Mathf.FloorToInt(worldPosition.y);

        // Convert int to ushort for WorldDataManager
        bool success = WorldDataManager.Instance.PlantCropAtWorldPosition(worldPosition, (ushort)currentCropTypeID);

        // track last tried/planted tile to prevent repeated logging while holding
        lastTriedTile = new Vector2Int(worldX, worldY);

        if (success)
        {
            if (showDebugLogs)
            {
                Debug.Log($"✓ Planted crop type {currentCropTypeID} at ({worldX}, {worldY})");
            }

            // Refresh chunk visuals instead
            if (loadingManager != null)
            {
                Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPosition);
                loadingManager.RefreshChunkVisuals(chunkPos);
            }

            // Sync to other players via Photon
            if (PhotonNetwork.IsConnected && photonView != null && syncManager != null)
            {
                syncManager.BroadcastCropPlanted(worldX, worldY, currentCropTypeID);
            }
        }
    }

    /// <summary>
    /// Photon RPC: Sync crop planting to other clients
    /// </summary>
    [PunRPC]
    private void RPC_PlantCrop(Vector3 worldPosition, int cropTypeID) // Changed from ushort to int
    {
        // Convert int back to ushort for WorldDataManager
        WorldDataManager.Instance.PlantCropAtWorldPosition(worldPosition, (ushort)cropTypeID);

        if (showDebugLogs)
        {
            Debug.Log($"[Network] Received planted crop type {cropTypeID} at ({worldPosition.x:F0}, {worldPosition.y:F0})");
        }
    }

}
