using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDeleteView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image trashIconImage;
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.red;
    [SerializeField] private float normalAlpha = 0.6f;
    [SerializeField] private float hoverAlpha = 1f;
    [SerializeField] private float scaleMultiplier = 1.2f;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.2f;

    private Vector3 originalScale;
    private bool isHovering = false;

    // Event
    public event Action<int> OnItemDeleteRequested;

    #region Unity Lifecycle

    private void Awake()
    {
        originalScale = transform.localScale;
        InitializeUI();
        Hide();
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.alpha = normalAlpha;

        if (trashIconImage != null)
            trashIconImage.color = normalColor;

        if (highlightEffect != null)
            highlightEffect.SetActive(false);

        if (hintText != null)
            hintText.text = "Drag here to delete";
    }

    #endregion

    #region IDropHandler Implementation

    public void OnDrop(PointerEventData eventData)
    {
        // Get dragged slot index from event data
        var draggedSlot = eventData.pointerDrag?.GetComponent<InventorySlotView>();

        if (draggedSlot != null)
        {
            var item = draggedSlot.GetCurrentItem();

            if (item != null)
            {
                int slotIndex = draggedSlot.GetSlotIndex();
                
                OnItemDeleteRequested?.Invoke(slotIndex);

                Debug.Log($"[ItemDeleteView] Delete requested for slot {slotIndex}: {item.ItemName}");
            }
        }

        // Reset visual state
        SetHoverState(false);
    }

    #endregion

    #region IPointerEnterHandler & IPointerExitHandler

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check if something is being dragged
        if (eventData.pointerDrag != null)
        {
            SetHoverState(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHoverState(false);
    }

    #endregion

    #region Visual State Management

    private void SetHoverState(bool hovering)
    {
        isHovering = hovering;

        if (trashIconImage != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateColor(hovering ? hoverColor : normalColor));
        }

        if (canvasGroup != null)
        {
            StartCoroutine(AnimateAlpha(hovering ? hoverAlpha : normalAlpha));
        }

        if (highlightEffect != null)
        {
            highlightEffect.SetActive(hovering);
        }

        // Scale animation
        StartCoroutine(AnimateScale(hovering ? originalScale * scaleMultiplier : originalScale));
    }

    private System.Collections.IEnumerator AnimateColor(Color targetColor)
    {
        Color startColor = trashIconImage.color;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            trashIconImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        trashIconImage.color = targetColor;
    }

    private System.Collections.IEnumerator AnimateAlpha(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private System.Collections.IEnumerator AnimateScale(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    #endregion

    public void OnAction() 
    { 
        gameObject.SetActive(!gameObject.activeSelf);
    }

    #region Public API

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    #endregion
}
