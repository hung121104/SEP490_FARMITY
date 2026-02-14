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
            roomNameText.text = roomInfo.Name;

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
