using System.Threading.Tasks;

public interface IMyWorldListService
{
    /// <summary>
    /// Get all worlds owned by the authenticated account
    /// </summary>
    /// <param name="ownerId">Optional owner ID (only allowed for admin accounts)</param>
    /// <returns>Array of world models</returns>
    Task<WorldModel[]> GetWorlds(string ownerId = null);

    /// <summary>
    /// Create a new world with given name
    /// </summary>
    Task<WorldResponse> CreateWorld(string worldName);
}
