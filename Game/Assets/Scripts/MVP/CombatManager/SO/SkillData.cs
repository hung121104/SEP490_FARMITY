using UnityEngine;
using CombatManager.Model;

namespace CombatManager.SO
{
    /// <summary>
    /// Skill configuration as ScriptableObject.
    /// Contains all metadata about a skill without needing inspector assignments.
    /// One SkillData per skill type (AirSlash, DoubleStrike, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "Skill_", menuName = "Combat/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Identification")]
        public string skillName = "Unnamed Skill";
        public string skillDescription = "";
        public int skillID = 0;

        [Header("UI Display")]
        public Sprite skillIcon;
        public Color skillColor = Color.white;

        [Header("Gameplay")]
        public KeyCode activationKey = KeyCode.Alpha1;
        public float cooldown = 3f;
        public DiceTier diceTier = DiceTier.D6;

        [Header("Combat")]
        public float skillMultiplier = 1.5f;
        public float skillRange = 5f;

        [Header("Linking")]
        public string linkedComponentName = ""; // e.g., "AirSlash" matches AirSlash.cs component name

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(skillName))
                skillName = name;
        }

        #endregion
    }
}