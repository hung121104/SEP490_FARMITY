using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for combat mode state and settings.
    /// Tracks whether combat mode is active and the toggle key.
    /// </summary>
    [System.Serializable]
    public class CombatModeModel
    {
        #region Combat Mode State

        [Header("Combat Mode State")]
        public bool isCombatModeActive = false;

        #endregion

        #region Settings

        [Header("Settings")]
        public KeyCode combatModeToggleKey = KeyCode.LeftAlt;

        #endregion

        #region Constructor

        public CombatModeModel()
        {
            isCombatModeActive = false;
            combatModeToggleKey = KeyCode.LeftAlt;
        }

        #endregion
    }
}