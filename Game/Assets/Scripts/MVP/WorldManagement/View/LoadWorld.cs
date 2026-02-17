using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;
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
        
        // Set up custom properties for the room
        var customProps = new Hashtable();
        string displayName = !string.IsNullOrEmpty(manager.WorldName) 
            ? manager.WorldName 
            : selectedId;
        customProps["displayName"] = displayName;
        
        var roomOptions = new RoomOptions 
        { 
            MaxPlayers = 4, 
            IsVisible = true, 
            IsOpen = true,
            CustomRoomProperties = customProps,
            CustomRoomPropertiesForLobby = new string[] { "displayName" }
        };
        
        PhotonNetwork.JoinOrCreateRoom(selectedId, roomOptions, TypedLobby.Default);
    }
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("GameCoreTestScene");
    }
}
