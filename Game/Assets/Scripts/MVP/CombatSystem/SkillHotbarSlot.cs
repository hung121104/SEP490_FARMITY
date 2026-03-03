using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Represents a single slot in the skill hotbar.
/// Can receive dropped skills from SkillManagementPanel.
/// Can be dragged to switch/swap with other slots or unequip.
/// Displays the equipped skill icon and cooldown.
/// Handles skill execution via key press.
/// 
/// Attached to individual slot GameObjects in the hotbar.
/// (SkillSlot1, SkillSlot2, etc.)
/// </summary>
public class SkillHotbarSlot : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    #region Editor Fields

    [Header("Slot Configuration")]
    [SerializeField] private int slotIndex = 0;
    [SerializeField] private KeyCode activationKey = KeyCode.Alpha1;

    [Header("UI References")]
    [SerializeField] private Image skillIconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private TextMeshProUGUI hotkeyLabel;
    [SerializeField] private Image slotBackground;

    [Header("Visual Settings")]
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color occupiedSlotColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.7f, 1f);
    [SerializeField] private Color dragColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);

    [Header("Drag Settings")]
    [SerializeField] private float unequipDistance = 300f;

    #endregion

    #region Private Fields

    private SkillData equippedSkill = null;
    private SkillBase equippedSkillComponent = null;
    private bool isHovering = false;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private SkillHotbarSlot dragSourceSlot = null;

    #endregion

    #region Initialization

    private void Start()
    {
        SetupComponents();
        SetupUI();
        SetEmptyState();
    }

    private void SetupComponents()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        originalPosition = rectTransform.localPosition;
    }

    private void SetupUI()
    {
        if (hotkeyLabel != null)
        {
            hotkeyLabel.text = activationKey.ToString().Replace("Alpha", "");
        }
    }

    #endregion

    #region Input Handling

    private void Update()
    {
        if (!CombatModeManager.Instance.IsCombatModeActive)
            return;

        // Only trigger skill if we're not dragging the hotbar
        if (!isDragging && Input.GetKeyDown(activationKey))
        {
            TryExecuteSkill();
        }

        // Update cooldown visual every frame
        UpdateCooldownVisual();
    }

    private void TryExecuteSkill()
    {
        if (equippedSkillComponent == null)
        {
            Debug.LogWarning($"[SkillHotbarSlot {slotIndex}] No skill equipped");
            return;
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Executing skill: {equippedSkill.skillName}");
        
        // Call the skill's public TriggerSkill method through hotbar
        equippedSkillComponent.TriggerSkill();
    }

    #endregion

    #region Cooldown Update

    private void UpdateCooldownVisual()
    {
        if (equippedSkillComponent == null || cooldownFillImage == null)
            return;

        float cooldownPercent = equippedSkillComponent.GetSkillCooldownPercent();
        cooldownFillImage.fillAmount = 1f - cooldownPercent;

        if (skillIconImage != null)
        {
            bool isOnCooldown = cooldownPercent < 0.999f;
            skillIconImage.color = isOnCooldown 
                ? new Color(0.6f, 0.6f, 0.6f, 1f) 
                : Color.white;
        }
    }

    #endregion

    #region Drop Handler (From SkillManagementPanel)

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[SkillHotbarSlot {slotIndex}] OnDrop called");

        // Check if skill panel is active
        if (!IsSkillPanelActive())
        {
            Debug.LogWarning("[SkillHotbarSlot] Skill management panel not active!");
            return;
        }

        // Get the dragged SkillDisplayItem from panel
        SkillDisplayItem draggedItem = eventData.pointerDrag?.GetComponent<SkillDisplayItem>();
        
        if (draggedItem != null)
        {
            // Drag from panel - simple equip
            SkillData droppedSkill = draggedItem.GetSkillData();
            if (droppedSkill != null)
            {
                EquipSkill(droppedSkill);
                Debug.Log($"[SkillHotbarSlot {slotIndex}] Equipped from panel: {droppedSkill.skillName}");
            }
            return;
        }

        // Get the dragged SkillHotbarSlot (slot-to-slot drag)
        SkillHotbarSlot draggedSlot = eventData.pointerDrag?.GetComponent<SkillHotbarSlot>();
        
        if (draggedSlot != null && draggedSlot != this)
        {
            // Swap skills between slots
            SwapSkills(draggedSlot);
            Debug.Log($"[SkillHotbarSlot {slotIndex}] Swapped with slot {draggedSlot.slotIndex}");
        }
    }

    #endregion

    #region Drag Handlers (Hotbar Slot Dragging)

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Can only drag if skill panel is active
        if (!IsSkillPanelActive())
        {
            Debug.LogWarning("[SkillHotbarSlot] Cannot drag - skill panel not active!");
            return;
        }

        // Can't drag empty slots
        if (equippedSkill == null)
        {
            Debug.LogWarning($"[SkillHotbarSlot {slotIndex}] Cannot drag empty slot");
            return;
        }

        isDragging = true;
        Debug.Log($"[SkillHotbarSlot {slotIndex}] Begin drag: {equippedSkill.skillName}");

        // Store original position for potential revert
        originalPosition = rectTransform.localPosition;

        // Visual feedback
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.7f;
            canvasGroup.blocksRaycasts = false;
        }

        if (slotBackground != null)
        {
            slotBackground.color = dragColor;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        // Move to follow mouse
        if (rectTransform != null)
        {
            rectTransform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        Debug.Log($"[SkillHotbarSlot {slotIndex}] End drag: {equippedSkill.skillName}");

        // Check if dragged far enough to unequip
        if (IsOutsideUnequipDistance(eventData.position))
        {
            Debug.Log($"[SkillHotbarSlot {slotIndex}] Unequipping skill (dragged too far)");
            UnequipSkill();
            return;
        }

        // Return to original position
        if (rectTransform != null)
        {
            rectTransform.localPosition = originalPosition;
        }

        // Restore visuals
        RestoreVisuals();
    }

    private bool IsOutsideUnequipDistance(Vector3 dragEndPosition)
    {
        Vector3 slotScreenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
        float distance = Vector3.Distance(slotScreenPos, dragEndPosition);
        
        Debug.Log($"[SkillHotbarSlot {slotIndex}] Drag distance: {distance}, Unequip threshold: {unequipDistance}");
        return distance > unequipDistance;
    }

    #endregion

    #region Pointer Events

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        
        if (slotBackground != null && !isDragging)
        {
            slotBackground.color = hoverColor;
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Hover enter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        
        if (slotBackground != null && !isDragging)
        {
            UpdateSlotBackground();
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Hover exit");
    }

    #endregion

    #region Skill Equipment

    /// <summary>
    /// Equips a skill to this slot (replaces existing skill if any).
    /// Updates SkillManager and refreshes UI.
    /// </summary>
    public void EquipSkill(SkillData skillData)
    {
        if (skillData == null)
        {
            UnequipSkill();
            return;
        }

        equippedSkill = skillData;
        
        // Tell SkillManager to equip this skill
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.EquipSkill(slotIndex, skillData);
            equippedSkillComponent = SkillManager.Instance.GetSkill(slotIndex);
        }

        // Update UI
        UpdateDisplay();

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Equipped skill: {skillData.skillName}");
    }

    /// <summary>
    /// Unequips the skill from this slot.
    /// </summary>
    public void UnequipSkill()
    {
        equippedSkill = null;
        equippedSkillComponent = null;

        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.EquipSkill(slotIndex, null);
        }

        SetEmptyState();
        Debug.Log($"[SkillHotbarSlot {slotIndex}] Skill unequipped");
    }

    /// <summary>
    /// Swap skills between this slot and another slot.
    /// </summary>
    private void SwapSkills(SkillHotbarSlot otherSlot)
    {
        if (otherSlot == null || otherSlot == this)
            return;

        // Store current skills
        SkillData thisSkill = this.equippedSkill;
        SkillData otherSkill = otherSlot.equippedSkill;

        // Swap
        this.EquipSkill(otherSkill);
        otherSlot.EquipSkill(thisSkill);

        Debug.Log($"[SkillHotbarSlot] Swapped slot {slotIndex} <-> {otherSlot.slotIndex}");
    }

    #endregion

    #region UI Updates

    private void UpdateDisplay()
    {
        if (equippedSkill == null)
        {
            SetEmptyState();
            return;
        }

        // Set skill icon
        if (skillIconImage != null)
        {
            skillIconImage.sprite = equippedSkill.skillIcon;
            skillIconImage.color = Color.white;
            skillIconImage.enabled = true;
        }

        // Set background color
        UpdateSlotBackground();

        // Reset cooldown visual
        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = 0f;
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Display updated");
    }

    private void SetEmptyState()
    {
        // Clear icon
        if (skillIconImage != null)
        {
            skillIconImage.sprite = null;
            skillIconImage.enabled = false;
        }

        // Set background color to empty state
        UpdateSlotBackground();

        // Reset cooldown
        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = 0f;
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Set to empty state");
    }

    private void UpdateSlotBackground()
    {
        if (slotBackground != null)
        {
            if (isDragging)
            {
                slotBackground.color = dragColor;
            }
            else if (isHovering)
            {
                slotBackground.color = hoverColor;
            }
            else
            {
                slotBackground.color = equippedSkill != null ? occupiedSlotColor : emptySlotColor;
            }
        }
    }

    private void RestoreVisuals()
    {
        // Restore opacity
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Restore background color based on state
        UpdateSlotBackground();

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Visuals restored");
    }

    #endregion

    #region Helpers

    private bool IsSkillPanelActive()
    {
        if (SkillManagementPanel.Instance == null)
            return false;

        return SkillManagementPanel.Instance.isSkillPanelActive;
    }

    #endregion

    #region Public API

    public SkillData GetEquippedSkill() => equippedSkill;

    public int GetSlotIndex() => slotIndex;

    public void SetActivationKey(KeyCode key)
    {
        activationKey = key;
        if (hotkeyLabel != null)
        {
            hotkeyLabel.text = key.ToString().Replace("Alpha", "");
        }
    }

    public bool IsDragging => isDragging;

    #endregion
}