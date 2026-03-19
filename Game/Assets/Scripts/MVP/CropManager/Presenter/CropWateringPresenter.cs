using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Presenter for the crop-watering action.
/// Translates player input (world position) into service calls and
/// notifies the View of success or failure.
/// Plain C# class — no MonoBehaviour dependency.
/// </summary>
public class CropWateringPresenter
{
    private readonly ICropWateringService service;
    private readonly CropWateringView view;

    public CropWateringPresenter(CropWateringView view, ICropWateringService service)
    {
        this.view    = view;
        this.service = service;
    }

    /// <summary>Initializes the presenter and service with required references.</summary>
    public void Initialize(TileBase wateredTile)
    {
        service.Initialize(wateredTile);
    }

    /// <summary>
    /// Called when the player uses the Watering Can at <paramref name="worldPosition"/>.
    /// </summary>
    public void HandleWaterAction(Vector3 worldPosition)
    {
        bool success = service.WaterTile(worldPosition);

        if (success)
            view.OnWaterSuccess(worldPosition);
        else
            view.OnWaterFailed(worldPosition);
    }
}
