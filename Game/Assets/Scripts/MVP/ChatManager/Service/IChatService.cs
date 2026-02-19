using System;

public interface IChatService
{
    void SendMessage(string content);

    event Action<ChatMessageModel> OnMessageReceived;
}
