using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central database of all available skills.
/// Loaded from a ScriptableObject asset.
/// SkillManager references this to know which skills exist.
/// SkillManagementMenu uses this to display available skills.
/// </summary>
public class SkillDatabase : MonoBehaviour
{
    public static SkillDatabase Instance { get; private set; }

    [Header("Skill Collection")]
    [SerializeField] private List<SkillData> availableSkills = new List<SkillData>();

    private Dictionary<int, SkillData> skillsByID = new Dictionary<int, SkillData>();
    private Dictionary<string, SkillData> skillsByName = new Dictionary<string, SkillData>();

    #region Initialization

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        skillsByID.Clear();
        skillsByName.Clear();

        foreach (SkillData skill in availableSkills)
        {
            if (skill == null) continue;

            // Index by ID
            if (!skillsByID.ContainsKey(skill.skillID))
                skillsByID.Add(skill.skillID, skill);
            else
                Debug.LogWarning($"[SkillDatabase] Duplicate skill ID {skill.skillID}!");

            // Index by name
            if (!skillsByName.ContainsKey(skill.skillName))
                skillsByName.Add(skill.skillName, skill);
            else
                Debug.LogWarning($"[SkillDatabase] Duplicate skill name '{skill.skillName}'!");
        }

        Debug.Log($"[SkillDatabase] Loaded {skillsByID.Count} skills");
    }

    #endregion

    #region Public API

    public SkillData GetSkillByID(int id)
    {
        if (skillsByID.TryGetValue(id, out var skill))
            return skill;
        
        Debug.LogWarning($"[SkillDatabase] Skill ID {id} not found!");
        return null;
    }

    public SkillData GetSkillByName(string name)
    {
        if (skillsByName.TryGetValue(name, out var skill))
            return skill;

        Debug.LogWarning($"[SkillDatabase] Skill '{name}' not found!");
        return null;
    }

    public List<SkillData> GetAllSkills() => new List<SkillData>(availableSkills);

    public int GetSkillCount() => availableSkills.Count;

    #endregion
}