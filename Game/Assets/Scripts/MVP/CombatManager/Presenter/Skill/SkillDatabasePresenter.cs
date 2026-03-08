using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Skill Database.
    /// Central registry of all available skills.
    /// Other systems query this to find skill data.
    /// </summary>
    public class SkillDatabasePresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private SkillDatabaseModel model = new SkillDatabaseModel();

        [Header("Skills Collection")]
        [SerializeField] private List<SkillData> availableSkills = new List<SkillData>();

        private ISkillDatabaseService service;

        #region Singleton (Optional)

        private static SkillDatabasePresenter instance;
        public static SkillDatabasePresenter Instance => instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializeService();
        }

        #endregion

        #region Initialization

        private void InitializeService()
        {
            // Sync inspector list to model
            model.availableSkills = availableSkills;

            // Initialize service
            service = new SkillDatabaseService(model);
            service.Initialize(availableSkills);

            // Validate skills
            service.ValidateSkills();

            Debug.Log("[SkillDatabasePresenter] Initialized successfully");
        }

        #endregion

        #region Public API - Query by ID

        public SkillData GetSkillByID(int id)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[SkillDatabasePresenter] Service not initialized!");
                return null;
            }

            return service.GetSkillByID(id);
        }

        public bool HasSkillWithID(int id)
        {
            return service != null && service.HasSkillWithID(id);
        }

        #endregion

        #region Public API - Query by Name

        public SkillData GetSkillByName(string name)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[SkillDatabasePresenter] Service not initialized!");
                return null;
            }

            return service.GetSkillByName(name);
        }

        public bool HasSkillWithName(string name)
        {
            return service != null && service.HasSkillWithName(name);
        }

        #endregion

        #region Public API - Query All

        public List<SkillData> GetAllSkills()
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[SkillDatabasePresenter] Service not initialized!");
                return new List<SkillData>();
            }

            return service.GetAllSkills();
        }

        public int GetSkillCount()
        {
            return service?.GetSkillCount() ?? 0;
        }

        #endregion

        #region Static Helpers (Optional - For Easy Access)

        public static SkillData GetSkill(int id)
        {
            return Instance?.GetSkillByID(id);
        }

        public static SkillData GetSkill(string name)
        {
            return Instance?.GetSkillByName(name);
        }

        #endregion

        #region Getters for Other Systems

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public ISkillDatabaseService GetService() => service;

        #endregion
    }
}