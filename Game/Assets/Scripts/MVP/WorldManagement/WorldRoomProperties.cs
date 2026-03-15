using ExitGames.Client.Photon;

public static class WorldRoomProperties
{
    public const string DisplayName = "displayName";
    public const string IsPublic = "isPublic";
    public const string OwnerId = "ownerId";

    public static bool GetBool(Hashtable props, string key, bool defaultValue = false)
    {
        if (props == null || !props.ContainsKey(key) || props[key] == null)
            return defaultValue;

        if (props[key] is bool b)
            return b;

        return defaultValue;
    }

    public static string GetString(Hashtable props, string key, string defaultValue = "")
    {
        if (props == null || !props.ContainsKey(key) || props[key] == null)
            return defaultValue;

        return props[key].ToString();
    }
}
