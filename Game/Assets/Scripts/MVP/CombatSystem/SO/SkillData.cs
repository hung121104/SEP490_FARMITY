using UnityEngine;

/// <summary>
/// Skill configuration as ScriptableObject.
/// Contains all metadata about a skill without needing inspector assignments.
/// One SkillData per skill type (AirSlash, DoubleStrike, etc.)
/// </summary>
[CreateAssetMenu(fileName = "Skill_", menuName = "Combat/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] public string skillName = "Unnamed Skill";
    [SerializeField] public string skillDescription = "";
    [SerializeField] public int skillID = 0;

    [Header("UI Display")]
    [SerializeField] public Sprite skillIcon;
    [SerializeField] public Color skillColor = Color.white;

    [Header("Gameplay")]
    [SerializeField] public KeyCode activationKey = KeyCode.Alpha1;
    [SerializeField] public float cooldown = 3f;
    [SerializeField] public DiceTier diceTier = DiceTier.D6;

    [Header("Combat")]
    [SerializeField] public float skillMultiplier = 1.5f;
    [SerializeField] public float skillRange = 5f;

    [Header("Linking")]
    [SerializeField] public string linkedComponentName = ""; // e.g., "AirSlash" matches AirSlash.cs component name

    #region Validation

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(skillName))
            skillName = name;
    }

    #endregion
}