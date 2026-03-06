using UnityEngine;
using System.Collections.Generic;
using CombatManager.SO;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for SkillManagementPanel.
    /// Mirrors SkillManagementPanel from CombatSystem (kept for legacy).
    /// </summary>
    [System.Serializable]
    public class SkillManagementModel
    {
        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.M;

        [Header("State")]
        public bool isPanelOpen = false;
        public bool isInitialized = false;

        // Runtime - not serialized
        [System.NonSerialized]
        public List<SkillData> allSkills = new List<SkillData>();

        [System.NonSerialized]
        public SkillData currentlyDraggingSkill = null;

        public bool IsAnySkillDragging => currentlyDraggingSkill != null;
    }
}