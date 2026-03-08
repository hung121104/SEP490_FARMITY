using System.Collections.Generic;
using CombatManager.SO;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for SkillManagement service.
    /// No longer depends on SkillDatabase.
    /// SkillManagementPresenter owns the skill list directly.
    /// </summary>
    public interface ISkillManagementService
    {
        void Initialize(List<SkillData> skills);
        bool IsInitialized();

        // ✅ Only PlayerSkills shown in panel
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