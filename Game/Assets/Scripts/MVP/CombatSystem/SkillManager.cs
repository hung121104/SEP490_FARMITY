using UnityEngine;
using System.Collections;
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
    private bool isInitialized = false;

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

        // Delayed initialization - wait for SkillDatabase to awake
        StartCoroutine(DelayedInitialization());
    }

    private void Update()
    {
        if (!isInitialized || !CombatModeManager.Instance.IsCombatModeActive)
            return;

        CheckSkillInput();
    }

    #endregion

    #region Initialization

    private IEnumerator DelayedInitialization()
    {
        // Wait until SkillDatabase is ready
        for (int attempts = 0; attempts < 50; attempts++)
        {
            if (SkillDatabase.Instance != null)
            {
                LinkSkillsFromDatabase();
                isInitialized = true;
                Debug.Log("[SkillManager] Initialization complete!");
                yield break;
            }
            yield return null;
        }

        Debug.LogError("[SkillManager] Failed to find SkillDatabase after 50 frames!");
    }

    private void LinkSkillsFromDatabase()
    {
        if (SkillDatabase.Instance == null)
        {
            Debug.LogError("[SkillManager] SkillDatabase not found!");
            return;
        }

        Debug.Log("[SkillManager] ===== LINKING SKILLS =====");

        // FIXED: Search in parent (CombatSystem) instead of SkillManager's children
        Transform combatSystemTransform = transform.parent;
        if (combatSystemTransform == null)
        {
            Debug.LogError("[SkillManager] SkillManager has no parent! Should be child of CombatSystem");
            return;
        }

        SkillBase[] allSkillComponents = combatSystemTransform.GetComponentsInChildren<SkillBase>(true);
        Debug.Log($"[SkillManager] Found {allSkillComponents.Length} SkillBase components");
        
        Dictionary<string, SkillBase> componentsByName = new Dictionary<string, SkillBase>();

        foreach (SkillBase skill in allSkillComponents)
        {
            string componentName = skill.GetType().Name;
            componentsByName[componentName] = skill;
            Debug.Log($"[SkillManager]   - Registered: {componentName} (GameObject: {skill.gameObject.name})");
        }

        // Link equipped skills
        for (int i = 0; i < equippedSkillsData.Length; i++)
        {
            Debug.Log($"[SkillManager] === SLOT {i} ===");

            if (equippedSkillsData[i] == null)
            {
                equippedSkillsComponents[i] = null;
                Debug.Log($"[SkillManager] Slot {i} is EMPTY (no SkillData assigned)");
                continue;
            }

            string linkedName = equippedSkillsData[i].linkedComponentName;
            Debug.Log($"[SkillManager] SkillData: {equippedSkillsData[i].skillName}");
            Debug.Log($"[SkillManager] Looking for component: '{linkedName}'");
            
            if (componentsByName.TryGetValue(linkedName, out var component))
            {
                equippedSkillsComponents[i] = component;
                Debug.Log($"[SkillManager] ✓✓✓ LINKED {linkedName} to slot {i} ✓✓✓");
            }
            else
            {
                Debug.LogError($"[SkillManager] ✗✗✗ FAILED: Component '{linkedName}' not found in scene!");
                Debug.LogError($"[SkillManager] Available components: {string.Join(", ", componentsByName.Keys)}");
                equippedSkillsComponents[i] = null;
            }
        }

        Debug.Log("[SkillManager] ===== LINKING COMPLETE =====");
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
        // TODO: Call skill.StartSkillFlow() or similar
    }

    #endregion

    #region Public API

    public SkillBase GetSkill(int index)
    {
        if (!isInitialized)
            return null;

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