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
    [SerializeField] private Button applyButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        if (openPanelButton != null)
            openPanelButton.onClick.AddListener(OpenPanel);

        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(ClosePanel);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyPublishSettings);

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
    }

    public override void OnJoinedRoom()
    {
        RefreshFromRoom();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        RefreshFromRoom();
    }

    public void OpenPanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(true);

        RefreshFromRoom();
    }

    public void ClosePanel()
    {
        if (optionPanel != null)
            optionPanel.SetActive(false);
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

        PhotonNetwork.CurrentRoom.IsVisible = makePublic;
        PhotonNetwork.CurrentRoom.IsOpen = makePublic;

        var props = new Hashtable
        {
            { WorldRoomProperties.IsPublic, makePublic }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        SetStatus(makePublic ? "World is now public." : "World is now private.");
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

        if (publicToggle != null)
            publicToggle.isOn = isPublic;

        SetStatus(isPublic ? "Current: Public" : "Current: Private");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log("[WorldPublishController] " + message);
    }
}
