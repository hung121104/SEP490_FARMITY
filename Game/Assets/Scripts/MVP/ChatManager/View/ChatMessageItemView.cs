using UnityEngine;
using TMPro;

public class ChatMessageItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private TMP_Text timeText;

    public void Setup(ChatMessageModel message)
    {
        senderText.text = message.SenderName;
        contentText.text = message.Content;
        timeText.text = message.TimeStamp.ToString("HH:mm");
    }
}
