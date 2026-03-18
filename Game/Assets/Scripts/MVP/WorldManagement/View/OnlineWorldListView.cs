using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    [Header("Join Denied Panel (optional)")]
    [SerializeField] private GameObject joinDeniedPanel;
    [SerializeField] private TextMeshProUGUI joinDeniedText;
    [SerializeField] private Button joinDeniedOkButton;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomListEntries = new Dictionary<string, GameObject>();
    private RoomInfo pendingPasswordRoom;
    private BlacklistPresenter blacklistPresenter;
    private bool isJoinValidationInProgress;

    private void Awake()
    {
        blacklistPresenter = new BlacklistPresenter(new BlacklistService());

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

        if (joinDeniedOkButton != null)
        {
            joinDeniedOkButton.onClick.AddListener(HideJoinDeniedPanel);
        }

        if (joinDeniedPanel != null)
        {
            joinDeniedPanel.SetActive(false);
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

        if (joinDeniedOkButton != null)
        {
            joinDeniedOkButton.onClick.RemoveListener(HideJoinDeniedPanel);
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

        bool canJoinLobbyNow = PhotonNetwork.IsConnectedAndReady
            || PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer;

        if (canJoinLobbyNow)
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
        else if (!PhotonNetwork.IsConnected)
        {
            if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.UserId))
            {
                PhotonNetwork.AuthValues = new Photon.Realtime.AuthenticationValues(SessionManager.Instance.UserId);
            }
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            // Already in a connecting state; wait for OnConnectedToMaster callback.
            UpdateStatus("Connecting to master...");
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
        ShowJoinDeniedMessage("Unable to join this world right now. Please try another world.");
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
        if (isJoinValidationInProgress)
            return;

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

        _ = ValidateAndJoinAsync(roomInfo);
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
        if (isJoinValidationInProgress)
            return;

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

        RoomInfo roomInfo = pendingPasswordRoom;
        ClosePasswordPanel();
        _ = ValidateAndJoinAsync(roomInfo);
    }

    private async Task ValidateAndJoinAsync(RoomInfo roomInfo)
    {
        if (roomInfo == null)
            return;

        if (isJoinValidationInProgress)
            return;

        isJoinValidationInProgress = true;
        ShowLoading(true);

        try
        {
            string worldId = ResolveWorldIdFromRoomInfo(roomInfo);
            if (string.IsNullOrEmpty(worldId))
            {
                UpdateStatus("Cannot join: world id is missing.");
                ShowJoinDeniedMessage("Unable to join this world because its id is missing.");
                return;
            }

            HashSet<string> blacklist = await blacklistPresenter.GetBlacklistSet(worldId);
            if (blacklist == null)
            {
                UpdateStatus("Cannot verify blacklist right now. Please try again.");
                ShowJoinDeniedMessage("Cannot verify world access right now. Please try again.");
                return;
            }

            string myId = SessionManager.Instance != null ? SessionManager.Instance.UserId : string.Empty;
            if (!string.IsNullOrEmpty(myId) && blacklist.Contains(myId))
            {
                UpdateStatus("You are blacklisted from this world.");
                ShowJoinDeniedMessage("You cannot join this world because you are blacklisted.");
                return;
            }

            JoinRoomByName(roomInfo.Name);
        }
        finally
        {
            isJoinValidationInProgress = false;
            if (!PhotonNetwork.InRoom)
                ShowLoading(false);
        }
    }

    private static string ResolveWorldIdFromRoomInfo(RoomInfo roomInfo)
    {
        if (roomInfo == null)
            return string.Empty;

        string worldId = WorldRoomProperties.GetString(roomInfo.CustomProperties, WorldRoomProperties.WorldId, string.Empty);
        if (!string.IsNullOrEmpty(worldId))
            return worldId;

        // Fallback to room name for older rooms missing worldId custom property.
        return roomInfo.Name;
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

    private void ShowJoinDeniedMessage(string message)
    {
        if (joinDeniedText != null)
            joinDeniedText.text = message;

        if (joinDeniedPanel != null)
            joinDeniedPanel.SetActive(true);
    }

    private void HideJoinDeniedPanel()
    {
        if (joinDeniedPanel != null)
            joinDeniedPanel.SetActive(false);
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
