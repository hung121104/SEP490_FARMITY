using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldPublishController : MonoBehaviourPunCallbacks
{
    [Header("Option Panel")]
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button openPanelButton;
    [SerializeField] private Button closePanelButton;

    [Header("Publish Controls")]
    [SerializeField] private Toggle publicToggle;
    [SerializeField] private Toggle passwordToggle;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button applyButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private bool suppressToggleEvents;
    private bool hasLocalUnsavedChanges;

    private void Start()
    {
        if (openPanelButton != null)
            openPanelButton.onClick.AddListener(OpenPanel);

        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(ClosePanel);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyPublishSettings);

        if (publicToggle != null)
            publicToggle.onValueChanged.AddListener(OnPublicToggleChanged);

        if (passwordToggle != null)
            passwordToggle.onValueChanged.AddListener(OnPasswordToggleChanged);

        if (optionPanel != null)
            optionPanel.SetActive(false);

        RefreshFromRoom();
    }

    private void OnDestroy()
    {
        if (openPanelButton != null)
            openPanelButton.onClick.RemoveListener(OpenPanel);

        if (closePanelButton != null)
            closePanelButton.onClick.RemoveListener(ClosePanel);

        if (applyButton != null)
            applyButton.onClick.RemoveListener(ApplyPublishSettings);

        if (publicToggle != null)
            publicToggle.onValueChanged.RemoveListener(OnPublicToggleChanged);

        if (passwordToggle != null)
            passwordToggle.onValueChanged.RemoveListener(OnPasswordToggleChanged);
    }

    public override void OnJoinedRoom()
    {
        RefreshFromRoom();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!HasPublishPropertyChange(propertiesThatChanged))
            return;

        // Keep the user's in-progress edits while the panel is open.
        if (optionPanel != null && optionPanel.activeSelf && hasLocalUnsavedChanges)
            return;

        RefreshFromRoom();
    }

    public void OpenPanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(true);

        hasLocalUnsavedChanges = false;
        RefreshFromRoom();
    }

    public void ClosePanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(false);

        hasLocalUnsavedChanges = false;
    }

    public void ApplyPublishSettings()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            SetStatus("Not in room.");
            return;
        }

        if (!CanEditPublishState())
        {
            SetStatus("Only room owner/master can change publish state.");
            return;
        }

        bool makePublic = publicToggle != null && publicToggle.isOn;
        bool usePassword = passwordToggle != null && passwordToggle.isOn;
        string plainPassword = passwordInput != null ? (passwordInput.text ?? string.Empty) : string.Empty;

        if (!makePublic)
        {
            usePassword = false;
            plainPassword = string.Empty;
        }

        if (makePublic && usePassword && string.IsNullOrWhiteSpace(plainPassword))
        {
            SetStatus("Password is required when password mode is enabled.");
            return;
        }

        PhotonNetwork.CurrentRoom.IsVisible = makePublic;
        PhotonNetwork.CurrentRoom.IsOpen = makePublic;

        var props = new Hashtable
        {
            { WorldRoomProperties.IsPublic, makePublic },
            { WorldRoomProperties.HasPassword, usePassword },
            { WorldRoomProperties.PasswordHash, usePassword ? WorldRoomProperties.ComputeSha256(plainPassword) : string.Empty }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        hasLocalUnsavedChanges = false;

        SetStatus(makePublic
            ? (usePassword ? "World is now public with password." : "World is now public.")
            : "World is now private.");
    }

    private void OnPublicToggleChanged(bool isPublic)
    {
        if (suppressToggleEvents)
            return;

        hasLocalUnsavedChanges = true;

        bool allowPassword = isPublic;
        if (passwordToggle != null)
            passwordToggle.interactable = allowPassword;

        if (!allowPassword && passwordToggle != null)
            passwordToggle.isOn = false;

        if (passwordInput != null)
            passwordInput.interactable = allowPassword && passwordToggle != null && passwordToggle.isOn;
    }

    private void OnPasswordToggleChanged(bool enabled)
    {
        if (suppressToggleEvents)
            return;

        hasLocalUnsavedChanges = true;

        if (passwordInput != null)
            passwordInput.interactable = enabled && publicToggle != null && publicToggle.isOn;
    }

    private bool CanEditPublishState()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.IsMasterClient)
            return true;

        string ownerId = WorldRoomProperties.GetString(
            PhotonNetwork.CurrentRoom.CustomProperties,
            WorldRoomProperties.OwnerId,
            string.Empty);

        string currentUserId = SessionManager.Instance?.UserId ?? string.Empty;
        return !string.IsNullOrEmpty(ownerId) && ownerId == currentUserId;
    }

    private void RefreshFromRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            SetStatus("Room not ready.");
            return;
        }

        bool isPublic = WorldRoomProperties.GetBool(
            PhotonNetwork.CurrentRoom.CustomProperties,
            WorldRoomProperties.IsPublic,
            PhotonNetwork.CurrentRoom.IsVisible);
        bool hasPassword = WorldRoomProperties.GetBool(
            PhotonNetwork.CurrentRoom.CustomProperties,
            WorldRoomProperties.HasPassword,
            false);

        suppressToggleEvents = true;

        if (publicToggle != null)
            publicToggle.isOn = isPublic;

        if (passwordToggle != null)
        {
            passwordToggle.isOn = hasPassword;
            passwordToggle.interactable = isPublic;
        }

        if (passwordInput != null)
            passwordInput.interactable = isPublic && hasPassword;

        suppressToggleEvents = false;
        hasLocalUnsavedChanges = false;

        SetStatus(isPublic
            ? (hasPassword ? "Current: Public (password protected)" : "Current: Public")
            : "Current: Private");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[WorldPublishController] " + message);
    }

    private bool HasPublishPropertyChange(Hashtable changedProps)
    {
        if (changedProps == null)
            return false;

        return changedProps.ContainsKey(WorldRoomProperties.IsPublic)
            || changedProps.ContainsKey(WorldRoomProperties.HasPassword)
            || changedProps.ContainsKey(WorldRoomProperties.PasswordHash);
    }
}
