using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for player knockback state and settings.
    /// Tracks knockback parameters, visual effects settings, and component references.
    /// </summary>
    [System.Serializable]
    public class PlayerKnockbackModel
    {
        #region Knockback Settings

        [Header("Knockback Settings")]
        public float knockbackDuration = 0.15f;
        public float squashPixels = 0.05f;
        public float stretchPixels = 0.05f;
        public float waveDuration = 0.3f;

        #endregion

        #region Flash Settings

        [Header("Flash Settings")]
        public float flashDuration = 0.2f;
        public int flashCount = 2;

        #endregion

        #region Component References

        [Header("Component References")]
        public Transform playerEntity = null;
        public Rigidbody2D rigidbody = null;
        public SpriteRenderer spriteRenderer = null;
        public PlayerMovement playerMovement = null;

        #endregion

        #region Visual State

        [Header("Visual State")]
        public Color originalColor = Color.white;
        public Vector3 originalScale = Vector3.one;

        #endregion

        #region State Flags

        [Header("State Flags")]
        public bool isInitialized = false;
        public bool isKnockbackActive = false;

        #endregion

        #region Constructor

        public PlayerKnockbackModel()
        {
            knockbackDuration = 0.15f;
            squashPixels = 0.05f;
            stretchPixels = 0.05f;
            waveDuration = 0.3f;
            flashDuration = 0.2f;
            flashCount = 2;

            playerEntity = null;
            rigidbody = null;
            spriteRenderer = null;
            playerMovement = null;

            originalColor = Color.white;
            originalScale = Vector3.one;

            isInitialized = false;
            isKnockbackActive = false;
        }

        #endregion
    }
}