using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateAndJoinRoom : MonoBehaviourPunCallbacks
{
    [SerializeField] private InputField createInput;
    [SerializeField] private InputField joinInput;
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private TMP_Dropdown regionDropdown;
    [SerializeField] private TextMeshProUGUI statusText;

    public List<string> regions = new List<string> { "hk", "asia", "eu", "us", "jp", "kr", "au" };

    bool pendingReconnect = false;

    void Start()
    {
        if (regionDropdown != null)
        {
            regionDropdown.ClearOptions();
            regionDropdown.AddOptions(regions);
            regionDropdown.onValueChanged.AddListener(OnRegionChanged);

            string current = PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion;
            if (string.IsNullOrEmpty(current)) current = PhotonNetwork.CloudRegion;
            if (!string.IsNullOrEmpty(current))
            {
                int idx = regions.IndexOf(current);
                if (idx >= 0) regionDropdown.value = idx;
            }
        }
    }

    void OnRegionChanged(int index)
    {
        string selectedRegion = regions[index];
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = selectedRegion;

        if (PhotonNetwork.IsConnected)
        {
            pendingReconnect = true;
            PhotonNetwork.Disconnect();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        if (pendingReconnect)
        {
            pendingReconnect = false;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        string roomName = createInput.text;
        if (!string.IsNullOrEmpty(roomName))
        {
            SetPlayerName();
            UpdateStatus($"Creating room: {roomName}...");
            PhotonNetwork.CreateRoom(roomName);
        }
    }

    public void JoinRoom()
    {
        string roomName = joinInput.text;
        if (!string.IsNullOrEmpty(roomName))
        {
            SetPlayerName();
            UpdateStatus($"Joining room: {roomName}...");
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    private void SetPlayerName()
    {
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            PhotonNetwork.NickName = playerNameInput.text;
        }
    }

    public override void OnJoinedRoom()
    {
        // With AutomaticallySyncScene = true, only the master calls LoadLevel.
        // Non-master clients are synced automatically by PUN.
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.AutomaticallySyncScene)
            PhotonNetwork.LoadLevel("GameCoreTestScene");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        else
            Debug.Log($"[CreateAndJoinRoom] {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[CreateAndJoinRoom] OnJoinRoomFailed: ({returnCode}) {message}");
        UpdateStatus("Join failed: " + message);
    }
}
