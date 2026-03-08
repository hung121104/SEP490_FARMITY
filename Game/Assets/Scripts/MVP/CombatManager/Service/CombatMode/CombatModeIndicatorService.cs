using UnityEngine;
using UnityEngine.UI;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for combat mode indicator management.
    /// Handles immediate state updates (animation handled by View).
    /// </summary>
    public class CombatModeIndicatorService : ICombatModeIndicatorService
    {
        private CombatModeIndicatorModel model;

        #region Constructor

        public CombatModeIndicatorService(CombatModeIndicatorModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(Image combatIcon, Image normalIcon)
        {
            model.combatModeIcon = combatIcon;
            model.normalModeIcon = normalIcon;
            model.isInitialized = true;

            Debug.Log("[CombatModeIndicatorService] Initialized with UI icons");
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Indicator Update

        public void UpdateIndicatorImmediate(bool isCombatMode)
        {
            if (!model.isInitialized)
            {
                Debug.LogWarning("[CombatModeIndicatorService] Cannot update - not initialized");
                return;
            }

            // Set immediate alpha values (no animation, instant switch)
            if (model.combatModeIcon != null)
            {
                Color combatColor = model.combatModeIcon.color;
                combatColor.a = isCombatMode ? 1f : 0f;
                model.combatModeIcon.color = combatColor;
            }

            if (model.normalModeIcon != null)
            {
                Color normalColor = model.normalModeIcon.color;
                normalColor.a = isCombatMode ? 0f : 1f;
                model.normalModeIcon.color = normalColor;
            }

            Debug.Log($"[CombatModeIndicatorService] Indicator updated: {(isCombatMode ? "Combat ⚔️" : "Normal 📝")}");
        }

        #endregion

        #region Getters

        public Image GetCombatModeIcon() => model.combatModeIcon;
        public Image GetNormalModeIcon() => model.normalModeIcon;
        public float GetFadeDuration() => model.fadeDuration;
        public AnimationCurve GetFadeCurve() => model.fadeCurve;

        #endregion
    }
}