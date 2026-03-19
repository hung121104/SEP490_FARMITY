using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RequestResetPasswordRequest
{
    public string email;
}

[Serializable]
public class RequestResetPasswordResponse
{
    public bool ok;
    public string message;
}

[Serializable]
public class ConfirmResetPasswordRequest
{
    public string email;
    public string otp;
    public string newPassword;
}

[Serializable]
public class ConfirmResetPasswordResponse
{
    public bool ok;
    public string message;
}

[Serializable]
public class ApiErrorResponse
{
    public string message;
}

public class ResetPasswordService : IResetPasswordService
{
    private const string RequestResetUrl = "https://localhost:3000/auth/reset/request";
    private const string ConfirmResetUrl = "https://localhost:3000/auth/reset/confirm";

    public async Task<RequestResetPasswordResponse> RequestResetOtp(RequestResetPasswordRequest request)
    {
        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(RequestResetUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.certificateHandler = new AcceptAllCertificatesHandler();

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                RequestResetPasswordResponse response = JsonUtility.FromJson<RequestResetPasswordResponse>(webRequest.downloadHandler.text);
                return response ?? new RequestResetPasswordResponse { ok = true, message = "OTP sent." };
            }

            string errorMessage = ParseErrorMessage(webRequest.downloadHandler?.text, "Failed to request password reset.");
            Debug.LogError($"[ResetPasswordService] Request OTP failed ({webRequest.responseCode}): {errorMessage}");
            return new RequestResetPasswordResponse { ok = false, message = errorMessage };
        }
    }

    public async Task<ConfirmResetPasswordResponse> ConfirmReset(ConfirmResetPasswordRequest request)
    {
        string json = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(ConfirmResetUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.certificateHandler = new AcceptAllCertificatesHandler();

            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                ConfirmResetPasswordResponse response = JsonUtility.FromJson<ConfirmResetPasswordResponse>(webRequest.downloadHandler.text);
                return response ?? new ConfirmResetPasswordResponse { ok = true, message = "Password reset successful." };
            }

            string errorMessage = ParseErrorMessage(webRequest.downloadHandler?.text, "Failed to confirm password reset.");
            Debug.LogError($"[ResetPasswordService] Confirm reset failed ({webRequest.responseCode}): {errorMessage}");
            return new ConfirmResetPasswordResponse { ok = false, message = errorMessage };
        }
    }

    private static string ParseErrorMessage(string responseText, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                ApiErrorResponse error = JsonUtility.FromJson<ApiErrorResponse>(responseText);
                if (error != null && !string.IsNullOrWhiteSpace(error.message))
                    return error.message;
            }
            catch
            {
                // Ignore parse errors and use fallback text.
            }
        }

        return fallback;
    }

    private class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}
