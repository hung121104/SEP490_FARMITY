using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for dice display service.
    /// Handles spawning and managing roll display instances.
    /// </summary>
    public interface IDiceDisplayService
    {
        #region Initialization

        void Initialize(
            GameObject d6, GameObject d8, GameObject d10, GameObject d12, GameObject d20,
            Vector3 offset, float duration, float wobbleScale, float wobbleSpeed
        );
        bool IsInitialized();

        #endregion

        #region Display Management

        // ✅ FIX: Use CombatManager.Model.DiceTier
        GameObject ShowRoll(CombatManager.Model.DiceTier tier, int rollResult, Vector3 position);
        GameObject GetDicePrefab(CombatManager.Model.DiceTier tier);

        #endregion

        #region Settings

        Vector3 GetRollDisplayOffset();
        float GetRollAnimationDuration();
        float GetWobbleScale();
        float GetWobbleSpeed();

        #endregion
    }
}