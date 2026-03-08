using System.Collections.Generic;
using CombatManager.SO;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for skill database service.
    /// Defines operations for querying and managing skill data.
    /// </summary>
    public interface ISkillDatabaseService
    {
        #region Initialization

        void Initialize(List<SkillData> skills);
        bool IsInitialized();

        #endregion

        #region Query by ID

        SkillData GetSkillByID(int id);
        bool HasSkillWithID(int id);

        #endregion

        #region Query by Name

        SkillData GetSkillByName(string name);
        bool HasSkillWithName(string name);

        #endregion

        #region Query All

        List<SkillData> GetAllSkills();
        int GetSkillCount();

        #endregion

        #region Validation

        bool ValidateSkills();

        #endregion
    }
}