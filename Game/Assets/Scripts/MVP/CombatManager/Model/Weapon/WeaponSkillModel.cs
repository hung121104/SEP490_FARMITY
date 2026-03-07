using UnityEngine;
using CombatManager.SO;

namespace CombatManager.Model
{
    /// <summary>
    /// Model for weapon skill slot.
    /// Tracks current weapon skill state.
    /// Shared across all weapon skill types.
    /// </summary>
    [System.Serializable]
    public class WeaponSkillModel
    {
        [Header("Skill State")]
        public SkillData currentSkillData;
        public WeaponType requiredWeaponType = WeaponType.None;
        public bool isSkillLoaded = false;
        public bool isOnCooldown = false;
        public float cooldownRemaining = 0f;
        public float cooldownDuration = 3f;

        [Header("Hotkey")]
        public KeyCode skillKey = KeyCode.R;
    }
}