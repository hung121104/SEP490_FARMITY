using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
public class LoadWorld : MonoBehaviourPunCallbacks
{
    private void Start()
    {
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

        // try to get the selected world id from the session manager
        var manager = WorldSelectionManager.EnsureExists();
        var selectedId = manager.SelectedWorldId;

        if (string.IsNullOrEmpty(selectedId))
        {
            Debug.LogWarning("LoadWorldView: no SelectedWorldId found; loading LobbyScene instead.");
            // Leave lobby before loading scene to avoid Photon errors
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.LeaveLobby();
            }
            PhotonNetwork.IsMessageQueueRunning = false;
            SceneManager.LoadScene("LobbyScene");
            return;
        }

        Debug.Log($"Joining or creating Photon room for world id: {selectedId}");
        var roomOptions = new RoomOptions { MaxPlayers = 4, IsVisible = true, IsOpen = true };
        PhotonNetwork.JoinOrCreateRoom(selectedId, roomOptions, TypedLobby.Default);
    }
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("GameCoreTestScene");
    }
}
