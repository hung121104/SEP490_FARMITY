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
    public string status;
    public string message;
}

[Serializable]
public class VerifyRegistrationRequest
{
    public string email;
    public string otp;
}

[Serializable]
public class VerifyRegistrationResponse
{
    public bool ok;
    public string message;
}

public class RegisterService : IRegisterService
{
    private const string RegisterUrl       = "https://localhost:3000/auth/register";
    private const string VerifyUrl         = "https://localhost:3000/auth/verify-registration";

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
                Debug.Log($"[RegisterService] Registration initiated: {response.status}");
                return response;
            }
            else
            {
                Debug.LogError($"[RegisterService] Registration failed ({webRequest.responseCode}): {webRequest.error}\n{webRequest.downloadHandler.text}");
                return null;
            }
        }
    }

    public async Task<VerifyRegistrationResponse> VerifyRegistration(VerifyRegistrationRequest request)
    {
        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(VerifyUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.certificateHandler = new AcceptAllCertificatesHandler();

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                VerifyRegistrationResponse response = JsonUtility.FromJson<VerifyRegistrationResponse>(webRequest.downloadHandler.text);
                Debug.Log($"[RegisterService] Verification result: {response.message}");
                return response;
            }
            else
            {
                Debug.LogError($"[RegisterService] Verification failed ({webRequest.responseCode}): {webRequest.error}\n{webRequest.downloadHandler.text}");
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
