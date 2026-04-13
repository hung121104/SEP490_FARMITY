using UnityEngine;

/// <summary>
/// Lazy resolver for IInventoryService.
///
/// Use this when a Service/MonoBehaviour is created during Awake/Start and
/// InventoryGameView may not exist yet, or lives on an inactive UI panel.
/// FindAnyObjectByType(FindObjectsInactive.Include) handles the inactive case.
///
/// Pass <see cref="Resolve"/> as a Func provider into services that need
/// IInventoryService — they call it on first use and cache the result.
/// </summary>
public static class InventoryServiceLocator
{
    private static IInventoryService _cached;

    public static IInventoryService Resolve()
    {
        if (_cached != null) return _cached;

        var view = Object.FindAnyObjectByType<InventoryGameView>(FindObjectsInactive.Include);
        if (view == null) return null;

        _cached = view.GetInventoryService();
        return _cached;
    }

    /// <summary>Drop the cached reference (e.g. on scene unload).</summary>
    public static void Invalidate() => _cached = null;
}
