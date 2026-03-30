using UnityEngine;
using CombatManager.Model;

namespace CombatManager.SO
{
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Combat/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Enemy Identity")]
        public string enemyId = "";
        public string enemyName = "Unnamed Enemy";
        [TextArea(2, 4)]
        public string enemyDescription = "";
        public Sprite enemyIcon;

        [Header("Enemy Prefab")]
        [Tooltip("Prefab with EnemyPresenter + Animator + SpriteRenderer")]
        public GameObject enemyPrefab;

        [Header("Spawn")]
        [Tooltip("Seconds to wait before this enemy type respawns after death.")]
        public float respawnDelaySeconds = 20f;

        [Header("Stats")]
        public int maxHealth = 10;
        public int damageAmount = 1;
        public int baseExp = 10;
        public float knockbackForce = 30f;

        [Header("Out of Combat Regeneration")]
        [Tooltip("If enabled, this enemy regenerates HP while out of combat.")]
        public bool enableOutOfCombatRegen = true;
        [Tooltip("Seconds after the last hit before regeneration starts.")]
        public float regenDelaySeconds = 10f;
        [Tooltip("HP regenerated per second while conditions are met.")]
        public float regenHpPerSecond = 2f;
        [Tooltip("Require enemy to be near its guard/home anchor to regenerate.")]
        public bool regenRequireNearGuardAnchor = true;
        [Tooltip("Allowed distance from guard/home anchor for regeneration.")]
        public float regenGuardProximity = 1.5f;

        [Header("Movement")]
        public float moveSpeed = 2f;
        public float chaseSpeed = 3f;
        public float wanderSpeed = 1f;
        public float wanderRange = 5f;

        [Header("Separation")]
        public bool enableSeparation = true;
        public float separationRadius = 0.8f;
        public float separationForce = 2.5f;

        [Header("Detection")]
        public float detectionRange = 8f;
        public float attackRange = 1.5f;
        public float fieldOfViewAngle = 120f;

        [Header("Guard")]
        public float guardDuration = 2f;
        public float guardLookDuration = 1f;

        [Header("Combat")]
        public float damageThrottleTime = 0.5f;
        public bool useActiveAttack = true;
        public float attackCooldown = 1.2f;
        public float attackRecovery = 0.1f;
        [Range(-1f, 1f)] public float attackFrontDotThreshold = 0.25f;

        [Header("Knockback")]
        public float knockbackDuration = 0.3f;
        public float squashPixels = 0.05f;
        public float stretchPixels = 0.05f;
        public float waveDuration = 0.3f;
        public float flashDuration = 0.2f;
        public int flashCount = 2;

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(enemyName))
                enemyName = name;

            if (string.IsNullOrEmpty(enemyId))
                enemyId = name.ToLower().Replace(" ", "_");

            if (respawnDelaySeconds < 0f)
                respawnDelaySeconds = 0f;

            if (regenDelaySeconds < 0f)
                regenDelaySeconds = 0f;

            if (regenHpPerSecond < 0f)
                regenHpPerSecond = 0f;

            if (regenGuardProximity < 0f)
                regenGuardProximity = 0f;
        }

        #endregion

        #region Public API

        public bool IsValid()
        {
            return enemyPrefab != null && !string.IsNullOrEmpty(enemyName);
        }

        #endregion
    }
}