using UnityEngine;
using Photon.Pun;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Main presenter for Enemy system.
    /// Now accepts EnemyDataSO for configurable enemy types.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class EnemyPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private EnemyModel model = new EnemyModel();

        // ✅ NEW: Enemy data reference
        [Header("Enemy Data")]
        [SerializeField] private EnemyDataSO enemyData;

        [Header("Dependencies")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject damagePopupPrefab;

        // ✅ NEW: Runtime enemy ID
        private string enemyId;

        private IEnemyHealthService healthService;
        private IEnemyKnockbackService knockbackService;
        private IEnemyCombatService combatService;
        private IEnemyAIService aiService;

        private Rigidbody2D rb;
        private EnemyView view;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
        }

        private void Update()
        {
            if (!model.isInitialized)
                return;

            knockbackService.UpdateKnockbackTimer(Time.deltaTime);

            if (knockbackService.IsKnockedBack())
                return;

            float distanceToPlayer = model.playerTransform != null 
                ? Vector2.Distance(transform.position, model.playerTransform.position) 
                : float.MaxValue;

            aiService.UpdateBehavior(Time.deltaTime, distanceToPlayer);

            if (healthService.IsDead())
            {
                HandleDeath();
            }
        }

        private void FixedUpdate()
        {
            if (!model.isInitialized)
                return;

            aiService.UpdatePhysics(Time.fixedDeltaTime);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!model.isInitialized)
                return;

            if (((1 << collision.gameObject.layer) & model.playerLayer) != 0)
            {
                if (combatService.CanDealDamage())
                {
                    combatService.DealDamageToPlayer(collision);
                }
            }
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // ✅ NEW: Validate enemy data
            if (enemyData == null)
            {
                Debug.LogError($"[EnemyPresenter] {gameObject.name} has no EnemyDataSO assigned!");
                return;
            }

            if (!enemyData.IsValid())
            {
                Debug.LogError($"[EnemyPresenter] EnemyDataSO '{enemyData.name}' is invalid!");
                return;
            }

            // ✅ NEW: Set enemy ID
            enemyId = enemyData.enemyId;
            gameObject.name = $"{enemyData.enemyName}_{enemyId}";

            // Get required components
            rb = GetComponent<Rigidbody2D>();
            
            if (animator == null)
                animator = GetComponent<Animator>();
            
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            view = GetComponent<EnemyView>();
            if (view == null)
            {
                view = gameObject.AddComponent<EnemyView>();
            }

            Transform playerTransform = FindLocalPlayerTransform();

            // ✅ NEW: Sync from EnemyDataSO instead of inspector
            SyncFromEnemyData(playerTransform);

            healthService = new EnemyHealthService(model);
            knockbackService = new EnemyKnockbackService(model);
            combatService = new EnemyCombatService(model);
            aiService = new EnemyAIService(model);

            healthService.Initialize(enemyData.maxHealth);
            knockbackService.Initialize(this);
            combatService.Initialize(damagePopupPrefab);
            aiService.Initialize(transform);

            model.isInitialized = true;

            if (view != null)
            {
                view.Initialize(this);
            }

            Debug.Log($"[EnemyPresenter] {gameObject.name} (ID: {enemyId}) initialized from {enemyData.name}");
        }

        // ✅ NEW: Load all settings from EnemyDataSO
        private void SyncFromEnemyData(Transform playerTransform)
        {
            // Runtime references
            model.playerTransform = playerTransform;
            model.rb = rb;
            model.animator = animator;
            model.spriteRenderer = spriteRenderer;

            // Health
            model.maxHealth = enemyData.maxHealth;
            model.currentHealth = enemyData.maxHealth;

            // Detection
            model.detectionRange = enemyData.detectionRange;
            model.attackRange = enemyData.attackRange;
            model.fieldOfViewAngle = enemyData.fieldOfViewAngle;
            model.playerLayer = LayerMask.GetMask("Player"); // Use your player layer
            model.obstacleLayer = LayerMask.GetMask("Obstacle"); // Use your obstacle layer

            // Movement
            model.moveSpeed = enemyData.moveSpeed;
            model.chaseSpeed = enemyData.chaseSpeed;
            model.wanderSpeed = enemyData.wanderSpeed;
            model.wanderRange = enemyData.wanderRange;

            // Guard
            model.guardDuration = enemyData.guardDuration;
            model.guardLookDuration = enemyData.guardLookDuration;

            // Combat
            model.damageAmount = enemyData.damageAmount;
            model.knockbackForce = enemyData.knockbackForce;
            model.damageThrottleTime = enemyData.damageThrottleTime;

            // Physics (keep defaults or add to SO)
            model.friction = 3f;
            model.maxVelocity = 10f;

            // Knockback
            model.knockbackDuration = enemyData.knockbackDuration;
            model.knockbackPushDistance = 3f;
            model.squashPixels = enemyData.squashPixels;
            model.stretchPixels = enemyData.stretchPixels;
            model.waveDuration = enemyData.waveDuration;
            model.flashDuration = enemyData.flashDuration;
            model.flashCount = enemyData.flashCount;
        }

        private Transform FindLocalPlayerTransform()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject go in players)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go.transform;
            }

            GameObject[] playerEntities = GameObject.FindGameObjectsWithTag("PlayerEntity");
            foreach (GameObject go in playerEntities)
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go.transform;
            }

            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
                return fallback.transform;

            PlayerHealthPresenter healthPresenter = Object.FindObjectOfType<PlayerHealthPresenter>();
            if (healthPresenter != null)
                return healthPresenter.transform;

            return null;
        }

        #endregion

        #region Public API

        public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!model.isInitialized)
                return;

            healthService.ChangeHealth(-damage);
            aiService.TakeKnockback(knockbackDirection, knockbackForce);

            StartCoroutine(knockbackService.PlayKnockbackEffect());
            StartCoroutine(knockbackService.PlayFlashEffect());

            DamagePopupPresenter.Spawn(transform.position, damage);
            aiService.OnHit();

            Debug.Log($"[EnemyPresenter] {enemyId} took {damage} damage. Health: {healthService.GetCurrentHealth()}/{healthService.GetMaxHealth()}");
        }

        // ✅ NEW: Get enemy ID
        public string GetEnemyId() => enemyId;
        public EnemyDataSO GetEnemyData() => enemyData;

        #endregion

        #region Death

        // ✅ NEW: Track if death has been handled
        private bool deathHandled = false;

        private void HandleDeath()
        {
            // ✅ FIX: Only handle death ONCE
            if (deathHandled)
                return;

            deathHandled = true;

            Debug.Log($"[EnemyPresenter] {enemyId} died");

            // ✅ Fire achievement event with enemy ID - called ONCE
            GameEventBus.FireEnemyKilled(enemyId, 1);

            aiService.Stop();

            if (model.animator != null)
            {
                model.animator.SetTrigger("Death");
            }

            Destroy(gameObject, 1f);
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => model.isInitialized;
        public EnemyState GetCurrentState() => aiService?.GetCurrentState() ?? EnemyState.Guard;
        public bool IsAlerted() => aiService?.IsAlerted() ?? false;
        public bool IsKnockedBack() => knockbackService?.IsKnockedBack() ?? false;
        public int GetCurrentHealth() => healthService?.GetCurrentHealth() ?? 0;
        public int GetMaxHealth() => healthService?.GetMaxHealth() ?? 1;
        public Vector2 GetFacingDirection() => model.facingDirection;
        public Animator GetAnimator() => model.animator;
        public SpriteRenderer GetSpriteRenderer() => model.spriteRenderer;

        #endregion

        #region Services API

        public IEnemyHealthService GetHealthService() => healthService;
        public IEnemyKnockbackService GetKnockbackService() => knockbackService;
        public IEnemyCombatService GetCombatService() => combatService;
        public IEnemyAIService GetAIService() => aiService;

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 8f);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 1.5f);
                return;
            }

            if (!model.isInitialized)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, model.detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, model.attackRange);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(model.startPosition, model.wanderRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, model.wanderTarget);
            Gizmos.DrawWireSphere(model.wanderTarget, 0.3f);

            DrawFieldOfViewCone();

            if (model.isAlerted)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
            }

            if (model.isKnockedBack)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
            }
        }

        private void DrawFieldOfViewCone()
        {
            float halfFOV = model.fieldOfViewAngle / 2f * Mathf.Deg2Rad;
            float facingAngle = Mathf.Atan2(model.facingDirection.y, model.facingDirection.x);

            Vector2 leftRay = new Vector2(
                Mathf.Cos(facingAngle - halfFOV),
                Mathf.Sin(facingAngle - halfFOV)
            );

            Vector2 rightRay = new Vector2(
                Mathf.Cos(facingAngle + halfFOV),
                Mathf.Sin(facingAngle + halfFOV)
            );

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + leftRay * model.detectionRange);
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + rightRay * model.detectionRange);
        }

        #endregion
    }
}