using UnityEngine;

/// <summary>
/// View for resource harvesting — MonoBehaviour that wires the MVP pattern.
/// Subscribes to UseToolService axe/pickaxe impact events and delegates
/// to the Presenter. Provides visual feedback callbacks. Zero business logic.
/// </summary>
public class ResourceHarvestingView : MonoBehaviour
{
    [Header("Harvest Settings")]
    [Min(0.1f)]
    [Tooltip("Max distance from local player to target tile when harvesting resources.")]
    [SerializeField] private float interactionRange = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private ResourceHarvestingPresenter presenter;

    private void Start()
    {
        // Build the MVP chain: View → Presenter → Service
        ResourceInteractionManager interactionManager = FindAnyObjectByType<ResourceInteractionManager>();
        IResourceHarvestingService service = new ResourceHarvestingService(interactionManager, interactionRange);
        presenter = new ResourceHarvestingPresenter(this, service);

        // Subscribe to delayed tool-impact events (animation-timed)
        UseToolService.OnAxeImpactRequested += HandleAxeRequested;
        UseToolService.OnPickaxeImpactRequested += HandlePickaxeRequested;
    }

    private void OnDestroy()
    {
        UseToolService.OnAxeImpactRequested -= HandleAxeRequested;
        UseToolService.OnPickaxeImpactRequested -= HandlePickaxeRequested;
    }

    private void HandleAxeRequested(ToolData tool, Vector3 pos)
    {
        presenter?.HandleToolHit(tool, pos);
    }

    private void HandlePickaxeRequested(ToolData tool, Vector3 pos)
    {
        presenter?.HandleToolHit(tool, pos);
    }

    /// <summary>Called by the Presenter when a hit request was successfully dispatched to the host.</summary>
    public void OnHitDispatched(ToolData tool, Vector3 worldPos)
    {
        if (showDebugLogs)
            Debug.Log($"[ResourceHarvestingView] Hit dispatched: tool={tool.itemID} at {worldPos}");
    }

    /// <summary>Called by the Presenter when a hit request failed (no valid target, etc.).</summary>
    public void OnHitFailed(ToolData tool, Vector3 worldPos)
    {
        if (showDebugLogs)
            Debug.Log($"[ResourceHarvestingView] Hit failed: tool={tool.itemID} at {worldPos}");
    }
}
