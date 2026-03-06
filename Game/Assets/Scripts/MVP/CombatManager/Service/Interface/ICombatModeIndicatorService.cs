using UnityEngine;
using UnityEngine.UI;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for combat mode indicator service.
    /// Defines operations for managing visual indicator state.
    /// </summary>
    public interface ICombatModeIndicatorService
    {
        #region Initialization

        void Initialize(Image combatIcon, Image normalIcon);
        bool IsInitialized();

        #endregion

        #region Indicator Update

        void UpdateIndicatorImmediate(bool isCombatMode);

        #endregion

        #region Getters

        Image GetCombatModeIcon();
        Image GetNormalModeIcon();
        float GetFadeDuration();
        AnimationCurve GetFadeCurve();

        #endregion
    }
}