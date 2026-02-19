using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ChatView : MonoBehaviour
{
    [SerializeField] private GameObject chatPanelUI;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private ChatMessageItemView messagePrefab;

    private ChatPresenter presenter;
    private bool isChatOpen = false;

    public void Initialize(ChatPresenter presenter)
    {
        this.presenter = presenter;
        CloseChat();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!isChatOpen)
            {
                OpenChat();
            }
            else
            {
                SendMessage();
            }
        }

        if (isChatOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
    }



    private void OpenChat()
    {
        isChatOpen = true;
        chatPanelUI.SetActive(true);

        inputField.ActivateInputField();
        inputField.Select();
    }

    private void CloseChat()
    {
        isChatOpen = false;
        chatPanelUI.SetActive(false);
        inputField.text = "";
    }


    private void SendMessage()
    {
        if (presenter == null) return;
        if (string.IsNullOrWhiteSpace(inputField.text)) return;

        presenter.SendMessage(inputField.text);
        inputField.text = "";
        inputField.ActivateInputField();
    }



    public void DisplayMessage(ChatMessageModel message)
    {
        var item = Instantiate(messagePrefab, messageContainer);
        item.Setup(message);
    }
}
