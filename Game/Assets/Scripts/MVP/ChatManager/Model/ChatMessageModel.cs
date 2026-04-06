using System;

[Serializable]
public class ChatMessageModel
{
    public string SenderName { get; private set; }
    public string Content { get; private set; }
    public DateTime TimeStamp { get; private set; }

    public ChatMessageModel(string senderName, string content)
    {
        SenderName = senderName;
        Content = content;
        TimeStamp = DateTime.Now;
    }
}
