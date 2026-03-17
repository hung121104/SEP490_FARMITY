using UnityEngine;

/// <summary>
/// Presenter for the crop-fertilizing action.
/// Translates player input (world position) into service calls and
/// notifies the View of success or failure.
/// Plain C# class — no MonoBehaviour dependency.
/// </summary>
public class CropFertilizingPresenter
{
    private readonly ICropFertilizingService service;
    private readonly CropFertilizingView view;

    public CropFertilizingPresenter(CropFertilizingView view, ICropFertilizingService service)
    {
        this.view    = view;
        this.service = service;
    }

    /// <summary>Initializes the presenter and service.</summary>
    public void Initialize()
    {
        service.Initialize();
    }

    /// <summary>
    /// Called when the player uses the Fertilizer at <paramref name="worldPosition"/>.
    /// </summary>
    public void HandleFertilizeAction(Vector3 worldPosition)
    {
        bool success = service.FertilizeTile(worldPosition);

        if (success)
            view.OnFertilizeSuccess(worldPosition);
        else
            view.OnFertilizeFailed(worldPosition);
    }
}
