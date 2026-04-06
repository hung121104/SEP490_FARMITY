using UnityEngine;
using UnityEngine.UI;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for combat mode indicator state and settings.
    /// Tracks UI references, animation settings, and current fade state.
    /// </summary>
    [System.Serializable]
    public class CombatModeIndicatorModel
    {
        #region UI References

        [Header("UI References")]
        public Image combatModeIcon = null;  // Sword sprite
        public Image normalModeIcon = null;  // Letter sprite

        #endregion

        #region Animation Settings

        [Header("Animation Settings")]
        public float fadeDuration = 0.3f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region State

        [Header("State")]
        public bool isInitialized = false;

        #endregion

        #region Constructor

        public CombatModeIndicatorModel()
        {
            combatModeIcon = null;
            normalModeIcon = null;
            fadeDuration = 0.3f;
            fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            isInitialized = false;
        }

        #endregion
    }
}