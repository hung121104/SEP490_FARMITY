using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for player health state.
    /// Tracks current health, invulnerability state, and initialization status.
    /// </summary>
    [System.Serializable]
    public class PlayerHealthModel
    {
        #region Health State

        [Header("Health State")]
        public int currentHealth = 0;
        public int maxHealth = 0;
        public float targetHealthValue = 0f; // For ease animation

        #endregion

        #region Invulnerability

        [Header("Invulnerability")]
        public bool isInvulnerable = false;

        #endregion

        #region Initialization

        [Header("Initialization")]
        public bool isInitialized = false;

        #endregion

        #region Player Reference

        [Header("Player Reference")]
        public Transform playerEntity = null;

        #endregion

        #region Constructor

        public PlayerHealthModel()
        {
            currentHealth = 0;
            maxHealth = 0;
            targetHealthValue = 0f;
            isInvulnerable = false;
            isInitialized = false;
            playerEntity = null;
        }

        #endregion

        #region Helpers

        public bool IsDead() => currentHealth <= 0;

        public void ClampHealth()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        #endregion
    }
}