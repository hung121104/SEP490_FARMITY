using System;

[Serializable]
public class WorldBlacklistResponse
{
    public string worldId;
    public string[] blacklistedPlayerIds;
}

[Serializable]
public class BlacklistMutateResponse
{
    public string worldId;
    public string playerId;
    public bool added;
    public bool removed;
    public string[] blacklistedPlayerIds;
}

[Serializable]
public class BlacklistMutateRequest
{
    public string _id;
    public string playerId;
}
