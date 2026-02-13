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
    public string userId;
    public string username;
    public string access_token;
}

public class AuthenticateService : IAuthenticateService
{
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
                
                // Store authentication data in SessionManager
                SessionManager.Instance.SetAuthenticationData(
                    response.access_token, 
                    response.userId, 
                    response.username
                );
                
                Debug.Log("Login successful for user: " + response.username);
                return response;
            }
            else
            {
                Debug.LogError("Login request failed: " + webRequest.error);
                return null;
            }
        }
    }
}
