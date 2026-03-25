using UnityEngine;
using Photon.Pun;
using UnityEngine.Networking;
using System.Collections;

public class LogOutView : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick.AddListener(OnLogOutClicked);
    }

    private void OnLogOutClicked()
    {
        Debug.Log("[LogOutView] Log out button clicked.");

        string token = SessionManager.Instance?.JwtToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            StartCoroutine(SendServerLogout(token));
        }

        // If connected, request a clean disconnect and clear session in OnDisconnected callback.
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[LogOutView] Disconnecting from Photon...");
            // Re-enable message queue so the disconnect completes and OnDisconnected fires.
            PhotonNetwork.IsMessageQueueRunning = true;
            PhotonNetwork.Disconnect();
            return;
        }

        // If not connected, clear authentication immediately.
        PhotonNetwork.AuthValues = null;
        ClearToken();
    }
    
    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log($"[LogOutView] OnDisconnected: {cause}. Clearing auth/session.");
        PhotonNetwork.AuthValues = null;
        ClearToken();
    }
    private void OnDestroy()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick.RemoveListener(OnLogOutClicked);
    }

    private void ClearToken()
    {
        SessionManager.Instance.ClearSession();
    }

    private IEnumerator SendServerLogout(string jwtToken)
    {
        string url = $"{AppConfig.ApiBaseUrl.TrimEnd('/')}/auth/logout";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(new byte[0]);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            req.certificateHandler = new AcceptAllCertificatesHandler();
            req.timeout = 10;

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isError = req.result != UnityWebRequest.Result.Success;
#else
            bool isError = req.isNetworkError || req.isHttpError;
#endif

            if (isError)
            {
                Debug.LogWarning($"[LogOutView] Server logout failed ({req.responseCode}): {req.downloadHandler?.text}");
            }
            else
            {
                Debug.Log("[LogOutView] Server logout succeeded.");
            }
        }
    }

    private class AcceptAllCertificatesHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

}
