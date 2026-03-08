using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for player attack state and settings.
    /// Tracks combo chain, cooldowns, VFX prefabs, and attack parameters.
    /// </summary>
    [System.Serializable]
    public class PlayerAttackModel
    {
        #region VFX Prefabs

        [Header("VFX Prefabs")]
        public GameObject stabVFXPrefab = null;
        public GameObject horizontalVFXPrefab = null;
        public GameObject verticalVFXPrefab = null;
        public GameObject damagePopupPrefab = null;

        #endregion

        #region Combat Settings

        [Header("Combat Settings")]
        public LayerMask enemyLayers;
        public float comboResetTime = 2f;

        #endregion

        #region Damage Multipliers

        [Header("Damage Multipliers")]
        public float stabMultiplier = 0.8f;
        public float horizontalMultiplier = 1.0f;
        public float verticalMultiplier = 1.5f;

        #endregion

        #region VFX Settings

        [Header("VFX Settings")]
        public float vfxSpawnOffset = 1f;
        public float stabDuration = 0.2f;
        public float horizontalDuration = 0.3f;
        public float verticalDuration = 0.4f;

        #endregion

        #region VFX Position Offsets

        [Header("VFX Position Offsets")]
        public Vector2 stabPositionOffset = Vector2.zero;
        public Vector2 horizontalPositionOffset = Vector2.zero;
        public Vector2 verticalPositionOffset = Vector2.zero;

        #endregion

        #region References

        [Header("References")]
        public Transform playerTransform = null;
        public Transform centerPoint = null;

        #endregion

        #region State

        [Header("State")]
        public int currentComboStep = 0;
        public float comboResetTimer = 0f;
        public float attackCooldownTimer = 0f;
        public bool isInitialized = false;

        #endregion

        #region Constants

        public const int TOTAL_COMBO_STEPS = 3;

        #endregion

        #region Constructor

        public PlayerAttackModel()
        {
            comboResetTime = 2f;
            stabMultiplier = 0.8f;
            horizontalMultiplier = 1.0f;
            verticalMultiplier = 1.5f;
            vfxSpawnOffset = 1f;
            stabDuration = 0.2f;
            horizontalDuration = 0.3f;
            verticalDuration = 0.4f;
            stabPositionOffset = Vector2.zero;
            horizontalPositionOffset = Vector2.zero;
            verticalPositionOffset = Vector2.zero;
            currentComboStep = 0;
            comboResetTimer = 0f;
            attackCooldownTimer = 0f;
            isInitialized = false;
        }

        #endregion
    }
}