using UnityEngine;

/// <summary>
/// Presenter for resource harvesting — plain C# class (no MonoBehaviour).
/// Routes tool-use actions from the View to the Service layer and relays
/// results back to the View for visual feedback.
/// </summary>
public class ResourceHarvestingPresenter
{
    private readonly ResourceHarvestingView view;
    private readonly IResourceHarvestingService harvestingService;

    public ResourceHarvestingPresenter(ResourceHarvestingView view, IResourceHarvestingService service)
    {
        this.view = view;
        this.harvestingService = service;
    }

    /// <summary>
    /// Called by the View when an axe or pickaxe impact is requested.
    /// Delegates to the service and notifies the view of the result.
    /// </summary>
    public void HandleToolHit(ToolData tool, Vector3 worldPos)
    {
        if (tool == null) return;

        bool dispatched = harvestingService.TryHitResource(tool, worldPos);

        if (dispatched)
            view.OnHitDispatched(tool, worldPos);
        else
            view.OnHitFailed(tool, worldPos);
    }
}
