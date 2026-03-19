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
        // Re-enable message queue in case it was disabled during scene transition
        PhotonNetwork.IsMessageQueueRunning = true;

        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "hk";

        if (PhotonNetwork.InLobby)
        {
            // Already in a lobby — go directly to room joining.
            OnJoinedLobby();
        }
        else if (PhotonNetwork.IsConnectedAndReady ||
            PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.ConnectedToMasterServer)
        {
            // Already connected after leaving a room — jump straight to lobby.
            // Do NOT touch AuthValues here; Photon already holds a valid token.
            PhotonNetwork.JoinLobby();
        }
        else if (!PhotonNetwork.IsConnected)
        {
            // Fresh connection — safe to set AuthValues now.
            ConnectWithAuth();
        }
        // else: still transitioning (e.g. Disconnecting) — OnDisconnected will fire and reconnect.
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log($"[LoadWorld] OnDisconnected: {cause}. Reconnecting...");
        ConnectWithAuth();
    }

    private void ConnectWithAuth()
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
        customProps[WorldRoomProperties.DisplayName] = displayName;
        customProps[WorldRoomProperties.IsPublic] = false;
        customProps[WorldRoomProperties.OwnerId] = SessionManager.Instance?.UserId ?? string.Empty;
        customProps[WorldRoomProperties.HasPassword] = false;
        customProps[WorldRoomProperties.PasswordHash] = string.Empty;
        
        var roomOptions = new RoomOptions 
        { 
            MaxPlayers = 4, 
            IsVisible = false,
            IsOpen = false,
            CustomRoomProperties = customProps,
            CustomRoomPropertiesForLobby = new string[]
            {
                WorldRoomProperties.DisplayName,
                WorldRoomProperties.IsPublic,
                WorldRoomProperties.HasPassword,
                WorldRoomProperties.PasswordHash
            },
            EmptyRoomTtl = 0,
        };
        
        PhotonNetwork.JoinOrCreateRoom(selectedId, roomOptions, TypedLobby.Default);
    }
    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("GameCoreTestScene");
    }
}
