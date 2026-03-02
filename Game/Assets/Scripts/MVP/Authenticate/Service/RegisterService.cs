using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;

[Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
    public string email;
}

[Serializable]
public class RegisterResponse
{
    public string userId;
    public string username;
    public string email;
}

public class RegisterService : IRegisterService
{
    private const string RegisterUrl = "https://localhost:3000/auth/register";

    public async Task<RegisterResponse> Register(RegisterRequest request)
    {
        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(RegisterUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.certificateHandler = new AcceptAllCertificatesHandler();

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                RegisterResponse response = JsonUtility.FromJson<RegisterResponse>(webRequest.downloadHandler.text);
                Debug.Log($"[RegisterService] Registration successful for user: {response.username}");
                return response;
            }
            else
            {
                Debug.LogError($"[RegisterService] Registration failed ({webRequest.responseCode}): {webRequest.error}\n{webRequest.downloadHandler.text}");
                return null;
            }
        }
    }

    // Accept self-signed certs (matches WorldDataBootstrapper pattern)
    private class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
