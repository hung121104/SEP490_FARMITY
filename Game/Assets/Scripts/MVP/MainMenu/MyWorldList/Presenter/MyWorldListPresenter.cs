using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Presenter for managing world list logic
/// </summary>
public class MyWorldListPresenter
{
    private readonly IMyWorldListService service;
    private MyWorldListView view;

    public MyWorldListPresenter(IMyWorldListService worldListService)
    {
        service = worldListService;
    }

    public void SetView(MyWorldListView worldListView)
    {
        view = worldListView;
    }

    /// <summary>
    /// Load worlds for the authenticated user
    /// </summary>
    public async Task LoadWorlds(string ownerId = null)
    {
        try
        {
            // Call service to get worlds
            WorldModel[] worlds = await service.GetWorlds(ownerId);

            if (worlds != null)
            {
                // Pass data to view for display
                view?.DisplayWorlds(worlds);
            }
            else
            {
                view?.ShowError("Failed to load worlds. Please check your connection.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading worlds: {ex.Message}");
            view?.ShowError($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Load worlds for a specific owner (admin only)
    /// </summary>
    public async Task LoadWorldsForOwner(string ownerId)
    {
        await LoadWorlds(ownerId);
    }
}
