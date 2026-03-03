using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Represents a single slot in the skill hotbar.
/// Can receive dropped skills from SkillManagementPanel.
/// Displays the equipped skill icon and cooldown.
/// Handles skill execution via key press.
/// 
/// Attached to individual slot GameObjects in the hotbar.
/// (SkillSlot1, SkillSlot2, etc.)
/// </summary>
public class SkillHotbarSlot : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
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

    #endregion

    #region Private Fields

    private SkillData equippedSkill = null;
    private SkillBase equippedSkillComponent = null;
    private bool isHovering = false;

    #endregion

    #region Initialization

    private void Start()
    {
        SetupUI();
        SetEmptyState();
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

        // Check if this slot's key was pressed
        if (Input.GetKeyDown(activationKey))
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
        // Fill represents the cooldown overlay (1 = fully covered, 0 = ready)
        cooldownFillImage.fillAmount = 1f - cooldownPercent;

        // Update icon color based on cooldown state
        if (skillIconImage != null)
        {
            bool isOnCooldown = cooldownPercent < 0.999f;
            skillIconImage.color = isOnCooldown 
                ? new Color(0.6f, 0.6f, 0.6f, 1f) 
                : Color.white;
        }
    }

    #endregion

    #region Drop Handler

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[SkillHotbarSlot {slotIndex}] OnDrop called");

        // Get the dragged SkillDisplayItem
        SkillDisplayItem draggedItem = eventData.pointerDrag?.GetComponent<SkillDisplayItem>();
        
        if (draggedItem == null)
        {
            Debug.LogWarning("[SkillHotbarSlot] Dropped object is not a SkillDisplayItem!");
            return;
        }

        SkillData droppedSkill = draggedItem.GetSkillData();
        
        if (droppedSkill == null)
        {
            Debug.LogWarning("[SkillHotbarSlot] Dropped skill data is null!");
            return;
        }

        // Assign the skill to this slot
        EquipSkill(droppedSkill);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        
        if (slotBackground != null)
        {
            slotBackground.color = hoverColor;
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Hover enter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        
        if (slotBackground != null)
        {
            slotBackground.color = equippedSkill != null ? occupiedSlotColor : emptySlotColor;
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Hover exit");
    }

    #endregion

    #region Skill Equipment

    /// <summary>
    /// Equips a skill to this slot.
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
        if (slotBackground != null)
        {
            slotBackground.color = occupiedSlotColor;
        }

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
        if (slotBackground != null)
        {
            slotBackground.color = emptySlotColor;
        }

        // Reset cooldown
        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = 0f;
        }

        Debug.Log($"[SkillHotbarSlot {slotIndex}] Set to empty state");
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

    #endregion
}