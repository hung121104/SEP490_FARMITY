using UnityEngine;

/// <summary>
/// Orchestrates pollen-application. Receives commands from CropBreedingView
/// and fires success / failure callbacks back to it.
/// </summary>
public class CropBreedingPresenter
{
    private readonly ICropBreedingService service;
    private readonly CropBreedingView view;

    public CropBreedingPresenter(CropBreedingView view, ICropBreedingService service)
    {
        this.view    = view;
        this.service = service;
    }

    public void HandleApplyPollen(PollenDataSO pollen, Vector3 targetWorldPos)
    {
        if (!service.CanApplyPollen(pollen, targetWorldPos))
        {
            view.OnBreedingFailed(targetWorldPos);
            return;
        }

        bool success = service.TryApplyPollen(pollen, targetWorldPos);
        if (success)
            view.OnBreedingSuccess(targetWorldPos);
        else
            view.OnBreedingFailed(targetWorldPos);
    }
}
