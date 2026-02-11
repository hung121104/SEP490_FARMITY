using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailView : MonoBehaviour, IItemDetailView
{
    [Header("Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Content")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemStatsText;
    [SerializeField] private GameObject giftReactionPanel;
    [SerializeField] private TextMeshProUGUI giftReactionText;

    [Header("Buttons")]
    [SerializeField] private Button useButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button compareButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    [Header("Positioning")]
    [SerializeField] private Vector2 offset = new Vector2(10, -10);
    [SerializeField] private bool followMouse = true;
    [SerializeField] private bool clampToScreen = true;

    private Canvas parentCanvas;
    private Coroutine fadeCoroutine;

    // Events
    public event Action OnUseRequested;
    public event Action OnDropRequested;
    public event Action OnCompareRequested;

    #region Unity Lifecycle

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null && detailPanel != null)
            canvasGroup = detailPanel.GetComponent<CanvasGroup>();

        if (panelRectTransform == null && detailPanel != null)
            panelRectTransform = detailPanel.GetComponent<RectTransform>();

        InitializeButtons();
        Hide();
    }

    private void Update()
    {
        if (followMouse && detailPanel != null && detailPanel.activeSelf)
        {
            SetPosition(Input.mousePosition);
        }
    }

    #endregion

    #region Initialization

    private void InitializeButtons()
    {
        if (useButton != null)
            useButton.onClick.AddListener(() => OnUseRequested?.Invoke());

        if (dropButton != null)
            dropButton.onClick.AddListener(() => OnDropRequested?.Invoke());

        if (compareButton != null)
            compareButton.onClick.AddListener(() => OnCompareRequested?.Invoke());
    }

    #endregion

    #region IItemDetailView Implementation

    public void Show()
    {
        if (detailPanel == null) return;

        detailPanel.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        if (detailPanel == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOut());

        // Hide gift reaction when hiding
        if (giftReactionPanel != null)
            giftReactionPanel.SetActive(false);
    }

    public void SetPosition(Vector2 screenPosition)
    {
        if (panelRectTransform == null || parentCanvas == null) return;

        // Convert screen position to canvas position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPosition,
            parentCanvas.worldCamera,
            out Vector2 localPoint
        );

        // Apply offset
        localPoint += offset;

        if (clampToScreen)
        {
            localPoint = ClampToCanvas(localPoint);
        }

        panelRectTransform.anchoredPosition = localPoint;
    }

    public void SetItemIcon(Sprite icon)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }
    }

    public void SetItemName(string itemName, Color qualityColor)
    {
        if (itemNameText != null)
        {
            itemNameText.text = itemName;
            itemNameText.color = qualityColor;
        }
    }

    public void SetItemDescription(string description)
    {
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = description;
        }
    }

    public void SetItemStats(string stats)
    {
        if (itemStatsText != null)
        {
            itemStatsText.text = stats;
        }
    }

    public void ShowGiftReaction(string npcName, GiftReaction reaction)
    {
        if (giftReactionPanel == null || giftReactionText == null) return;

        giftReactionPanel.SetActive(true);

        string reactionText = reaction switch
        {
            GiftReaction.Love => $"<color=#FF69B4>❤</color> {npcName} loves this!",
            GiftReaction.Like => $"<color=#90EE90>😊</color> {npcName} likes this",
            GiftReaction.Neutral => $"<color=#D3D3D3>😐</color> {npcName} is neutral",
            GiftReaction.Dislike => $"<color=#FFA500>😕</color> {npcName} dislikes this",
            GiftReaction.Hate => $"<color=#FF4500>😡</color> {npcName} hates this!",
            _ => ""
        };

        giftReactionText.text = reactionText;
    }

    public void SetUseButtonState(bool interactable)
    {
        if (useButton != null)
        {
            useButton.gameObject.SetActive(interactable);
            useButton.interactable = interactable;
        }
    }

    public void SetDropButtonState(bool interactable)
    {
        if (dropButton != null)
        {
            dropButton.gameObject.SetActive(interactable);
            dropButton.interactable = interactable;
        }
    }

    public void SetCompareButtonState(bool visible)
    {
        if (compareButton != null)
        {
            compareButton.gameObject.SetActive(visible);
        }
    }

    #endregion

    #region Helper Methods

    private Vector2 ClampToCanvas(Vector2 localPoint)
    {
        if (parentCanvas == null || panelRectTransform == null) return localPoint;

        Vector2 pivot = panelRectTransform.pivot;
        Vector2 size = panelRectTransform.rect.size;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Vector2 canvasSize = canvasRect.rect.size;

        // Calculate bounds
        float minX = -canvasSize.x / 2 + size.x * pivot.x;
        float maxX = canvasSize.x / 2 - size.x * (1 - pivot.x);
        float minY = -canvasSize.y / 2 + size.y * pivot.y;
        float maxY = canvasSize.y / 2 - size.y * (1 - pivot.y);

        // Clamp position
        localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        return localPoint;
    }

    #endregion

    #region Animation

    private System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    #endregion
}
