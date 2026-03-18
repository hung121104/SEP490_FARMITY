using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BlacklistPresenter
{
    private readonly IBlacklistService service;

    public BlacklistPresenter(IBlacklistService service)
    {
        this.service = service;
    }

    public async Task<HashSet<string>> GetBlacklistSet(string worldId)
    {
        WorldBlacklistResponse response = await service.GetBlacklist(worldId);
        if (response == null)
            return null;

        return ToHashSet(response.blacklistedPlayerIds);
    }

    public async Task<HashSet<string>> AddToBlacklist(string worldId, string playerId)
    {
        BlacklistMutateResponse response = await service.AddToBlacklist(worldId, playerId);
        if (response == null)
            return null;

        return ToHashSet(response.blacklistedPlayerIds);
    }

    public async Task<HashSet<string>> RemoveFromBlacklist(string worldId, string playerId)
    {
        BlacklistMutateResponse response = await service.RemoveFromBlacklist(worldId, playerId);
        if (response == null)
            return null;

        return ToHashSet(response.blacklistedPlayerIds);
    }

    private HashSet<string> ToHashSet(string[] ids)
    {
        HashSet<string> set = new HashSet<string>();
        if (ids == null)
            return set;

        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrEmpty(ids[i]))
                set.Add(ids[i]);
        }

        return set;
    }
}
