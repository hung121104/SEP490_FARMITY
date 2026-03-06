using UnityEngine;
using System.Collections.Generic;
using CombatManager.SO;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for skill database.
    /// Stores all available skills and indexing structures.
    /// </summary>
    [System.Serializable]
    public class SkillDatabaseModel
    {
        #region Skill Collection

        [Header("Skill Collection")]
        public List<SkillData> availableSkills = new List<SkillData>();

        #endregion

        #region Indexing

        [System.NonSerialized]
        public Dictionary<int, SkillData> skillsByID = new Dictionary<int, SkillData>();

        [System.NonSerialized]
        public Dictionary<string, SkillData> skillsByName = new Dictionary<string, SkillData>();

        #endregion

        #region Initialization

        [Header("Initialization")]
        public bool isInitialized = false;

        #endregion

        #region Constructor

        public SkillDatabaseModel()
        {
            availableSkills = new List<SkillData>();
            skillsByID = new Dictionary<int, SkillData>();
            skillsByName = new Dictionary<string, SkillData>();
            isInitialized = false;
        }

        #endregion

        #region Helpers

        public int GetSkillCount() => availableSkills.Count;

        public void ClearIndexes()
        {
            skillsByID.Clear();
            skillsByName.Clear();
        }

        #endregion
    }
}