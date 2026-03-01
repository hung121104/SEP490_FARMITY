using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all equipped skills.
/// Handles skill input (Alpha1-4) and triggers skills.
/// Provides skill references for UI cooldown display.
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Equipped Skills")]
    [SerializeField] private SkillBase[] equippedSkills = new SkillBase[4];

    [Header("Input Keys")]
    [SerializeField] private KeyCode[] skillKeys = new KeyCode[]
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4
    };

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        AutoFindSkills();
    }

    private void Update()
    {
        if (!CombatModeManager.Instance.IsCombatModeActive)
            return;

        CheckSkillInput();
    }

    #endregion

    #region Initialization

    private void AutoFindSkills()
    {
        // Auto-find skill components in children if not assigned
        SkillBase[] allSkills = GetComponentsInChildren<SkillBase>(true);
        
        for (int i = 0; i < Mathf.Min(allSkills.Length, equippedSkills.Length); i++)
        {
            if (equippedSkills[i] == null && i < allSkills.Length)
            {
                equippedSkills[i] = allSkills[i];
                Debug.Log($"[SkillManager] Auto-assigned {allSkills[i].GetType().Name} to slot {i}");
            }
        }
    }

    #endregion

    #region Input Handling

    private void CheckSkillInput()
    {
        for (int i = 0; i < skillKeys.Length; i++)
        {
            if (Input.GetKeyDown(skillKeys[i]))
            {
                TryTriggerSkill(i);
            }
        }
    }

    private void TryTriggerSkill(int slotIndex)
    {
        SkillBase skill = GetSkill(slotIndex);
        if (skill == null)
        {
            Debug.LogWarning($"[SkillManager] No skill equipped in slot {slotIndex}");
            return;
        }

        // Skill will handle its own input via SkillBase.CheckSkillInput()
        // This is just a fallback trigger
        Debug.Log($"[SkillManager] Attempting to trigger skill in slot {slotIndex}");
    }

    #endregion

    #region Public API

    public SkillBase GetSkill(int index)
    {
        if (index >= 0 && index < equippedSkills.Length)
            return equippedSkills[index];
        return null;
    }

    public int GetSkillCount() => equippedSkills.Length;

    // Future: Add EquipSkill(index, skillPrefab) for dynamic skill swapping
    public void EquipSkill(int slotIndex, SkillBase skill)
    {
        if (slotIndex >= 0 && slotIndex < equippedSkills.Length)
        {
            equippedSkills[slotIndex] = skill;
            Debug.Log($"[SkillManager] Equipped {skill?.GetType().Name} to slot {slotIndex}");
        }
    }

    #endregion
}