using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    public interface IDiceDisplayService
    {
        #region Initialization

        void Initialize(DiceDisplayModel model);
        bool IsInitialized();

        #endregion

        #region Dice Display

        GameObject SpawnDice(CombatManager.Model.DiceTier tier, Transform followTarget);
        void DespawnDice(GameObject diceInstance);
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