using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for SkillManagement system.
    /// No longer depends on SkillDatabase.
    /// Receives skill list directly from SkillManagementPresenter.
    /// </summary>
    public class SkillManagementService : ISkillManagementService
    {
        private SkillManagementModel model;
        private readonly List<SkillData> catalogSkills = new List<SkillData>();

        public SkillManagementService(SkillManagementModel model)
        {
            this.model = model;
        }

        #region Initialization

        public void Initialize(List<SkillData> skills, int playerLevel)
        {
            catalogSkills.Clear();
            if (skills != null)
                catalogSkills.AddRange(skills.FindAll(s => s != null));

            RefreshForLevel(playerLevel);
            model.isInitialized = true;
            Debug.Log($"[SkillManagementService] Initialized with " +
                      $"{model.allSkills.Count} unlocked player skills at level {Mathf.Max(1, playerLevel)}");
        }

        public void RefreshForLevel(int playerLevel)
        {
            int safeLevel = Mathf.Max(1, playerLevel);

            // Show only player skills that meet unlock-level requirement.
            model.allSkills = catalogSkills.FindAll(s =>
                s != null &&
                s.IsPlayerSkill &&
                Mathf.Max(1, s.unlockLevel) <= safeLevel);

            Debug.Log($"[SkillManagementService] RefreshForLevel level={safeLevel} visibleSkills={model.allSkills.Count}");
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
