using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    public interface ISkillIndicatorService
    {
        void Initialize(SkillIndicatorModel model);
        bool IsInitialized();

        GameObject SpawnIndicator(GameObject prefab);
        void SetCurrentType(CombatManager.Model.IndicatorType type);
        CombatManager.Model.IndicatorType GetCurrentType();
        bool IsActive();
    }
}