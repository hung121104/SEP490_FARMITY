using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class OnlineWorldListView : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Button refreshButton;
    [SerializeField] private bool autoConnectOnStart = true;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListEntries = new Dictionary<string, GameObject>();

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        
        if (roomItemPrefab != null && roomItemPrefab.activeInHierarchy)
        {
            roomItemPrefab.SetActive(false);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshRoomList);
        }
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.IsMessageQueueRunning = true;
        }
        
        if (autoConnectOnStart)
        {
            ConnectToPhoton();
        }
    }

    public void ConnectToPhoton()
    {
        ShowLoading(true);
        UpdateStatus("Connecting...");

        if (PhotonNetwork.IsConnected)
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
            else
            {
                ShowLoading(false);
            }
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void RefreshRoomList()
    {
        if (PhotonNetwork.InLobby)
        {
            ShowLoading(true);
        }
        else
        {
            ConnectToPhoton();
        }
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        UpdateStatus("Connected! Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        UpdateStatus("Joined lobby. Loading rooms...");
        cachedRoomList.Clear();
        ClearRoomListView();
    }

    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();
        ClearRoomListView();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        UpdateRoomListView();
        UpdateStatus($"{cachedRoomList.Count} room(s) available");
        ShowLoading(false);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateStatus($"Disconnected: {cause}");
        ShowLoading(false);
        cachedRoomList.Clear();
        ClearRoomListView();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        UpdateStatus($"Join failed: {message}");
        ShowLoading(false);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("GameCoreTestScene");
    }

    #endregion

    #region Room List Management

    /// <summary>
    /// Update the cached room list based on Photon's room updates
    /// </summary>
    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                cachedRoomList.Remove(info.Name);
            }
            else
            {
                cachedRoomList[info.Name] = info;
            }
        }
    }

    private void UpdateRoomListView()
    {
        ClearRoomListView();

        foreach (RoomInfo info in cachedRoomList.Values)
        {
            CreateRoomListEntry(info);
        }
    }

    private void CreateRoomListEntry(RoomInfo roomInfo)
    {
        if (roomItemPrefab == null || roomListContainer == null) return;

        GameObject entry = Instantiate(roomItemPrefab, roomListContainer);
        entry.SetActive(true);

        OnlineWorldItemView itemView = entry.GetComponent<OnlineWorldItemView>();
        if (itemView != null)
        {
            itemView.Initialize(roomInfo, OnJoinRoomClicked);
            roomListEntries.Add(roomInfo.Name, entry);
        }
    }

    private void ClearRoomListView()
    {
        foreach (GameObject entry in roomListEntries.Values)
        {
            Destroy(entry);
        }
        roomListEntries.Clear();
    }

    #endregion

    #region UI Callbacks

    public void OnJoinRoomClicked(string roomName)
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.InRoom)
        {
            UpdateStatus("Cannot join room!");
            return;
        }

        UpdateStatus($"Joining {roomName}...");
        ShowLoading(true);

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        PhotonNetwork.JoinRoom(roomName);
    }

    private void ShowLoading(bool show)
    {
        if (loadingPanel != null) loadingPanel.SetActive(show);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    #endregion
}
