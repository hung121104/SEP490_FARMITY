using ExitGames.Client.Photon;
using System;
using System.Security.Cryptography;
using System.Text;

public static class WorldRoomProperties
{
    public const string DisplayName = "displayName";
    public const string IsPublic = "isPublic";
    public const string OwnerId = "ownerId";
    public const string HasPassword = "hasPassword";
    public const string PasswordHash = "passwordHash";

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

    public static string ComputeSha256(string input)
    {
        input ??= string.Empty;

        using (var sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha.ComputeHash(bytes);

            var builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    public static bool VerifyPassword(string plainTextPassword, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return false;

        string actualHash = ComputeSha256(plainTextPassword ?? string.Empty);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }
}
