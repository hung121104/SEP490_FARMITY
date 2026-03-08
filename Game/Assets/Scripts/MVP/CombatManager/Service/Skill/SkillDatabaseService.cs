using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.SO;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for skill database management.
    /// Handles skill indexing, querying, and validation.
    /// </summary>
    public class SkillDatabaseService : ISkillDatabaseService
    {
        private SkillDatabaseModel model;

        #region Constructor

        public SkillDatabaseService(SkillDatabaseModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(List<SkillData> skills)
        {
            model.availableSkills = skills ?? new List<SkillData>();
            BuildIndexes();
            model.isInitialized = true;

            Debug.Log($"[SkillDatabaseService] Initialized with {model.GetSkillCount()} skills");
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Indexing

        private void BuildIndexes()
        {
            model.ClearIndexes();

            foreach (SkillData skill in model.availableSkills)
            {
                if (skill == null)
                {
                    Debug.LogWarning("[SkillDatabaseService] Null skill in collection!");
                    continue;
                }

                // Index by ID
                if (!model.skillsByID.ContainsKey(skill.skillID))
                {
                    model.skillsByID.Add(skill.skillID, skill);
                }
                else
                {
                    Debug.LogWarning($"[SkillDatabaseService] Duplicate skill ID {skill.skillID} ({skill.skillName})!");
                }

                // Index by name
                if (!model.skillsByName.ContainsKey(skill.skillName))
                {
                    model.skillsByName.Add(skill.skillName, skill);
                }
                else
                {
                    Debug.LogWarning($"[SkillDatabaseService] Duplicate skill name '{skill.skillName}'!");
                }
            }

            Debug.Log($"[SkillDatabaseService] Built indexes: {model.skillsByID.Count} by ID, {model.skillsByName.Count} by name");
        }

        #endregion

        #region Query by ID

        public SkillData GetSkillByID(int id)
        {
            if (model.skillsByID.TryGetValue(id, out SkillData skill))
            {
                return skill;
            }

            Debug.LogWarning($"[SkillDatabaseService] Skill ID {id} not found!");
            return null;
        }

        public bool HasSkillWithID(int id)
        {
            return model.skillsByID.ContainsKey(id);
        }

        #endregion

        #region Query by Name

        public SkillData GetSkillByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[SkillDatabaseService] Cannot query with null/empty name!");
                return null;
            }

            if (model.skillsByName.TryGetValue(name, out SkillData skill))
            {
                return skill;
            }

            Debug.LogWarning($"[SkillDatabaseService] Skill '{name}' not found!");
            return null;
        }

        public bool HasSkillWithName(string name)
        {
            return !string.IsNullOrEmpty(name) && model.skillsByName.ContainsKey(name);
        }

        #endregion

        #region Query All

        public List<SkillData> GetAllSkills()
        {
            return new List<SkillData>(model.availableSkills);
        }

        public int GetSkillCount()
        {
            return model.GetSkillCount();
        }

        #endregion

        #region Validation

        public bool ValidateSkills()
        {
            bool isValid = true;

            // Check for null skills
            int nullCount = model.availableSkills.FindAll(s => s == null).Count;
            if (nullCount > 0)
            {
                Debug.LogError($"[SkillDatabaseService] Found {nullCount} null skills in collection!");
                isValid = false;
            }

            // Check for duplicate IDs
            HashSet<int> seenIDs = new HashSet<int>();
            foreach (var skill in model.availableSkills)
            {
                if (skill == null) continue;

                if (seenIDs.Contains(skill.skillID))
                {
                    Debug.LogError($"[SkillDatabaseService] Duplicate ID {skill.skillID}!");
                    isValid = false;
                }
                else
                {
                    seenIDs.Add(skill.skillID);
                }
            }

            // Check for duplicate names
            HashSet<string> seenNames = new HashSet<string>();
            foreach (var skill in model.availableSkills)
            {
                if (skill == null) continue;

                if (seenNames.Contains(skill.skillName))
                {
                    Debug.LogError($"[SkillDatabaseService] Duplicate name '{skill.skillName}'!");
                    isValid = false;
                }
                else
                {
                    seenNames.Add(skill.skillName);
                }
            }

            if (isValid)
            {
                Debug.Log("[SkillDatabaseService] Validation passed!");
            }

            return isValid;
        }

        #endregion
    }
}