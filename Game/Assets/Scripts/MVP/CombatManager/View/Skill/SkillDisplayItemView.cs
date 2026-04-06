using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CombatManager.Model;

namespace CombatManager.View
{
    /// <summary>
    /// View for a single skill item in SkillManagementPanel.
    /// Sits on SkillDisplayItem prefab.
    /// Drag behavior: item itself moves with mouse (mirrors old SkillDisplayItem).
    /// </summary>
    public class SkillDisplayItemView : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References - Assign in Inspector")]
        [SerializeField] private Image skillIcon;
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI skillDescriptionText;
        [SerializeField] private Button selectButton;

        // Data
        private SkillData skillData;
        private bool isDragging = false;

        // Drag state - mirrors old SkillDisplayItem
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalPosition;
        private Transform gridParent;
        private RectTransform dragVisualRect;
        private Image dragVisualImage;
        private Canvas dragRootCanvas;

        // Events → Presenter listens
        public System.Action<SkillDisplayItemView> OnBeginDragEvent;
        public System.Action<SkillDisplayItemView> OnDragEvent;
        public System.Action<SkillDisplayItemView> OnEndDragEvent;
        public System.Action<SkillDisplayItemView> OnSelectEvent;
        public System.Action<SkillDisplayItemView, Vector2> OnHoverEnterEvent;
        public System.Action<SkillDisplayItemView> OnHoverExitEvent;

        #region Setup

        public void Initialize(SkillData data)
        {
            skillData = data;

            EnsureRaycastTarget();

            // Setup components
            rectTransform = GetComponent<RectTransform>();
            gridParent = transform.parent;

            // Setup CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Store original position
            originalPosition = rectTransform.localPosition;

            RefreshDisplay();

            if (selectButton != null)
            {
                // Root view handles click/drag. Make child button non-blocking for pointer events.
                selectButton.onClick.RemoveAllListeners();
                if (selectButton.targetGraphic != null)
                    selectButton.targetGraphic.raycastTarget = false;
            }
        }

        private void RefreshDisplay()
        {
            if (skillData == null) return;

            if (skillIcon != null)
            {
                // No fallback icon: keep slot visibly empty when icon is missing.
                skillIcon.sprite = skillData.skillIcon;
                bool hasIcon = skillData.skillIcon != null;
                skillIcon.enabled = hasIcon;
                skillIcon.color = hasIcon ? Color.white : Color.clear;
            }

            if (skillNameText != null)
                skillNameText.text = skillData.skillName;

            if (skillDescriptionText != null)
                skillDescriptionText.text = skillData.skillDescription;
        }

        private void EnsureRaycastTarget()
        {
            Graphic rootGraphic = GetComponent<Graphic>();
            if (rootGraphic != null)
            {
                rootGraphic.raycastTarget = true;
                return;
            }

            // Add invisible raycast receiver so pointer enter/exit and click can work reliably.
            Image raycastImage = gameObject.AddComponent<Image>();
            raycastImage.color = new Color(0f, 0f, 0f, 0f);
            raycastImage.raycastTarget = true;
        }

        #endregion

        #region Drag Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (skillData == null) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            isDragging = true;

            // Store current position as original (grid may have shifted)
            originalPosition = rectTransform.localPosition;
            gridParent = transform.parent;

            CreateDragVisual();
            UpdateDragVisual(eventData.position);

            // Keep this item anchored in the grid but dim it to indicate active drag source.
            ApplyDraggingSourceVisual();

