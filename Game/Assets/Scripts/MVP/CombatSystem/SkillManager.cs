using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all equipped skills.
/// Now uses SkillDatabase to find skills by SkillData reference.
/// Handles skill input (Alpha1-4) and triggers skills.
/// Provides skill references for UI cooldown display.
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Equipped Skills - Now using SkillData")]
    [SerializeField] private SkillData[] equippedSkillsData = new SkillData[4];

    [Header("Input Keys")]
    [SerializeField] private KeyCode[] skillKeys = new KeyCode[]
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4
    };

    private SkillBase[] equippedSkillsComponents = new SkillBase[4];

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

        LinkSkillsFromDatabase();
    }

    private void Update()
    {
        if (!CombatModeManager.Instance.IsCombatModeActive)
            return;

        CheckSkillInput();
    }

    #endregion

    #region Initialization

    private void LinkSkillsFromDatabase()
    {
        if (SkillDatabase.Instance == null)
        {
            Debug.LogError("[SkillManager] SkillDatabase not found!");
            return;
        }

        // Find skill components in CombatSystem children
        SkillBase[] allSkillComponents = GetComponentsInChildren<SkillBase>(true);
        Dictionary<string, SkillBase> componentsByName = new Dictionary<string, SkillBase>();

        foreach (SkillBase skill in allSkillComponents)
        {
            componentsByName[skill.GetType().Name] = skill;
        }

        // Link equipped skills
        for (int i = 0; i < equippedSkillsData.Length; i++)
        {
            if (equippedSkillsData[i] == null)
            {
                equippedSkillsComponents[i] = null;
                continue;
            }

            string linkedName = equippedSkillsData[i].linkedComponentName;
            
            if (componentsByName.TryGetValue(linkedName, out var component))
            {
                equippedSkillsComponents[i] = component;
                Debug.Log($"[SkillManager] Linked {linkedName} to slot {i}");
            }
            else
            {
                Debug.LogWarning($"[SkillManager] Could not find skill component '{linkedName}'");
                equippedSkillsComponents[i] = null;
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

        Debug.Log($"[SkillManager] Attempting to trigger skill in slot {slotIndex}");
    }

    #endregion

    #region Public API

    public SkillBase GetSkill(int index)
    {
        if (index >= 0 && index < equippedSkillsComponents.Length)
            return equippedSkillsComponents[index];
        return null;
    }

    public SkillData GetSkillData(int index)
    {
        if (index >= 0 && index < equippedSkillsData.Length)
            return equippedSkillsData[index];
        return null;
    }

    public int GetSkillCount() => equippedSkillsComponents.Length;

    public void EquipSkill(int slotIndex, SkillData skillData)
    {
        if (slotIndex >= 0 && slotIndex < equippedSkillsData.Length)
        {
            equippedSkillsData[slotIndex] = skillData;
            LinkSkillsFromDatabase(); // Re-link all skills
            Debug.Log($"[SkillManager] Equipped {skillData?.skillName} to slot {slotIndex}");
        }
    }

    #endregion
}