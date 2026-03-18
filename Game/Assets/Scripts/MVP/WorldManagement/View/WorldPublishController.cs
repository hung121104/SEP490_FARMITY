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
    [SerializeField] private Button publicStateButton;
    [SerializeField] private TextMeshProUGUI publicStateButtonText;
    [SerializeField] private Button passwordStateButton;
    [SerializeField] private TextMeshProUGUI passwordStateButtonText;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button applyButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private bool suppressToggleEvents;
    private bool hasLocalUnsavedChanges;
    private bool baselineIsPublic;
    private bool baselineHasPassword;
    private string baselinePasswordHash = string.Empty;
    private bool hasBaseline;
    private bool publicButtonState;
    private bool passwordButtonState;

    private void Start()
    {
        if (openPanelButton != null)
            openPanelButton.onClick.AddListener(OpenPanel);

        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(ClosePanel);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyPublishSettings);

        if (publicStateButton != null)
            publicStateButton.onClick.AddListener(OnPublicStateButtonClicked);

        if (passwordStateButton != null)
            passwordStateButton.onClick.AddListener(OnPasswordStateButtonClicked);

        if (passwordInput != null)
            passwordInput.onValueChanged.AddListener(OnPasswordInputChanged);

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

        if (publicStateButton != null)
            publicStateButton.onClick.RemoveListener(OnPublicStateButtonClicked);

        if (passwordStateButton != null)
            passwordStateButton.onClick.RemoveListener(OnPasswordStateButtonClicked);

        if (passwordInput != null)
            passwordInput.onValueChanged.RemoveListener(OnPasswordInputChanged);
    }

    public override void OnJoinedRoom()
    {
        RefreshFromRoom();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!HasPublishPropertyChange(propertiesThatChanged))
            return;

        // Keep the user's in-progress edits while the panel is open
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
        UpdateApplyButtonState();
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

        bool makePublic = GetPublicStateFromUI();
        bool usePassword = GetPasswordStateFromUI();
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

        baselineIsPublic = makePublic;
        baselineHasPassword = usePassword;
        baselinePasswordHash = usePassword ? WorldRoomProperties.ComputeSha256(plainPassword) : string.Empty;
        hasBaseline = true;
        hasLocalUnsavedChanges = false;
        UpdateApplyButtonState();

        SetStatus(makePublic
            ? (usePassword ? "World is now public with password." : "World is now public.")
            : "World is now private.");
    }

    private void OnPublicStateButtonClicked()
    {
        if (suppressToggleEvents)
            return;

        bool nextState = !GetPublicStateFromUI();
        suppressToggleEvents = true;
        SetPublicStateInUI(nextState);
        suppressToggleEvents = false;

        HandlePublicStateChanged(nextState);
    }

    private void HandlePublicStateChanged(bool isPublic)
    {
        hasLocalUnsavedChanges = true;

        bool allowPassword = isPublic;
        if (passwordStateButton != null)
            passwordStateButton.interactable = allowPassword;

        if (!allowPassword)
            SetPasswordStateInUI(false);

        if (passwordInput != null)
            passwordInput.interactable = allowPassword && GetPasswordStateFromUI();

        RecalculateDirtyState();
    }

    private void OnPasswordStateButtonClicked()
    {
        if (suppressToggleEvents)
            return;

        if (!GetPublicStateFromUI())
            return;

        bool nextState = !GetPasswordStateFromUI();
        suppressToggleEvents = true;
        SetPasswordStateInUI(nextState);
        suppressToggleEvents = false;

        HandlePasswordStateChanged(nextState);
    }

    private void HandlePasswordStateChanged(bool enabled)
    {
        hasLocalUnsavedChanges = true;

        bool canUsePassword = enabled && GetPublicStateFromUI();

        if (passwordInput != null)
            passwordInput.interactable = canUsePassword;

        RecalculateDirtyState();
    }

    private void OnPasswordInputChanged(string _)
    {
        if (suppressToggleEvents)
            return;

        RecalculateDirtyState();
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
            if (applyButton != null)
                applyButton.interactable = false;
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
        string passwordHash = WorldRoomProperties.GetString(
            PhotonNetwork.CurrentRoom.CustomProperties,
            WorldRoomProperties.PasswordHash,
            string.Empty);

        suppressToggleEvents = true;

        SetPublicStateInUI(isPublic);

        SetPasswordStateInUI(hasPassword);

        if (passwordStateButton != null)
            passwordStateButton.interactable = isPublic;

        if (passwordInput != null)
            passwordInput.interactable = isPublic && hasPassword;

        baselineIsPublic = isPublic;
        baselineHasPassword = hasPassword;
        baselinePasswordHash = hasPassword ? passwordHash : string.Empty;
        hasBaseline = true;

        suppressToggleEvents = false;
        hasLocalUnsavedChanges = false;
        UpdateApplyButtonState();

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

    private void RecalculateDirtyState()
    {
        if (!hasBaseline)
        {
            hasLocalUnsavedChanges = false;
            UpdateApplyButtonState();
            return;
        }

        GetCurrentUiState(out bool uiIsPublic, out bool uiHasPassword, out string uiPasswordHash);

        hasLocalUnsavedChanges = uiIsPublic != baselineIsPublic
            || uiHasPassword != baselineHasPassword
            || uiPasswordHash != baselinePasswordHash;

        UpdateApplyButtonState();
    }

    private void UpdateApplyButtonState()
    {
        if (applyButton == null)
            return;

        bool canEdit = CanEditPublishState();
        bool panelOpen = optionPanel == null || optionPanel.activeSelf;
        applyButton.interactable = panelOpen && canEdit && hasLocalUnsavedChanges;
    }

    private void GetCurrentUiState(out bool isPublic, out bool hasPassword, out string passwordHash)
    {
        isPublic = GetPublicStateFromUI();
        hasPassword = isPublic && GetPasswordStateFromUI();

        if (hasPassword)
        {
            string plain = passwordInput != null ? (passwordInput.text ?? string.Empty) : string.Empty;
            passwordHash = WorldRoomProperties.ComputeSha256(plain);
        }
        else
        {
            passwordHash = string.Empty;
        }
    }

    private bool GetPublicStateFromUI()
    {
        if (publicStateButton != null)
            return publicButtonState;

        return false;
    }

    private void SetPublicStateInUI(bool isPublic)
    {
        publicButtonState = isPublic;
        UpdatePublicStateButtonText(isPublic);
    }

    private void UpdatePublicStateButtonText(bool isPublic)
    {
        if (publicStateButtonText != null)
        {
            publicStateButtonText.text = isPublic ? "Public" : "Private";
            return;
        }

        if (publicStateButton == null)
            return;

        TextMeshProUGUI embeddedLabel = publicStateButton.GetComponentInChildren<TextMeshProUGUI>();
        if (embeddedLabel != null)
            embeddedLabel.text = isPublic ? "Public" : "Private";
    }

    private bool GetPasswordStateFromUI()
    {
        if (passwordStateButton != null)
            return passwordButtonState;

        return false;
    }

    private void SetPasswordStateInUI(bool hasPassword)
    {
        passwordButtonState = hasPassword;
        UpdatePasswordStateButtonText(hasPassword);
    }

    private void UpdatePasswordStateButtonText(bool hasPassword)
    {
        string label = hasPassword ? "Password: On" : "Password: Off";

        if (passwordStateButtonText != null)
        {
            passwordStateButtonText.text = label;
            return;
        }

        if (passwordStateButton == null)
            return;

        TextMeshProUGUI embeddedLabel = passwordStateButton.GetComponentInChildren<TextMeshProUGUI>();
        if (embeddedLabel != null)
            embeddedLabel.text = label;
    }
}
