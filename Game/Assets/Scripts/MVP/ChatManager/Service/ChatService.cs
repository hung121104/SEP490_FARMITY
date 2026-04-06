using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class ChatService : MonoBehaviourPunCallbacks, IChatService
{
    private const byte CHAT_EVENT_CODE = 150;

    public event System.Action<ChatMessageModel> OnMessageReceived;

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
    }

    public void SendMessage(string content)
    {
        if (!PhotonNetwork.IsConnected) return;
        if (string.IsNullOrWhiteSpace(content)) return;

        object[] data = new object[]
        {
            PhotonNetwork.NickName,
            content
        };

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All
        };

        PhotonNetwork.RaiseEvent(
            CHAT_EVENT_CODE,
            data,
            options,
            SendOptions.SendReliable
        );
    }



    private void OnPhotonEvent(EventData photonEvent)
    {
        if (photonEvent.Code != CHAT_EVENT_CODE) return;

        object[] data = (object[])photonEvent.CustomData;

        string senderName = (string)data[0];
        string content = (string)data[1];

        ChatMessageModel message = new ChatMessageModel(senderName, content);

        OnMessageReceived?.Invoke(message);
    }


}
