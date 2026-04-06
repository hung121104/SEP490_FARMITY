using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentText.rectTransform);

        float contentHeight = contentText.preferredHeight;

        float padding = 6f;
        float minHeight = 22f;

        float finalHeight = Mathf.Max(contentHeight + padding, minHeight);

        RectTransform rt = GetComponent<RectTransform>();
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
    }
}

