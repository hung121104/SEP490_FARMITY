using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for damage popup manager.
    /// Stores popup prefab reference and spawn settings.
    /// </summary>
    [System.Serializable]
    public class DamagePopupModel
    {
        #region Prefab Reference

        [Header("Prefab")]
        public GameObject popupPrefab = null;

        #endregion

        #region Spawn Settings

        [Header("Spawn Settings")]
        public Vector3 spawnOffset = new Vector3(0f, 0.8f, 0f);
        public float randomOffsetX = 0.2f; // Random horizontal spread
        public float randomOffsetY = 0.1f; // Random vertical spread

        #endregion

        #region Initialization

        [Header("Initialization")]
        public bool isInitialized = false;

        #endregion

        #region Constructor

        public DamagePopupModel()
        {
            popupPrefab = null;
            spawnOffset = new Vector3(0f, 0.8f, 0f);
            randomOffsetX = 0.2f;
            randomOffsetY = 0.1f;
            isInitialized = false;
        }

        #endregion
    }

    /// <summary>
    /// Enum for popup types (for future color variations).
    /// </summary>
    public enum PopupType
    {
        Damage,      // Red/White - normal damage
        CritDamage,  // Yellow/Orange - critical hit
        Heal,        // Green - healing
        Miss         // Gray - missed attack
    }
}