using UnityEngine;

/// <summary>
/// Interface for resource harvesting business logic.
/// </summary>
public interface IResourceHarvestingService
{
    /// <summary>
    /// Attempts to hit a resource at the given world position with the specified tool.
    /// Resolves the target tile, calculates chunk/tile coordinates, and delegates
    /// to the host-authoritative RPC path.
    /// </summary>
    bool TryHitResource(ToolData tool, Vector3 worldPos);
}
