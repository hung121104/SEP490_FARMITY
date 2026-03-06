using System.Collections.Generic;
using CombatManager.SO;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for SkillManagement service.
    /// </summary>
    public interface ISkillManagementService
    {
        void Initialize(List<SkillData> skills);
        bool IsInitialized();

        List<SkillData> GetAllSkills();

        void SetDraggingSkill(SkillData skill);
        void ClearDraggingSkill();
        SkillData GetDraggingSkill();
        bool IsAnySkillDragging();

        void OpenPanel();
        void ClosePanel();
        void TogglePanel();
        bool IsPanelOpen();
    }
}