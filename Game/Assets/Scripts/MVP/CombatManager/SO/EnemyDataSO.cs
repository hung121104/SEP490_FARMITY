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

        [Header("Stats")]
        public int maxHealth = 10;
        public int damageAmount = 1;
        public float knockbackForce = 30f;

        [Header("Movement")]
        public float moveSpeed = 2f;
        public float chaseSpeed = 3f;
        public float wanderSpeed = 1f;
        public float wanderRange = 5f;

        [Header("Detection")]
        public float detectionRange = 8f;
        public float attackRange = 1.5f;
        public float fieldOfViewAngle = 120f;

        [Header("Guard")]
        public float guardDuration = 2f;
        public float guardLookDuration = 1f;

        [Header("Combat")]
        public float damageThrottleTime = 0.5f;

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