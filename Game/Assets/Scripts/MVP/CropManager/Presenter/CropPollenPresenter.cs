using UnityEngine;

/// <summary>
/// Orchestrates pollen collection. Receives commands from the View and fires
/// success/failure callbacks back to it.
/// </summary>
public class CropPollenPresenter
{
    private readonly ICropPollenService service;
    private readonly CropPollenHarvestingView view;

    public CropPollenPresenter(CropPollenHarvestingView view, ICropPollenService service)
    {
        this.view    = view;
        this.service = service;
    }

    // ── Called by the View ────────────────────────────────────────────────

    public void HandleCollectPollen(Vector3 worldPos)
    {
        if (!service.CanCollectPollen(worldPos))
        {
            view.OnPollenCollectFailed(worldPos);
            return;
        }

        PollenData pollen = service.TryCollectPollen(worldPos);

        if (pollen != null)
            view.OnPollenCollected(worldPos, pollen);
        else
            view.OnPollenCollectFailed(worldPos);
    }

    public Vector3 FindNearbyPollenTile(Vector3 playerPos, float radius)
        => service.FindNearbyPollenTile(playerPos, radius);

    public bool CanCollectPollen(Vector3 worldPos)
        => service.CanCollectPollen(worldPos);
}
