using UnityEngine;

public class ChatInstaller : MonoBehaviour
{
    [SerializeField] private ChatView chatView;
    [SerializeField] private ChatService chatService;

    private void Awake()
    {
        var presenter = new ChatPresenter(chatService, chatView);
        chatView.Initialize(presenter);
    }
}
