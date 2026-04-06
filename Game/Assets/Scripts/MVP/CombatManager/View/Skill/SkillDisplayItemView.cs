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

            // Reduce opacity, disable raycast so drop targets can receive events
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.85f;
                canvasGroup.blocksRaycasts = false;
            }

            // Hide description during drag (mirrors old behavior)
            if (skillDescriptionText != null)
                skillDescriptionText.enabled = false;

            OnBeginDragEvent?.Invoke(this);
            Debug.Log($"[SkillDisplayItemView] Begin drag: {skillData.skillName}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // ✅ Item itself follows mouse - mirrors old SkillDisplayItem.OnDrag
            rectTransform.position = eventData.position;

            OnDragEvent?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;

            // Re-enable raycast FIRST
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            // Return to original position
            rectTransform.localPosition = originalPosition;

            // Restore opacity
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            // Restore description
            if (skillDescriptionText != null)
                skillDescriptionText.enabled = true;

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

            if (skillDescriptionText != null)
                skillDescriptionText.enabled = true;

            if (gridParent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent as RectTransform);

            Debug.Log($"[SkillDisplayItemView] Force reset: {skillData?.skillName}");
        }

        #endregion
    }
}
