using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the skill hotbar UI display.
/// Updates skill icons, cooldowns, and visibility based on combat mode.
/// Does NOT attach to SkillHotbar canvas - attach to a manager object in CombatSystem.
/// </summary>
public class SkillHotbarUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Canvas Reference")]
    [SerializeField] private GameObject skillHotbarCanvas;

    [Header("Slot 1 References")]
    [SerializeField] private Image skillIcon1;
    [SerializeField] private Image cooldownFill1;
    [SerializeField] private TextMeshProUGUI hotkeyLabel1;

    [Header("Slot 2 References")]
    [SerializeField] private Image skillIcon2;
    [SerializeField] private Image cooldownFill2;
    [SerializeField] private TextMeshProUGUI hotkeyLabel2;

    [Header("Slot 3 References")]
    [SerializeField] private Image skillIcon3;
    [SerializeField] private Image cooldownFill3;
    [SerializeField] private TextMeshProUGUI hotkeyLabel3;

    [Header("Slot 4 References")]
    [SerializeField] private Image skillIcon4;
    [SerializeField] private Image cooldownFill4;
    [SerializeField] private TextMeshProUGUI hotkeyLabel4;

    [Header("Skill Icons")]
    [SerializeField] private Sprite airSlashIcon;
    [SerializeField] private Sprite doubleStrikeIcon;
    [SerializeField] private Sprite heavySwingIcon;
    [SerializeField] private Sprite lightningStrikeIcon;
    [SerializeField] private Sprite emptySlotIcon;

    [Header("Visual State")]
    [SerializeField] private Color readyIconColor = Color.white;
    [SerializeField] private Color cooldownIconColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private float emptySlotAlpha = 0.35f;

    #endregion

    #region Private Fields

    private Image[] skillIcons;
    private Image[] cooldownFills;
    private TextMeshProUGUI[] hotkeyLabels;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeArrays();
        SetupHotkeyLabels();
        SubscribeToCombatMode();
        InitializeSkillIcons();
        
        // Start hidden
        if (skillHotbarCanvas != null)
            skillHotbarCanvas.SetActive(false);
    }

    private void Update()
    {
        if (!CombatModeManager.Instance.IsCombatModeActive)
            return;

        UpdateCooldowns();
    }

    private void OnDestroy()
    {
        UnsubscribeFromCombatMode();
    }

    #endregion

    #region Initialization

    private void InitializeArrays()
    {
        skillIcons = new Image[] { skillIcon1, skillIcon2, skillIcon3, skillIcon4 };
        cooldownFills = new Image[] { cooldownFill1, cooldownFill2, cooldownFill3, cooldownFill4 };
        hotkeyLabels = new TextMeshProUGUI[] { hotkeyLabel1, hotkeyLabel2, hotkeyLabel3, hotkeyLabel4 };
    }

    private void SetupHotkeyLabels()
    {
        for (int i = 0; i < hotkeyLabels.Length; i++)
        {
            if (hotkeyLabels[i] != null)
                hotkeyLabels[i].text = (i + 1).ToString();
        }
    }

    private void InitializeSkillIcons()
    {
        // Slot 1: AirSlash
        SetSkillIcon(0, airSlashIcon);

        // Slot 2: DoubleStrike
        SetSkillIcon(1, doubleStrikeIcon);

        // Slot 3: Empty (HeavySwing not implemented)
        SetSkillIcon(2, emptySlotIcon);

        // Slot 4: Empty (LightningStrike not implemented)
        SetSkillIcon(3, emptySlotIcon);
    }

    #endregion

    #region Combat Mode Events

    private void SubscribeToCombatMode()
    {
        CombatModeManager.OnCombatModeChanged += OnCombatModeChanged;
    }

    private void UnsubscribeFromCombatMode()
    {
        CombatModeManager.OnCombatModeChanged -= OnCombatModeChanged;
    }

    private void OnCombatModeChanged(bool isActive)
    {
        if (skillHotbarCanvas != null)
            skillHotbarCanvas.SetActive(isActive);

        Debug.Log($"[SkillHotbarUI] Combat mode: {isActive}");
    }

    #endregion

    #region Cooldown Update

    private void UpdateCooldowns()
    {
        if (SkillManager.Instance == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            SkillBase skill = SkillManager.Instance.GetSkill(i);

            if (skill == null)
            {
                // empty slot
                SetCooldownFill(i, 1f);
                SetIconVisual(i, isOnCooldown: true, isEmpty: true);
                continue;
            }

            float cooldownPercent = skill.GetSkillCooldownPercent();

            // overlay fill: 1 = fully covered (just used), 0 = ready
            SetCooldownFill(i, 1f - cooldownPercent);

            bool isOnCooldown = cooldownPercent < 0.999f;
            SetIconVisual(i, isOnCooldown, isEmpty: false);
        }
    }

    private void SetIconVisual(int index, bool isOnCooldown, bool isEmpty)
    {
        if (index < 0 || index >= skillIcons.Length) return;
        if (skillIcons[index] == null) return;

        if (isEmpty)
        {
            Color c = cooldownIconColor;
            c.a = emptySlotAlpha;
            skillIcons[index].color = c;
            return;
        }

        skillIcons[index].color = isOnCooldown ? cooldownIconColor : readyIconColor;
    }

    #endregion

    #region Helper Methods

    private void SetSkillIcon(int index, Sprite icon)
    {
        if (index < 0 || index >= skillIcons.Length)
            return;

        if (skillIcons[index] != null)
        {
            skillIcons[index].sprite = icon;
            skillIcons[index].enabled = icon != null;
        }
    }

    private void SetCooldownFill(int index, float fillAmount)
    {
        if (index < 0 || index >= cooldownFills.Length)
            return;

        if (cooldownFills[index] != null)
            cooldownFills[index].fillAmount = fillAmount;
    }

    #endregion

    #region Public API

    public void UpdateSkillIcon(int slotIndex, Sprite newIcon)
    {
        SetSkillIcon(slotIndex, newIcon);
    }

    #endregion
}