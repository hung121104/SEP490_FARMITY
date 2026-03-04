using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages all equipped skills.
/// Links SkillData to SkillBase components in the scene.
/// Provides equipped skill info to SkillHotbarUI.
/// 
/// Equipped skills start EMPTY - they are assigned via drag-drop
/// from SkillManagementPanel to SkillHotbar.
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Equipped Skills - Start Empty, Assigned via Drag-Drop")]
    [SerializeField] private SkillData[] equippedSkillsData = new SkillData[4];

    private SkillBase[] equippedSkillsComponents = new SkillBase[4];
    private Dictionary<string, SkillBase> skillComponentsByName = new Dictionary<string, SkillBase>();
    private bool isInitialized = false;

    #region Singleton

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion

    #region Initialization

    private void Start()
    {
        // Delayed initialization - wait for other systems to awake
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait until SkillDatabase is ready
        for (int attempts = 0; attempts < 50; attempts++)
        {
            if (SkillDatabase.Instance != null)
            {
                CacheAllSkillComponents();
                LinkInitialSkills();
                isInitialized = true;
                Debug.Log("[SkillManager] Initialization complete!");
                yield break;
            }
            yield return null;
        }

        Debug.LogError("[SkillManager] Failed to initialize - SkillDatabase not found!");
    }

    /// <summary>
    /// Cache all SkillBase components found in the scene for quick lookup.
    /// </summary>
    private void CacheAllSkillComponents()
    {
        skillComponentsByName.Clear();

        Transform combatSystemTransform = transform.parent;
        if (combatSystemTransform == null)
        {
            Debug.LogError("[SkillManager] SkillManager has no parent! Should be child of CombatSystem");
            return;
        }

        SkillBase[] allSkillComponents = combatSystemTransform.GetComponentsInChildren<SkillBase>(true);
        
        foreach (SkillBase skill in allSkillComponents)
        {
            string componentName = skill.GetType().Name;
            skillComponentsByName[componentName] = skill;
            Debug.Log($"[SkillManager] Cached skill component: {componentName}");
        }

        Debug.Log($"[SkillManager] Cached {skillComponentsByName.Count} skill components");
    }

    /// <summary>
    /// Link any skills that were manually assigned in Inspector (if any).
    /// Otherwise starts with all slots empty.
    /// </summary>
    private void LinkInitialSkills()
    {
        for (int i = 0; i < equippedSkillsData.Length; i++)
        {
            if (equippedSkillsData[i] != null)
            {
                LinkSkillToSlot(i, equippedSkillsData[i]);
                Debug.Log($"[SkillManager] Linked initial skill to slot {i}: {equippedSkillsData[i].skillName}");
            }
            else
            {
                equippedSkillsComponents[i] = null;
            }
        }
    }

    /// <summary>
    /// Link a SkillData to its SkillBase component.
    /// </summary>
    private void LinkSkillToSlot(int slotIndex, SkillData skillData)
    {
        if (skillData == null || slotIndex < 0 || slotIndex >= equippedSkillsComponents.Length)
            return;

        string linkedName = skillData.linkedComponentName;
        
        if (skillComponentsByName.TryGetValue(linkedName, out var component))
        {
            equippedSkillsComponents[slotIndex] = component;
            Debug.Log($"[SkillManager] Linked {linkedName} to slot {slotIndex}");
        }
        else
        {
            Debug.LogError($"[SkillManager] Skill component '{linkedName}' not found!");
            equippedSkillsComponents[slotIndex] = null;
        }
    }

    #endregion

    #region Skill Equipment (Called by SkillHotbarSlot on Drop)

    /// <summary>
    /// Equip a skill to a hotbar slot.
    /// Called when user drags a skill from panel and drops on hotbar.
    /// </summary>
    public void EquipSkill(int slotIndex, SkillData skillData)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[SkillManager] Not yet initialized!");
            return;
        }

        if (slotIndex < 0 || slotIndex >= equippedSkillsData.Length)
        {
            Debug.LogWarning($"[SkillManager] Invalid slot index: {slotIndex}");
            return;
        }

        // Assign or clear
        equippedSkillsData[slotIndex] = skillData;
        
        if (skillData != null)
        {
            LinkSkillToSlot(slotIndex, skillData);
            Debug.Log($"[SkillManager] Equipped {skillData.skillName} to slot {slotIndex}");
        }
        else
        {
            equippedSkillsComponents[slotIndex] = null;
            Debug.Log($"[SkillManager] Cleared slot {slotIndex}");
        }
    }

    #endregion

    #region Skill Queries

    /// <summary>
    /// Get the SkillBase component at a slot (for execution).
    /// </summary>
    public SkillBase GetSkill(int index)
    {
        if (!isInitialized)
            return null;

        if (index >= 0 && index < equippedSkillsComponents.Length)
            return equippedSkillsComponents[index];
        return null;
    }

    /// <summary>
    /// Get the SkillData at a slot (for UI display).
    /// </summary>
    public SkillData GetSkillData(int index)
    {
        if (index >= 0 && index < equippedSkillsData.Length)
            return equippedSkillsData[index];
        return null;
    }

    public int GetSkillCount() => equippedSkillsComponents.Length;

    public bool IsInitialized => isInitialized;

    #endregion
}