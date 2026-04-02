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

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;

    [Header("Positioning")]
    [SerializeField] private Vector2 offset = new Vector2(10, -10);
    [SerializeField] private bool followMouse = true;

    private Canvas parentCanvas;
    private Coroutine fadeCoroutine;

    // Events
    public event Action OnDropRequested;

    #region Unity Lifecycle

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null && detailPanel != null)
            canvasGroup = detailPanel.GetComponent<CanvasGroup>();

        if (panelRectTransform == null && detailPanel != null)
            panelRectTransform = detailPanel.GetComponent<RectTransform>();

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
        RectTransform parentRect = panelRectTransform.parent as RectTransform;

        RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
        Vector2 panelSize = detailRect != null ? detailRect.rect.size : new Vector2(320, 500);
        Vector2 pivot = detailRect != null ? detailRect.pivot : new Vector2(0.5f, 0.5f);
        float scale = parentCanvas.scaleFactor;

        // Calculate all 4 edges of the tooltip in screen space
        // Pivot (0.5, 0.5) means panel extends half size in each direction from offset point
        float leftEdge   = screenPosition.x + (offset.x - panelSize.x * pivot.x) * scale;
        float rightEdge  = screenPosition.x + (offset.x + panelSize.x * (1 - pivot.x)) * scale;
        float bottomEdge = screenPosition.y + (offset.y - panelSize.y * pivot.y) * scale;
        float topEdge    = screenPosition.y + (offset.y + panelSize.y * (1 - pivot.y)) * scale;

        Vector2 adjustedOffset = offset;

        // Overflow right → flip tooltip to the left of cursor
        if (rightEdge > Screen.width)
            adjustedOffset.x = -offset.x;

        // Overflow left → shift right by the overflow amount
        if (leftEdge < 0)
            adjustedOffset.x -= leftEdge / scale;

        // Overflow top → shift down by the overflow amount
        if (topEdge > Screen.height)
            adjustedOffset.y -= (topEdge - Screen.height) / scale;

        // Overflow bottom → shift up by the overflow amount
        if (bottomEdge < 0)
            adjustedOffset.y -= bottomEdge / scale;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screenPosition, cam, out Vector2 localPoint
        );

        localPoint += adjustedOffset;
        panelRectTransform.anchoredPosition = localPoint;
    }

    public void SetItemDetail(ItemDetailData data)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = data.Icon;
            itemIcon.enabled = data.Icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = data.Name;
            itemNameText.color = data.NameColor;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = data.Description;
        }

        if (itemStatsText != null)
        {
            itemStatsText.text = data.Stats;
        }
    }

    #endregion

    #region Helper Methods


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
