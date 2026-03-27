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
    [SerializeField] private GameObject giftReactionPanel;
    
    [Header("Content")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemStatsText;

    [Header("Buttons")]
    [SerializeField] private Button useButton;

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

        if (!gameObject.activeInHierarchy || !enabled)
        {
            // Can not run coroutine, hide immediately
            HideImmediate();
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOut());
    }

    public void HideImmediate()
    {
        if (detailPanel == null) return;

        // Stop coroutine if running
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // Set alpha to 0 immediately
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        detailPanel.SetActive(false);

        // Hide gift reaction
        if (giftReactionPanel != null)
            giftReactionPanel.SetActive(false);
    }

    public void SetPosition(Vector2 screenPosition)
    {
        if (panelRectTransform == null || parentCanvas == null || panelRectTransform.parent == null) return;

        Camera cam = parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

        // Convert screen position to local position of whatever PARENT it currently belongs to
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRectTransform.parent as RectTransform,
            screenPosition,
            cam,
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

    public void SetUseButtonState(bool interactable)
    {
        if (useButton != null)
        {
            useButton.gameObject.SetActive(interactable);
            useButton.interactable = interactable;
        }
    }

    #endregion

    #region Helper Methods

    private Vector2 ClampToCanvas(Vector2 localPoint)
    {
        if (parentCanvas == null || panelRectTransform == null || panelRectTransform.parent == null) return localPoint;

        Vector2 pivot = panelRectTransform.pivot;
        Vector2 size = panelRectTransform.rect.size;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        RectTransform parentRect = panelRectTransform.parent as RectTransform;

        // Find the canvas corners in world space
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        
        // Convert bottom-left (0) and top-right (2) corners from world to parent local space
        Vector3 bottomLeftLocal = parentRect.InverseTransformPoint(canvasCorners[0]);
        Vector3 topRightLocal = parentRect.InverseTransformPoint(canvasCorners[2]);

        // Calculate bounds in local space relative to the panel's pivot/size
        float minX = bottomLeftLocal.x + size.x * pivot.x;
        float maxX = topRightLocal.x - size.x * (1 - pivot.x);
        float minY = bottomLeftLocal.y + size.y * pivot.y;
        float maxY = topRightLocal.y - size.y * (1 - pivot.y);

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
