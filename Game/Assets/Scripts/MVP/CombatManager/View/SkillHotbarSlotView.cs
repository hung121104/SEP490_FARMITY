using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CombatManager.SO;
using CombatManager.Model;

namespace CombatManager.View
{
    /// <summary>
    /// View for a single SkillHotbar slot.
    /// Mirrors SkillHotbarSlot from CombatSystem (kept for legacy).
    /// Sits on each SkillSlot prefab instance in canvas.
    /// Handles: display, drag, drop, hover visuals.
    /// NO logic - fires events to SkillHotbarPresenter.
    /// </summary>
    public class SkillHotbarSlotView : MonoBehaviour,
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image skillIconImage;
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private TextMeshProUGUI hotkeyLabel;
        [SerializeField] private Image slotBackground;

        // Runtime
        private int slotIndex;
        private SkillData equippedSkill;
        private bool isHovering = false;
        private bool isDragging = false;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 originalLocalPosition;
        private Vector3 dragStartWorldPosition;
        private Transform hotbarParent;

        // Events → Presenter listens
        public System.Action<SkillHotbarSlotView, SkillData> OnDropFromPanelEvent;
        public System.Action<SkillHotbarSlotView, SkillHotbarSlotView> OnDropFromSlotEvent;
        public System.Action<SkillHotbarSlotView> OnUnequipEvent;
        public System.Action<SkillHotbarSlotView> OnBeginDragEvent;
        public System.Action<SkillHotbarSlotView> OnPointerEnterEvent;
        public System.Action<SkillHotbarSlotView> OnPointerExitEvent;

        #region Initialization

        public void Initialize(int index, KeyCode hotkey, SkillHotbarModel model)
        {
            slotIndex = index;

            rectTransform = GetComponent<RectTransform>();
            hotbarParent = transform.parent;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            originalLocalPosition = rectTransform.localPosition;

            if (hotkeyLabel != null)
                hotkeyLabel.text = hotkey.ToString().Replace("Alpha", "");

            SetEmptyVisual(model);

            Debug.Log($"[SkillHotbarSlotView] Slot {index} initialized");
        }

        #endregion

        #region Display

        public void RefreshDisplay(SkillData skillData, SkillHotbarModel model)
        {
            equippedSkill = skillData;

            if (skillData == null)
            {
                SetEmptyVisual(model);
                return;
            }

            if (skillIconImage != null)
            {
                skillIconImage.sprite = skillData.skillIcon;
                skillIconImage.color = Color.white;
                skillIconImage.enabled = true;
            }

            if (slotBackground != null)
                slotBackground.color = model.occupiedSlotColor;

            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = 0f;
                cooldownFillImage.color = Color.white;
            }
        }

        private void SetEmptyVisual(SkillHotbarModel model)
        {
            if (skillIconImage != null)
            {
                skillIconImage.sprite = null;
                skillIconImage.color = Color.clear;
                skillIconImage.enabled = false;
            }

            if (slotBackground != null)
                slotBackground.color = model.emptySlotColor;

            if (cooldownFillImage != null)
            {
                cooldownFillImage.fillAmount = 0f;
                cooldownFillImage.color = Color.clear;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void UpdateCooldown(float fillAmount)
        {
            if (cooldownFillImage == null) return;
            cooldownFillImage.fillAmount = 1f - fillAmount;

            if (skillIconImage != null)
                skillIconImage.color = fillAmount < 0.999f
                    ? new Color(0.6f, 0.6f, 0.6f, 1f)
                    : Color.white;
        }

        public void SetHighlight(bool highlight, SkillHotbarModel model)
        {
            if (slotBackground == null) return;
            slotBackground.color = highlight ? model.hoverColor
                : (equippedSkill != null ? model.occupiedSlotColor : model.emptySlotColor);
        }

        #endregion

        #region Drop Handler

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            // Drop from SkillManagementPanel item
            SkillDisplayItemView panelItem =
                eventData.pointerDrag.GetComponent<SkillDisplayItemView>();
            if (panelItem != null)
            {
                OnDropFromPanelEvent?.Invoke(this, panelItem.GetSkillData());
                return;
            }

            // Drop from another hotbar slot
            SkillHotbarSlotView otherSlot =
                eventData.pointerDrag.GetComponent<SkillHotbarSlotView>();
            if (otherSlot != null && otherSlot != this)
            {
                OnDropFromSlotEvent?.Invoke(this, otherSlot);
                return;
            }
        }

        #endregion

        #region Drag Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            // ✅ Block if management panel not open
            if (CombatManager.Presenter.SkillManagementPresenter.Instance == null
                || !CombatManager.Presenter.SkillManagementPresenter.Instance.IsPanelOpen())
            {
                eventData.pointerDrag = null;
                return;
            }

            // Can't drag empty slot
            if (equippedSkill == null)
            {
                eventData.pointerDrag = null;
                return;
            }

            isDragging = true;
            originalLocalPosition = rectTransform.localPosition;
            dragStartWorldPosition = rectTransform.position;
            hotbarParent = transform.parent;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.6f;
                canvasGroup.blocksRaycasts = false;
            }

            if (slotBackground != null)
                slotBackground.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);

            OnBeginDragEvent?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            // Re-enable raycasts
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // Check unequip distance
            float distance = Vector3.Distance(dragStartWorldPosition, eventData.position);
            if (distance > 150f)
            {
                OnUnequipEvent?.Invoke(this);
            }
            else
            {
                // Return to original position
                rectTransform.localPosition = originalLocalPosition;
            }

            // Rebuild layout
            if (hotbarParent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(hotbarParent as RectTransform);
        }

        #endregion

        #region Pointer Hover

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            OnPointerEnterEvent?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            OnPointerExitEvent?.Invoke(this);
        }

        #endregion

        #region Public API

        public int GetSlotIndex() => slotIndex;
        public SkillData GetEquippedSkill() => equippedSkill;
        public bool IsDragging => isDragging;
        public bool IsHovering => isHovering;

        public void ForceReturnToPosition()
        {
            if (rectTransform != null)
                rectTransform.localPosition = originalLocalPosition;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            isDragging = false;
        }

        #endregion
    }
}