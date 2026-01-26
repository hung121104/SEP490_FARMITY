using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public string access_token;
    public string userId;  // Added for backend user ID
    public string displayName;  // Optional: Display name from backend
}

public class AuthenticateService : IAuthenticateService
{
    public static string JwtToken { get; private set; }  // Session-only storage for JWT
    public static string UserId { get; private set; }    // Session-only storage for user ID

    public async Task<LoginResponse> Login(LoginRequest request)
    {
        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest("https://localhost:3000/auth/login-ingame", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(jsonResponse);
                JwtToken = response.access_token;  // Store token in memory for session
                UserId = response.userId;          // Store user ID in memory for session
                Debug.Log("Login successful, token and user ID stored in session");
                return response;
            }
            else
            {
                Debug.LogError("Login failed: " + webRequest.error);
                return null;
            }
        }
    }
}
