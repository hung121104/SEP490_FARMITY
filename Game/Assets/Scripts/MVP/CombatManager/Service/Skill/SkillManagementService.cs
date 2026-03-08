using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.SO;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for SkillManagement system.
    /// Mirrors SkillManagementPanel logic from CombatSystem (kept for legacy).
    /// Pure logic - no MonoBehaviour.
    /// </summary>
    public class SkillManagementService : ISkillManagementService
    {
        private SkillManagementModel model;

        public SkillManagementService(SkillManagementModel model)
        {
            this.model = model;
        }

        #region Initialization

        public void Initialize(List<SkillData> skills)
        {
            model.allSkills = skills;
            model.isInitialized = true;
            Debug.Log($"[SkillManagementService] Initialized with {skills.Count} skills");
        }

        public bool IsInitialized() => model.isInitialized;

        #endregion

        #region Skills

        public List<SkillData> GetAllSkills() => model.allSkills;

        #endregion

        #region Drag

        public void SetDraggingSkill(SkillData skill)
        {
            model.currentlyDraggingSkill = skill;
            Debug.Log($"[SkillManagementService] Dragging: {skill?.skillName}");
        }

        public void ClearDraggingSkill()
        {
            model.currentlyDraggingSkill = null;
            Debug.Log("[SkillManagementService] Drag cleared");
        }

        public SkillData GetDraggingSkill() => model.currentlyDraggingSkill;

        public bool IsAnySkillDragging() => model.currentlyDraggingSkill != null;

        #endregion

        #region Panel State

        public void OpenPanel()
        {
            model.isPanelOpen = true;
            Debug.Log("[SkillManagementService] Panel opened");
        }

        public void ClosePanel()
        {
            model.isPanelOpen = false;
            ClearDraggingSkill();
            Debug.Log("[SkillManagementService] Panel closed");
        }

        public void TogglePanel()
        {
            if (model.isPanelOpen) ClosePanel();
            else OpenPanel();
        }

        public bool IsPanelOpen() => model.isPanelOpen;

        #endregion
    }
}