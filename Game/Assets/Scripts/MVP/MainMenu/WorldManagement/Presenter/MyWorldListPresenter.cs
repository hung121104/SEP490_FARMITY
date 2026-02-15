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
    /// Create a new world using the service and notify the view.
    /// </summary>
    public async Task<WorldModel> CreateWorld(string worldName)
    {
        try
        {
            var resp = await service.CreateWorld(worldName);
            if (resp == null)
            {
                view?.ShowError("Failed to create world.");
                return null;
            }

            // Map WorldResponse to WorldModel
            WorldModel model = new WorldModel()
            {
                _id = resp._id,
                worldName = resp.worldName,
                ownerId = resp.ownerId
                // other fields remain at default values (day/month/year/hour/minute/gold)
            };

            // Notify view to add the created world to the list
            view?.AddWorld(model);

            return model;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error creating world: {ex.Message}");
            view?.ShowError($"Error: {ex.Message}");
            return null;
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
