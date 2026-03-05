using UnityEngine;
using Photon.Pun;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Main presenter for Enemy system.
    /// Coordinates all enemy services (Health, AI, Knockback, Combat).
    /// Single brain that manages all enemy behavior.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class EnemyPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private EnemyModel model = new EnemyModel();

        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 10;

        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float fieldOfViewAngle = 120f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float chaseSpeed = 3f;
        [SerializeField] private float wanderSpeed = 1f;
        [SerializeField] private float wanderRange = 5f;

        [Header("Guard Settings")]
        [SerializeField] private float guardDuration = 2f;
        [SerializeField] private float guardLookDuration = 1f;

        [Header("Combat Settings")]
        [SerializeField] private int damageAmount = 1;
        [SerializeField] private float knockbackForce = 30f;
        [SerializeField] private float damageThrottleTime = 0.5f;
        [SerializeField] private GameObject damagePopupPrefab;

        [Header("Physics Settings")]
        [SerializeField] private float friction = 3f;
        [SerializeField] private float maxVelocity = 10f;

        [Header("Knockback Settings")]
        [SerializeField] private float knockbackDuration = 0.3f;
        [SerializeField] private float knockbackPushDistance = 3f;
        [SerializeField] private float squashPixels = 0.05f;
        [SerializeField] private float stretchPixels = 0.05f;
        [SerializeField] private float waveDuration = 0.3f;
        [SerializeField] private float flashDuration = 0.2f;
        [SerializeField] private int flashCount = 2;

        [Header("Dependencies")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

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

            // Update knockback timer
            knockbackService.UpdateKnockbackTimer(Time.deltaTime);

            // Skip AI if knocked back
            if (knockbackService.IsKnockedBack())
                return;

            // Calculate distance to player
            float distanceToPlayer = model.playerTransform != null 
                ? Vector2.Distance(transform.position, model.playerTransform.position) 
                : float.MaxValue;

            // Update AI behavior
            aiService.UpdateBehavior(Time.deltaTime, distanceToPlayer);

            // Check for death
            if (healthService.IsDead())
            {
                HandleDeath();
            }
        }

        private void FixedUpdate()
        {
            if (!model.isInitialized)
                return;

            // Update physics
            aiService.UpdatePhysics(Time.fixedDeltaTime);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!model.isInitialized)
                return;

            // Check if colliding with player
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
            // Get required components
            rb = GetComponent<Rigidbody2D>();
            
            if (animator == null)
                animator = GetComponent<Animator>();
            
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            // Get view
            view = GetComponent<EnemyView>();
            if (view == null)
            {
                view = gameObject.AddComponent<EnemyView>();
            }

            // Find player
            Transform playerTransform = FindLocalPlayerTransform();
            if (playerTransform == null)
            {
                Debug.LogWarning($"[EnemyPresenter] Player not found for {gameObject.name}");
            }

            // Sync inspector values to model
            SyncInspectorToModel(playerTransform);

            // Initialize services
            healthService = new EnemyHealthService(model);
            knockbackService = new EnemyKnockbackService(model);
            combatService = new EnemyCombatService(model);
            aiService = new EnemyAIService(model);

            // Initialize each service
            healthService.Initialize(maxHealth);
            knockbackService.Initialize(this);
            combatService.Initialize(damagePopupPrefab);
            aiService.Initialize(transform);

            model.isInitialized = true;

            // Initialize view
            if (view != null)
            {
                view.Initialize(this);
            }

            Debug.Log($"[EnemyPresenter] {gameObject.name} initialized successfully");
        }

        private void SyncInspectorToModel(Transform playerTransform)
        {
            // Runtime references
            model.playerTransform = playerTransform;
            model.rb = rb;
            model.animator = animator;
            model.spriteRenderer = spriteRenderer;

            // Health
            model.maxHealth = maxHealth;
            model.currentHealth = maxHealth;

            // Detection
            model.detectionRange = detectionRange;
            model.attackRange = attackRange;
            model.fieldOfViewAngle = fieldOfViewAngle;
            model.playerLayer = playerLayer;
            model.obstacleLayer = obstacleLayer;

            // Movement
            model.moveSpeed = moveSpeed;
            model.chaseSpeed = chaseSpeed;
            model.wanderSpeed = wanderSpeed;
            model.wanderRange = wanderRange;

            // Guard
            model.guardDuration = guardDuration;
            model.guardLookDuration = guardLookDuration;

            // Combat
            model.damageAmount = damageAmount;
            model.knockbackForce = knockbackForce;
            model.damageThrottleTime = damageThrottleTime;

            // Physics
            model.friction = friction;
            model.maxVelocity = maxVelocity;

            // Knockback
            model.knockbackDuration = knockbackDuration;
            model.knockbackPushDistance = knockbackPushDistance;
            model.squashPixels = squashPixels;
            model.stretchPixels = stretchPixels;
            model.waveDuration = waveDuration;
            model.flashDuration = flashDuration;
            model.flashCount = flashCount;
        }

        private Transform FindLocalPlayerTransform()
        {
            // Method 1: Find by "Player" tag (multiplayer spawn point)
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject go in players)
            {
                Photon.Pun.PhotonView pv = go.GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    Debug.Log($"[EnemyPresenter] Found local player via 'Player' tag: {go.name}");
                    return go.transform;
                }
            }

            // Method 2: Find by "PlayerEntity" tag (test scene)
            GameObject[] playerEntities = GameObject.FindGameObjectsWithTag("PlayerEntity");
            foreach (GameObject go in playerEntities)
            {
                Photon.Pun.PhotonView pv = go.GetComponent<Photon.Pun.PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    Debug.Log($"[EnemyPresenter] Found local player via 'PlayerEntity' tag: {go.name}");
                    return go.transform;
                }
            }

            // Method 3: Fallback for test scenes (find by name)
            GameObject fallback = GameObject.Find("PlayerEntity");
            if (fallback != null)
            {
                Debug.LogWarning("[EnemyPresenter] Found PlayerEntity by name (fallback)");
                return fallback.transform;
            }

            // Method 4: Find ANY object with PlayerHealthPresenter (ultra fallback)
            PlayerHealthPresenter healthPresenter = Object.FindObjectOfType<PlayerHealthPresenter>();
            if (healthPresenter != null)
            {
                Debug.LogWarning("[EnemyPresenter] Found player via PlayerHealthPresenter component");
                return healthPresenter.transform;
            }

            Debug.LogError("[EnemyPresenter] Could not find player transform!");
            return null;
        }

        #endregion

        #region Public API - Damage & Knockback

        /// <summary>
        /// Called when enemy takes damage (e.g., from player attack).
        /// </summary>
        public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!model.isInitialized)
                return;

            // Apply damage
            healthService.ChangeHealth(-damage);

            // Apply knockback physics
            aiService.TakeKnockback(knockbackDirection, knockbackForce);

            // Play visual effects
            StartCoroutine(knockbackService.PlayKnockbackEffect());
            StartCoroutine(knockbackService.PlayFlashEffect());

            // Alert AI
            aiService.OnHit();

            Debug.Log($"[EnemyPresenter] {gameObject.name} took {damage} damage. Health: {healthService.GetCurrentHealth()}/{healthService.GetMaxHealth()}");
        }

        #endregion

        #region Death

        private void HandleDeath()
        {
            Debug.Log($"[EnemyPresenter] {gameObject.name} died");

            // Stop all movement
            aiService.Stop();

            // Play death animation (if exists)
            if (model.animator != null)
            {
                model.animator.SetTrigger("Death");
            }

            // Destroy after delay
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

        #region Public API for Other Systems

        public IEnemyHealthService GetHealthService() => healthService;
        public IEnemyKnockbackService GetKnockbackService() => knockbackService;
        public IEnemyCombatService GetCombatService() => combatService;
        public IEnemyAIService GetAIService() => aiService;

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                // Show detection range
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, detectionRange);

                // Show attack range
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, attackRange);
                return;
            }

            if (!model.isInitialized)
                return;

            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, model.detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, model.attackRange);

            // Wander range
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(model.startPosition, model.wanderRange);

            // Wander target
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, model.wanderTarget);
            Gizmos.DrawWireSphere(model.wanderTarget, 0.3f);

            // Field of view cone
            DrawFieldOfViewCone();

            // State indicators
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