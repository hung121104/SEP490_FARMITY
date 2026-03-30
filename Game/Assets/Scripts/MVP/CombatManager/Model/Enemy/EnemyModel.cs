using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Unified data model for all enemy systems.
    /// Contains health, AI state, movement, knockback, and combat data.
    /// </summary>
    [System.Serializable]
    public class EnemyModel
    {
        #region Health Data

        [Header("Health")]
        public int currentHealth = 10;
        public int maxHealth = 10;
        public float lastHitAt = -999f;
        public float regenProgress = 0f;

        [Header("Out of Combat Regeneration")]
        public bool enableOutOfCombatRegen = true;
        public float regenDelaySeconds = 10f;
        public float regenHpPerSecond = 2f;
        public bool regenRequireNearGuardAnchor = true;
        public float regenGuardProximity = 1.5f;

        #endregion

        #region AI State Data

        [Header("AI State")]
        public EnemyState currentState = EnemyState.Guard;
        public bool isAlerted = false;
        public float alertTimer = 0f;
        public float hitAlertDuration = 5f;

        #endregion

        #region Detection Settings

        [Header("Detection")]
        public float detectionRange = 8f;
        public float attackRange = 1.5f;
        public float fieldOfViewAngle = 120f;
        public LayerMask playerLayer;
        public LayerMask obstacleLayer;

        #endregion

        #region Movement Data

        [Header("Movement")]
        public float moveSpeed = 2f;
        public float chaseSpeed = 3f;
        public float wanderSpeed = 1f;
        public float wanderRange = 5f;
        public bool enableSeparation = true;
        public float separationRadius = 0.8f;
        public float separationForce = 2.5f;
        public Vector3 startPosition = Vector3.zero;
        public Vector3 wanderTarget = Vector3.zero;
        public Vector2 currentWanderDirection = Vector2.right;
        public Vector2 facingDirection = Vector2.right;

        #endregion

        #region Guard Data

        [Header("Guard")]
        public float guardDuration = 2f;
        public float guardLookDuration = 1f;
        public float guardTimer = 0f;
        public GuardBehavior guardBehavior = GuardBehavior.NoCheck;
        public int guardDirection = 1;
        public float guardLookTimer = 0f;
        public bool isLookingLeft = false;

        #endregion

        #region Physics Data

        [Header("Physics")]
        public float friction = 3f;
        public float maxVelocity = 10f;

        #endregion

        #region Knockback Data

        [Header("Knockback")]
        public bool isKnockedBack = false;
        public float knockbackTimer = 0f;
        public float knockbackDuration = 0.3f;
        public float knockbackPushDistance = 3f;
        public float squashPixels = 0.05f;
        public float stretchPixels = 0.05f;
        public float waveDuration = 0.3f;
        public float flashDuration = 0.2f;
        public int flashCount = 2;
        public Color originalColor = Color.white;
        public Vector3 originalScale = Vector3.one;

        #endregion

        #region Combat Data

        [Header("Combat")]
        public int damageAmount = 1;
        public float knockbackForce = 30f;
        public float damageThrottleTime = 0.5f;
        public float lastDamageTime = -999f;
        public bool useActiveAttack = true;
        public float attackCooldown = 1.2f;
        public float attackRecovery = 0.1f;
        public float attackFrontDotThreshold = 0.25f;
        public float nextAttackTime = 0f;
        public bool isAttackAnimating = false;
        public bool hasAppliedImpactThisAttack = false;
        public bool pendingAttackTrigger = false;
        public int attackSequence = 0;
        public float attackTimeoutAt = 0f;

        [Header("Progression")]
        public int enemyLevel = 1;
        public int baseExp = 10;

        #endregion

        #region Runtime References

        [Header("Runtime References")]
        public Transform playerTransform = null;
        public Transform currentTarget = null;
        public Rigidbody2D rb = null;
        public Animator animator = null;
        public SpriteRenderer spriteRenderer = null;

        [Header("Network")]
        public string runtimeEnemyId = "";

        #endregion

        #region Initialization State

        public bool isInitialized = false;

        #endregion
    }

    #region Enums

    public enum EnemyState
    {
        Guard,
        Wandering,
        Chasing,
        Attacking
    }

    public enum GuardBehavior
    {
        NoCheck,
        OneCheck,
        BothCheck
    }

    #endregion
}