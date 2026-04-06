using System.Threading.Tasks;

public interface IBlacklistService
{
    Task<WorldBlacklistResponse> GetBlacklist(string worldId);
    Task<BlacklistMutateResponse> AddToBlacklist(string worldId, string playerId);
    Task<BlacklistMutateResponse> RemoveFromBlacklist(string worldId, string playerId);
}
