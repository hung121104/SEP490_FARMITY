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
        // If there's a crop on this tile, remove it instead of plowing
        if (WorldDataManager.Instance != null &&
            WorldDataManager.Instance.HasCropAtWorldPosition(worldPosition))
        {
            cropPlowingService.RemoveCropOnTile(worldPosition);
            return;
        }

        // No crop — proceed with normal plow
        Tilemap anyTilemap = Object.FindAnyObjectByType<Tilemap>();
        if (anyTilemap == null)
        {
            Debug.LogError("No tilemap found in scene!");
            return;
        }

        Vector3Int tilePosition = anyTilemap.WorldToCell(worldPosition);

        bool success = cropPlowingService.PlowTile(tilePosition, worldPosition);

        if (success)
            view.OnPlowSuccess(tilePosition);
        else
            view.OnPlowFailed(tilePosition);
    }

    
    /// <summary>
    /// Initializes the presenter and service with required references
    /// </summary>
    public void Initialize(TileBase tilledTile)
    {
        cropPlowingService.Initialize(tilledTile);
    }
}