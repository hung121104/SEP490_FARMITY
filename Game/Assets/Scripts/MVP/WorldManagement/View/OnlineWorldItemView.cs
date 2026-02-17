using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using System;

public class OnlineWorldItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button joinButton;

    private string roomName;
    private Action<string> onJoinCallback;

    public void Initialize(RoomInfo roomInfo, Action<string> joinCallback)
    {
        roomName = roomInfo.Name;
        onJoinCallback = joinCallback;

        if (roomNameText != null)
        {
            // Use displayName from custom properties, fallback to room name
            string displayName = roomInfo.Name;
            if (roomInfo.CustomProperties != null && roomInfo.CustomProperties.ContainsKey("displayName"))
            {
                displayName = roomInfo.CustomProperties["displayName"].ToString();
            }
            roomNameText.text = displayName;
        }

        if (playerCountText != null)
            playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";

        if (joinButton != null)
        {
            bool canJoin = roomInfo.IsOpen && roomInfo.PlayerCount < roomInfo.MaxPlayers;
            joinButton.interactable = canJoin;
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => onJoinCallback?.Invoke(roomName));
        }
    }
}