            // Reduce opacity, disable raycast so drop targets can receive events
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.6f;
                canvasGroup.blocksRaycasts = false;
            }

            OnBeginDragEvent?.Invoke(this);
            Debug.Log($"[SkillDisplayItemView] Begin drag: {skillData.skillName}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Floating preview follows mouse while source item stays anchored.
            UpdateDragVisual(eventData.position);

            OnDragEvent?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;

            // Re-enable raycast FIRST
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            // Restore opacity
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            DestroyDragVisual();
            RefreshDisplay();

            // Rebuild grid layout
            if (gridParent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent as RectTransform);

            OnEndDragEvent?.Invoke(this);
            Debug.Log($"[SkillDisplayItemView] End drag: {skillData.skillName}");
        }

        #endregion

        #region Button

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (isDragging) return;
            OnSelectClicked();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (skillData == null)
            {
                Debug.Log("[SkillDisplayItemView] Hover enter ignored: skillData is null.");
                return;
            }

            if (isDragging)
            {
                Debug.Log($"[SkillDisplayItemView] Hover enter ignored while dragging: {skillData.skillName}");
                return;
            }

            Debug.Log($"[SkillDisplayItemView] Hover enter: {skillData.skillName} at {eventData.position}");
            OnHoverEnterEvent?.Invoke(this, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (skillData == null)
            {
                Debug.Log("[SkillDisplayItemView] Hover exit ignored: skillData is null.");
                return;
            }

            Debug.Log($"[SkillDisplayItemView] Hover exit: {skillData.skillName}");
            OnHoverExitEvent?.Invoke(this);
        }

        private void OnSelectClicked()
        {
            OnSelectEvent?.Invoke(this);
        }

        #endregion

        #region Public API

        public SkillData GetSkillData() => skillData;
        public bool IsDragging => isDragging;

        public void ForceResetState()
        {
            if (!isDragging) return;

            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (rectTransform != null)
                rectTransform.localPosition = originalPosition;

            DestroyDragVisual();
            RefreshDisplay();

            if (gridParent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent as RectTransform);

            Debug.Log($"[SkillDisplayItemView] Force reset: {skillData?.skillName}");
        }

        private void ApplyDraggingSourceVisual()
        {
            if (skillIcon != null && skillIcon.sprite != null)
                skillIcon.color = new Color(1f, 1f, 1f, 0.8f);

            if (skillNameText != null)
                skillNameText.alpha = 0.85f;

            if (skillDescriptionText != null)
                skillDescriptionText.alpha = 0.85f;
        }

        private void CreateDragVisual()
        {
            if (dragVisualRect != null)
                return;

            if (skillData == null)
                return;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
                return;

            dragRootCanvas = parentCanvas.rootCanvas != null ? parentCanvas.rootCanvas : parentCanvas;

            GameObject visualGO = Instantiate(gameObject, dragRootCanvas.transform);
            visualGO.name = $"DraggedSkillItem_{skillData.skillName}";
            dragVisualRect = visualGO.GetComponent<RectTransform>();
            dragVisualImage = visualGO.GetComponent<Image>();

            SkillDisplayItemView visualView = visualGO.GetComponent<SkillDisplayItemView>();
            if (visualView != null)
                visualView.enabled = false;

            Button[] buttons = visualGO.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
                button.interactable = false;

            Graphic[] graphics = visualGO.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
                graphic.raycastTarget = false;

            CanvasGroup visualCanvasGroup = visualGO.GetComponent<CanvasGroup>();
            if (visualCanvasGroup == null)
                visualCanvasGroup = visualGO.AddComponent<CanvasGroup>();

            dragVisualRect.SetAsLastSibling();
            dragVisualRect.anchorMin = new Vector2(0.5f, 0.5f);
            dragVisualRect.anchorMax = new Vector2(0.5f, 0.5f);
            dragVisualRect.pivot = new Vector2(0.5f, 0.5f);

            if (rectTransform != null)
                dragVisualRect.sizeDelta = rectTransform.rect.size;

            visualCanvasGroup.blocksRaycasts = false;
            visualCanvasGroup.interactable = false;
            visualCanvasGroup.alpha = 0.95f;
        }

        private void UpdateDragVisual(Vector2 screenPosition)
        {
            if (dragVisualRect == null || dragRootCanvas == null)
                return;

            Camera cam = dragRootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : dragRootCanvas.worldCamera;

            RectTransform rootRect = dragRootCanvas.transform as RectTransform;
            if (rootRect == null)
            {
                dragVisualRect.position = screenPosition;
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootRect, screenPosition, cam, out Vector3 worldPoint))
                dragVisualRect.position = worldPoint;
        }

        private void DestroyDragVisual()
        {
            if (dragVisualRect != null)
                Destroy(dragVisualRect.gameObject);

            dragVisualRect = null;
            dragVisualImage = null;
            dragRootCanvas = null;
        }

        #endregion
    }
}
