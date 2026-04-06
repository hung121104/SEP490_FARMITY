using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
public class ConnectToServer : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.UserId))
        {
            var auth = new Photon.Realtime.AuthenticationValues(SessionManager.Instance.UserId);
            if (!string.IsNullOrEmpty(SessionManager.Instance.JwtToken))
            {
                // The Token property's setter is protected internal; send the JWT as custom auth parameters instead.
                auth.AuthType = Photon.Realtime.CustomAuthenticationType.Custom;
                auth.AddAuthParameter("token", SessionManager.Instance.JwtToken);
            }
            PhotonNetwork.AuthValues = auth;
        }

        // Ensure Photon message processing is enabled so callbacks are delivered.
        PhotonNetwork.IsMessageQueueRunning = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        SceneManager.LoadScene("LobbyScene");
    }
}
