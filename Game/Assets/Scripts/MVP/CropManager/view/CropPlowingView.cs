using UnityEngine;
using UnityEngine.Tilemaps;

public class CropPlowingView : MonoBehaviour
{
    [Header("Tile Reference")]
    [SerializeField] private TileBase tilledTile;
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode plowingKey = KeyCode.E;
    [SerializeField] private bool allowHoldToPlow = true;
    [Tooltip("How often (seconds) to attempt plowing while holding the plow key.")]
    [SerializeField] private float plowRepeatInterval = 0.1f;
    
    [Header("Player Settings")]
    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";
    
    [Header("Plowing Settings")]
    [SerializeField] private float plowingRange = 2f;
    [SerializeField] private bool showDebugLogs = false;
    
    private CropPlowingPresenter presenter;
    private Transform playerTransform;
    private Vector2Int lastPlowedTile = new Vector2Int(int.MinValue, int.MinValue);
    private float holdTimer = 0f;
    
    private void Start()
    {
        // Initialize the MVP pattern
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        ICropPlowingService service = new CropPlowingService(syncManager, showDebugLogs);
        presenter = new CropPlowingPresenter(this, service);
        
        // Initialize the presenter with tilled tile reference
        presenter.Initialize(tilledTile);
        
        // Validate references
        ValidateReferences();
    }
    
    private void Update()
    {
        // Re-check player if it becomes null
        if (playerTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                // Try to find CenterPoint child first
                Transform centerPoint = playerEntity.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : playerEntity.transform;
            }
        }
        
        // Check for plowing input
        if (allowHoldToPlow)
        {
            if (Input.GetKeyDown(plowingKey))
            {
                // Immediate plow on key down
                HandlePlowInput();
                holdTimer = plowRepeatInterval;
            }
            
            if (Input.GetKey(plowingKey))
            {
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f)
                {
                    HandlePlowInput();
                    holdTimer = plowRepeatInterval;
                }
            }
            
            if (Input.GetKeyUp(plowingKey))
            {
                // Reset timer and tile tracking
                holdTimer = 0f;
                lastPlowedTile = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
        else
        {
            if (Input.GetKeyDown(plowingKey))
            {
                HandlePlowInput();
            }
            
            // Reset last plowed tile when key is released
            if (Input.GetKeyUp(plowingKey))
            {
                lastPlowedTile = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
    }
    
    
    private void HandlePlowInput()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform is not assigned!");
            return;
        }
        
        // Get directional tile position
        Vector3 targetTilePos = GetDirectionalTileForPlowing();
        
        if (targetTilePos != Vector3.zero)
        {
            // Tell the presenter to handle the plow action
            presenter.HandlePlowAction(targetTilePos);
        }
    }
    
    /// <summary>
    /// Gets the tile in the direction of the mouse, within range of player.
    /// Delegates to the shared <see cref="CropTileSelector"/> utility.
    /// </summary>
    private Vector3 GetDirectionalTileForPlowing()
    {
        if (Camera.main == null || playerTransform == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[CropPlowingView] Camera or Player not found. Cannot calculate tile.");
            return Vector3.zero;
        }

        Vector3 playerPos = playerTransform.position;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3 result = CropTileSelector.GetDirectionalTile(
            playerPos,
            mouseWorldPos,
            plowingRange,
            ref lastPlowedTile);

        if (showDebugLogs && result != Vector3.zero)
            Debug.Log($"[CropPlowingView] Plowing at tile ({result.x}, {result.y})");

        return result;
    }
    
    /// <summary>
    /// Called when plowing is successful
    /// </summary>
    public void OnPlowSuccess(Vector3Int tilePosition)
    {
        Debug.Log($"Successfully plowed tile at {tilePosition}");
        // You can add visual/audio feedback here
        // PlayPlowSound();
        // SpawnPlowParticles(tilePosition);
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
        {
            Debug.LogError("TilledTile is not assigned in CropPlowingView!");
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