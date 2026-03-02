using UnityEngine;

/// <summary>
/// Orchestrates crop harvesting: receives commands from the View,
/// delegates all business logic to the service, and fires callbacks
/// back to the View for UI updates.
/// </summary>
public class CropHarvestingPresenter
{
    private readonly ICropHarvestingService service;
    private readonly CropHarvestingView view;

    public CropHarvestingPresenter(CropHarvestingView view, ICropHarvestingService service)
    {
        this.view    = view;
        this.service = service;
    }

    // ── Called by the View ────────────────────────────────────────────────

    /// <summary>
    /// Try to harvest the crop at the given world position.
    /// Notifies the View of success or failure.
    /// </summary>
    public void HandleHarvestAction(Vector3 worldPos)
    {
        if (!service.IsReadyToHarvest(worldPos))
        {
            view.OnHarvestFailed(worldPos);
            return;
        }

        bool success = service.TryHarvest(worldPos, out ItemData harvestedItem);

        if (success)
            view.OnHarvestSuccess(worldPos, harvestedItem);
        else
            view.OnHarvestFailed(worldPos);
    }

    /// <summary>
    /// Scans for a nearby harvestable tile and returns its position for the prompt.
    /// Returns Vector3.zero if nothing in range.
    /// </summary>
    public Vector3 FindNearbyHarvestableTile(Vector3 playerPos, float radius)
    {
        return service.FindNearbyHarvestableTile(playerPos, radius);
    }

    /// <summary>Checks whether the tile at worldPos is ready to harvest.</summary>
    public bool IsReadyToHarvest(Vector3 worldPos) => service.IsReadyToHarvest(worldPos);

    // ── Pollen harvesting ─────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the crop at worldPos is at its pollen stage and can yield pollen.
    /// </summary>
    public bool IsReadyToCollectPollen(Vector3 worldPos) => service.IsReadyToCollectPollen(worldPos);

    /// <summary>
    /// Try to collect pollen from the crop at the given world position.
    /// The crop is NOT removed. Notifies the View of success or failure.
    /// </summary>
    public void HandleCollectPollenAction(Vector3 worldPos)
    {
        if (!service.IsReadyToCollectPollen(worldPos))
        {
            view.OnPollenCollectFailed(worldPos);
            return;
        }

        bool success = service.TryCollectPollen(worldPos, out PollenData pollenItem);

        if (success)
            view.OnPollenCollectSuccess(worldPos, pollenItem);
        else
            view.OnPollenCollectFailed(worldPos);
    }
}
