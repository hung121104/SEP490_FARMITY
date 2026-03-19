using UnityEngine;
using Photon.Pun;

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

}
