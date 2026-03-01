using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all equipped skills and handles skill swapping in the future.
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [SerializeField] private SkillBase[] equippedSkills = new SkillBase[4];

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

        // Auto-find skill objects in children if not assigned
        if (equippedSkills[0] == null || equippedSkills[1] == null || equippedSkills[2] == null || equippedSkills[3] == null)
        {
            FindSkillsInChildren();
        }
    }

    private void FindSkillsInChildren()
    {
        SkillBase[] allSkills = GetComponentsInChildren<SkillBase>();
        for (int i = 0; i < Mathf.Min(allSkills.Length, equippedSkills.Length); i++)
        {
            if (equippedSkills[i] == null)
            {
                equippedSkills[i] = allSkills[i];
            }
        }
    }

    public SkillBase GetSkill(int index)
    {
        if (index >= 0 && index < equippedSkills.Length)
            return equippedSkills[index];
        return null;
    }

    // Future: Add EquipSkill(index, skillPrefab) for dynamic skill swapping
}