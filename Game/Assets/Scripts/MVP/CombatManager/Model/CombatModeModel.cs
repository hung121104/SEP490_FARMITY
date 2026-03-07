using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for combat mode state and settings.
    /// Combat mode is now driven by weapon equip/unequip.
    /// Alt key toggle removed - Phase 4.
    /// </summary>
    [System.Serializable]
    public class CombatModeModel
    {
        #region Combat Mode State

        [Header("Combat Mode State")]
        public bool isCombatModeActive = false;

        #endregion

        #region Debug Settings

        [Header("Debug Settings")]
        [Tooltip("DEBUG ONLY - kept for future debug toggle if needed. Not used in gameplay.")]
        public KeyCode debugToggleKey = KeyCode.LeftAlt;

        [Tooltip("Enable debug toggle via key. Disable in production!")]
        public bool enableDebugToggle = false;

        #endregion

        #region Constructor

        public CombatModeModel()
        {
            isCombatModeActive = false;
            enableDebugToggle = false;
        }

        #endregion
    }
}