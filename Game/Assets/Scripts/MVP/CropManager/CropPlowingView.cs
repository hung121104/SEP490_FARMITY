using UnityEngine;
using UnityEngine.Tilemaps;

public class CropPlowingView : MonoBehaviour
{
    [Header("Tile Reference")]
    [SerializeField] private TileBase tilledTile;
    
    [Header("Input Settings")]
    [SerializeField] private KeyCode plowingKey = KeyCode.E;
    
    [Header("Player Settings")]
    [Tooltip("Tag to find the player GameObject")]
    public string playerTag = "PlayerEntity";
    
    [Header("Plowing Settings")]
    [SerializeField] private float plowingRange = 2f;
    
    private CropPlowingPresenter presenter;
    private Transform playerTransform;
    
    private void Start()
    {
        // Initialize the MVP pattern
        ICropPlowingService service = new CropPlowingService();
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
        if (Input.GetKeyDown(plowingKey))
        {
            HandlePlowInput();
        }
    }
    
    
    private void HandlePlowInput()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Player transform is not assigned!");
            return;
        }
        
        // Get the mouse position in world space
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // Check if the position is within plowing range
        float distance = Vector3.Distance(playerTransform.position, mouseWorldPos);
        
        if (distance <= plowingRange)
        {
            // Tell the presenter to handle the plow action
            presenter.HandlePlowAction(mouseWorldPos);
        }
        else
        {
            Debug.Log("Target position is too far away!");
        }
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
    
    [ContextMenu("Test Spawn Tilled Tile")]
    public void TestSpawnTilledTile()
    {
        if (Camera.main == null)
        {
            Debug.LogError("Main Camera not found. Cannot determine mouse position.");
            return;
        }

        // Get the mouse position in world space
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Attempt to plow the tile at the mouse position
        if (presenter != null)
        {
            presenter.HandlePlowAction(mouseWorldPos);
            Debug.Log($"Attempting to spawn a tilled tile at mouse position {mouseWorldPos}.");
        }
        else
        {
            Debug.LogError("Test failed: Presenter is not initialized.");
        }
    }
    
    private void ValidateReferences()
    {
        if (tilledTile == null)
        {
            Debug.LogError("TilledTile is not assigned in CropPlowingView!");
        }
    }
}