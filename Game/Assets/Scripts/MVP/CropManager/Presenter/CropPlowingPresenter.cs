using UnityEngine;
using UnityEngine.Tilemaps;

public class CropPlowingPresenter
{
    private ICropPlowingService cropPlowingService;
    private CropPlowingView view;
    
    public CropPlowingPresenter(CropPlowingView view, ICropPlowingService service)
    {
        this.view = view;
        this.cropPlowingService = service;
    }
    
    /// <summary>
    /// Handles the plowing action when player presses the plowing key
    /// </summary>
    /// <param name="worldPosition">The world position where the player wants to plow</param>
    public void HandlePlowAction(Vector3 worldPosition)
    {
        // Convert world position to tile position (using any tilemap for conversion)
        Tilemap anyTilemap = Object.FindAnyObjectByType<Tilemap>();
        if (anyTilemap == null)
        {
            Debug.LogError("No tilemap found in scene!");
            return;
        }
        
        Vector3Int tilePosition = anyTilemap.WorldToCell(worldPosition);
        
        // Attempt to plow the tile
        bool success = cropPlowingService.PlowTile(tilePosition, worldPosition);
        
        // Update the view based on the result
        if (success)
        {
            view.OnPlowSuccess(tilePosition);
        }
        else
        {
            view.OnPlowFailed(tilePosition);
        }
    }
    
    /// <summary>
    /// Initializes the presenter and service with required references
    /// </summary>
    public void Initialize(TileBase tilledTile)
    {
        cropPlowingService.Initialize(tilledTile);
    }
}