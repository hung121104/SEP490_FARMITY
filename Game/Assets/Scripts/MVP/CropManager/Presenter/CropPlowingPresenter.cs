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
        // Hoe on a tile that has a crop → remove the crop, keep the tilled tile
        if (WorldDataManager.Instance != null &&
            WorldDataManager.Instance.HasCropAtWorldPosition(worldPosition))
        {
            bool removed = cropPlowingService.RemoveCropOnTile(worldPosition);
            if (removed)
                view.OnPlowSuccess(Vector3Int.zero, worldPosition);
            return;
        }

        // Hoe on an already-tilled tile (no crop) → untill it
        if (WorldDataManager.Instance != null &&
            WorldDataManager.Instance.IsTilledAtWorldPosition(worldPosition))
        {
            bool untilled = cropPlowingService.UntillTile(worldPosition);
            if (untilled)
                view.OnPlowSuccess(Vector3Int.zero, worldPosition);
            else
                view.OnPlowFailed(Vector3Int.zero);
            return;
        }

        // Untilled farmable tile → till it
        bool success = cropPlowingService.PlowTile(Vector3Int.zero, worldPosition);

        if (success)
            view.OnPlowSuccess(Vector3Int.zero, worldPosition);
        else
            view.OnPlowFailed(Vector3Int.zero);
    }

    
    /// <summary>
    /// Initializes the presenter and service with required references
    /// </summary>
    public void Initialize(TileBase tilledTile)
    {
        cropPlowingService.Initialize(tilledTile);
    }
}