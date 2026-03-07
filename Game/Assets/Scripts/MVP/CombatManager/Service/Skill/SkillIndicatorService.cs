using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    public class SkillIndicatorService : ISkillIndicatorService
    {
        private SkillIndicatorModel model;

        public SkillIndicatorService(SkillIndicatorModel model)
        {
            this.model = model;
        }

        public void Initialize(SkillIndicatorModel model)
        {
            this.model = model;
            ValidatePrefabs();
            model.isInitialized = true;
            Debug.Log("[SkillIndicatorService] Initialized");
        }

        public bool IsInitialized() => model != null && model.isInitialized;

        public GameObject SpawnIndicator(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[SkillIndicatorService] Prefab is null!");
                return null;
            }
            return Object.Instantiate(prefab);
        }

        public void SetCurrentType(CombatManager.Model.IndicatorType type)
        {
            model.currentType = type;
        }

        public CombatManager.Model.IndicatorType GetCurrentType() => model.currentType;

        public bool IsActive() => model.IsActive;

        private void ValidatePrefabs()
        {
            if (model.arrowPrefab == null)
                Debug.LogWarning("[SkillIndicatorService] Arrow prefab missing!");
            if (model.conePrefab == null)
                Debug.LogWarning("[SkillIndicatorService] Cone prefab missing!");
            if (model.circlePrefab == null)
                Debug.LogWarning("[SkillIndicatorService] Circle prefab missing!");
        }
    }
}