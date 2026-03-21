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
    /// Sits on SkillHotbarSlot prefab.
    /// Handles: display, drop from ManagementPanel,
    /// drag to swap/unequip (only when panel is open).
    /// </summary>
    public class SkillHotbarSlotView : MonoBehaviour,
        IDropHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private Image skillIconImage;
        [SerializeField] private TextMeshProUGUI hotkeyLabel;
        [SerializeField] private Image cooldownFill;

        // Slot identity
        private int slotIndex = -1;
        private SkillData currentSkillData;
        private SkillHotbarModel model;

        // Drag state (for swap/unequip)
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalLocalPosition;
        private Transform hotbarParent;
        private bool isDragging = false;

        // Events → Presenter listens
        public System.Action<int, SkillData> OnDroppedOnSlot;       // from ManagementPanel drag
        public System.Action<int, int> OnSlotSwapRequested;          // slot → slot drag
        public System.Action<int> OnSlotUnequipRequested;            // slot → outside drag
        public System.Action<int> OnSlotHoverEnter;
        public System.Action<int> OnSlotHoverExit;

        #region Setup

        public void Initialize(int index, SkillHotbarModel hotbarModel)
        {
            slotIndex = index;
            model = hotbarModel;

            rectTransform = GetComponent<RectTransform>();
            hotbarParent = transform.parent;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Set hotkey label
            if (hotkeyLabel != null && index < hotbarModel.activationKeys.Length)
                hotkeyLabel.text = (index + 1).ToString();

            SetEmptyVisual();
        }

        #endregion

        #region Display

        public void RefreshDisplay(SkillData skillData)
        {
            currentSkillData = skillData;

            if (skillData == null)
            {
                SetEmptyVisual();
                return;
            }

            SetOccupiedVisual(skillData);
        }

        private void SetEmptyVisual()
        {
            if (skillIconImage != null)
            {
                skillIconImage.sprite = null;
                skillIconImage.color = Color.clear;
                skillIconImage.enabled = false;
            }

            // ✅ Don't touch slotBackground color - keep it as designed in prefab
            // slotBackground is the root Image the user styled themselves

            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = 0f;
                cooldownFill.color = Color.clear;
            }
        }

        private void SetOccupiedVisual(SkillData skillData)
        {
            if (skillIconImage != null)
            {
                skillIconImage.sprite = skillData.skillIcon;
                bool hasIcon = skillData.skillIcon != null;
                skillIconImage.color = hasIcon ? Color.white : Color.clear;
                skillIconImage.enabled = hasIcon;
            }

            // ✅ Don't touch slotBackground color - keep it as designed in prefab

            if (cooldownFill != null)
            {
                cooldownFill.fillAmount = 0f;
                cooldownFill.color = new Color(0f, 0f, 0f, 0.6f);
            }
        }

        public void UpdateCooldownFill(float fillAmount)
        {
            if (cooldownFill == null) return;

            cooldownFill.fillAmount = Mathf.Clamp01(fillAmount);

            // ✅ Only show fill overlay when actually cooling down
            cooldownFill.color = fillAmount > 0f
                ? new Color(0f, 0f, 0f, 0.6f)
                : Color.clear;
        }

        #endregion

        #region IDropHandler - Receive from ManagementPanel

        public void OnDrop(PointerEventData eventData)
        {
            // Case 1: Drop from ManagementPanel (SkillDisplayItemView)
            SkillDisplayItemView draggedItem = eventData.pointerDrag?.GetComponent<SkillDisplayItemView>();
            if (draggedItem != null)
            {
                SkillData droppedSkill = draggedItem.GetSkillData();
                if (droppedSkill != null)
                {
                    OnDroppedOnSlot?.Invoke(slotIndex, droppedSkill);
                    Debug.Log($"[SkillHotbarSlotView] Slot {slotIndex} received: {droppedSkill.skillName} from ManagementPanel");
                }
                return;
            }

            // Case 2: Drop from another hotbar slot (swap)
            SkillHotbarSlotView draggedSlot = eventData.pointerDrag?.GetComponent<SkillHotbarSlotView>();
            if (draggedSlot != null && draggedSlot != this)
            {
                OnSlotSwapRequested?.Invoke(draggedSlot.slotIndex, slotIndex);
                Debug.Log($"[SkillHotbarSlotView] Swap requested: slot {draggedSlot.slotIndex} ↔ slot {slotIndex}");
            }
        }

        #endregion

        #region Pointer Hover

        public void OnPointerEnter(PointerEventData eventData)
        {
            // ✅ Only highlight if we have a background reference AND slot is empty
            if (slotBackground != null && currentSkillData == null)
                slotBackground.color = model != null ? model.hoveredSlotColor : Color.green;

            OnSlotHoverEnter?.Invoke(slotIndex);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // ✅ Restore to original prefab color (white, full alpha) not emptySlotColor
            if (slotBackground != null && currentSkillData == null)
                slotBackground.color = Color.white;

            OnSlotHoverExit?.Invoke(slotIndex);
        }

        #endregion

        #region Drag (Swap / Unequip) - Only when panel is open

        public void OnBeginDrag(PointerEventData eventData)
        {
            // ✅ Block drag if ManagementPanel is closed
            if (!IsManagementPanelOpen())
            {
                eventData.pointerDrag = null;
                return;
            }

            // ✅ Block drag if slot is empty
            if (currentSkillData == null)
            {
                eventData.pointerDrag = null;
                return;
            }

            isDragging = true;
            originalLocalPosition = rectTransform.localPosition;
            hotbarParent = transform.parent;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = model != null ? 0.6f : 0.6f;
                canvasGroup.blocksRaycasts = false;
            }

            Debug.Log($"[SkillHotbarSlotView] Begin drag slot {slotIndex}: {currentSkillData?.skillName}");
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

            // Re-enable raycast
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            // Return to original position
            rectTransform.localPosition = originalLocalPosition;

            // Check if dropped OUTSIDE any slot → unequip
            if (eventData.pointerCurrentRaycast.gameObject == null
                || eventData.pointerCurrentRaycast.gameObject.GetComponent<SkillHotbarSlotView>() == null
                && eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<SkillHotbarSlotView>() == null)
            {
                // Make sure it wasn't dropped on a ManagementPanel item either
                if (eventData.pointerCurrentRaycast.gameObject == null
                    || eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<SkillDisplayItemView>() == null)
                {
                    OnSlotUnequipRequested?.Invoke(slotIndex);
                    Debug.Log($"[SkillHotbarSlotView] Slot {slotIndex} unequip requested");
                }
            }

            if (hotbarParent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(hotbarParent as RectTransform);
        }

        #endregion

        #region Helpers

        private bool IsManagementPanelOpen()
        {
            var panel = CombatManager.Presenter.SkillManagementPresenter.Instance;
            return panel != null && panel.IsPanelOpen();
        }

        #endregion

        #region Public API

        public int SlotIndex => slotIndex;
        public SkillData CurrentSkillData => currentSkillData;
        public bool IsDragging => isDragging;

        public void ForceResetDragState()
        {
            if (!isDragging) return;
            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (rectTransform != null)
                rectTransform.localPosition = originalLocalPosition;
        }

        #endregion
    }
}