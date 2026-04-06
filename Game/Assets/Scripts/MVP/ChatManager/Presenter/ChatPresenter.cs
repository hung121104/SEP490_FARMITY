public class ChatPresenter
{
    private readonly IChatService chatService;
    private readonly ChatView chatView;

    public ChatPresenter(IChatService chatService, ChatView chatView)
    {
        this.chatService = chatService;
        this.chatView = chatView;

        chatService.OnMessageReceived += HandleMessageReceived;
    }

    public void SendMessage(string content)
    {
        chatService.SendMessage(content);
    }

    private void HandleMessageReceived(ChatMessageModel message)
    {
        chatView.DisplayMessage(message);
    }
}
