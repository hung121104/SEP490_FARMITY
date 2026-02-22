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
            PhotonNetwork.AuthValues = new Photon.Realtime.AuthenticationValues(SessionManager.Instance.UserId);
        }
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
