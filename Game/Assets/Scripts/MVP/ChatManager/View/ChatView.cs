using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class ChatView : MonoBehaviour
{
    [SerializeField] private GameObject chatPanelUI;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private ChatMessageItemView messagePrefab;
    [SerializeField] private ScrollRect scrollRect;

    private ChatPresenter presenter;
    private bool isChatOpen = false;
    private InputAction _chatEnterAction;
    private InputAction _escCloseAction;

    public void Initialize(ChatPresenter presenter)
    {
        this.presenter = presenter;
        CloseChat();
    }

    private void Start()
    {
        // Anchor content to bottom so messages stack upward
        if (messageContainer is RectTransform rt)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
        }

        var vlg = messageContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
            vlg.childAlignment = TextAnchor.LowerLeft;
    }

    private void Awake()
    {
        _chatEnterAction = new InputAction("ChatEnter", InputActionType.Button, "<Keyboard>/enter");
        _chatEnterAction.performed += OnOpenOrSendChat;

        _escCloseAction = new InputAction("ChatCloseESC", InputActionType.Button, "<Keyboard>/escape");
        _escCloseAction.performed += OnEscPressed;
    }

    private void OnEnable()
    {
        _chatEnterAction?.Enable();
        _escCloseAction?.Enable();
    }

    private void OnDisable()
    {
        _chatEnterAction?.Disable();
        _escCloseAction?.Disable();
    }

    private void OnDestroy()
    {
        if (_chatEnterAction != null)
        {
            _chatEnterAction.performed -= OnOpenOrSendChat;
            _chatEnterAction.Dispose();
            _chatEnterAction = null;
        }
        if (_escCloseAction != null)
        {
            _escCloseAction.performed -= OnEscPressed;
            _escCloseAction.Dispose();
            _escCloseAction = null;
        }
    }

    private void OnOpenOrSendChat(InputAction.CallbackContext ctx)
    {
        if (!isChatOpen)
        {
            OpenChat();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(inputField.text))
                SendMessage();
            else
                CloseChat();
        }
    }

    private void OnEscPressed(InputAction.CallbackContext ctx)
    {
        if (isChatOpen)
            CloseChat();
    }

    private void OpenChat()
    {
        isChatOpen = true;
        chatPanelUI.SetActive(true);
        inputField.ActivateInputField();
        inputField.Select();
        InputManager.Instance?.DisablePlayerActions();
    }

    private void CloseChat()
    {
        isChatOpen = false;
        chatPanelUI.SetActive(false);
        inputField.text = "";
        InputManager.Instance?.EnablePlayerActions();
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
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}
