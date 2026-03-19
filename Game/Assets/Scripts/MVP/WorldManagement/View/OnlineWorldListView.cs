using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class OnlineWorldListView : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;
    [SerializeField] private bool autoConnectOnStart = true;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Password Prompt (optional)")]
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button passwordConfirmButton;
    [SerializeField] private Button passwordCancelButton;
    [SerializeField] private TextMeshProUGUI passwordPromptText;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListEntries = new Dictionary<string, GameObject>();
    private RoomInfo pendingPasswordRoom;

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

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        if (passwordConfirmButton != null)
        {
            passwordConfirmButton.onClick.AddListener(OnPasswordConfirmClicked);
        }

        if (passwordCancelButton != null)
        {
            passwordCancelButton.onClick.AddListener(ClosePasswordPanel);
        }

        if (passwordPanel != null)
        {
            passwordPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshRoomList);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        if (passwordConfirmButton != null)
        {
            passwordConfirmButton.onClick.RemoveListener(OnPasswordConfirmClicked);
        }

        if (passwordCancelButton != null)
        {
            passwordCancelButton.onClick.RemoveListener(ClosePasswordPanel);
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
            if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.UserId))
            {
                PhotonNetwork.AuthValues = new Photon.Realtime.AuthenticationValues(SessionManager.Instance.UserId);
            }
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
        ClosePasswordPanel();
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
            bool isPublic = WorldRoomProperties.GetBool(info.CustomProperties, WorldRoomProperties.IsPublic, info.IsVisible);

            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList || !isPublic)
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

        if (!cachedRoomList.TryGetValue(roomName, out RoomInfo roomInfo) || roomInfo == null)
        {
            UpdateStatus("Room no longer exists. Please refresh.");
            return;
        }

        bool hasPassword = WorldRoomProperties.GetBool(roomInfo.CustomProperties, WorldRoomProperties.HasPassword, false);
        if (hasPassword)
        {
            pendingPasswordRoom = roomInfo;
            OpenPasswordPanel(roomInfo);
            return;
        }

        JoinRoomByName(roomName);
    }

    public void OnBackButtonClicked()
    {
        ClosePasswordPanel();

        // Avoid handling incoming Photon callbacks during scene transition.
        PhotonNetwork.IsMessageQueueRunning = false;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("OnlineWorldListView: Main menu scene name is empty.");
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnPasswordConfirmClicked()
    {
        if (pendingPasswordRoom == null)
        {
            ClosePasswordPanel();
            return;
        }

        string expectedHash = WorldRoomProperties.GetString(
            pendingPasswordRoom.CustomProperties,
            WorldRoomProperties.PasswordHash,
            string.Empty);

        if (string.IsNullOrEmpty(expectedHash))
        {
            UpdateStatus("Room password info is unavailable. Please refresh.");
            return;
        }

        string enteredPassword = passwordInput != null ? (passwordInput.text ?? string.Empty) : string.Empty;
        if (!WorldRoomProperties.VerifyPassword(enteredPassword, expectedHash))
        {
            if (passwordPromptText != null)
            {
                passwordPromptText.text = "Wrong password. Try again.";
            }
            UpdateStatus("Wrong password.");
            return;
        }

        string roomName = pendingPasswordRoom.Name;
        ClosePasswordPanel();
        JoinRoomByName(roomName);
    }

    private void OpenPasswordPanel(RoomInfo roomInfo)
    {
        if (passwordPanel == null)
        {
            UpdateStatus("This room requires a password.");
            return;
        }

        if (passwordInput != null)
        {
            passwordInput.text = string.Empty;
            passwordInput.ActivateInputField();
        }

        if (passwordPromptText != null)
        {
            string displayName = WorldRoomProperties.GetString(
                roomInfo.CustomProperties,
                WorldRoomProperties.DisplayName,
                roomInfo.Name);
            passwordPromptText.text = "Enter password for: " + displayName;
        }

        passwordPanel.SetActive(true);
    }

    public void ClosePasswordPanel()
    {
        pendingPasswordRoom = null;

        if (passwordInput != null)
            passwordInput.text = string.Empty;

        if (passwordPanel != null)
            passwordPanel.SetActive(false);
    }

    private void JoinRoomByName(string roomName)
    {
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
