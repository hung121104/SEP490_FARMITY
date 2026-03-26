using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using System.Collections.Generic;
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
    [RequireComponent(typeof(PhotonView))]
    public class EnemyPresenter : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        private const byte ENEMY_STATE_EVENT = 166;
        private const float STATE_BROADCAST_INTERVAL = 0.1f;
        private const float REMOTE_POSITION_LERP = 12f;

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
        private PhotonView enemyPhotonView;

        private readonly List<Transform> playerTargets = new List<Transform>();
        private float playerScanTimer;
        private float nextStateBroadcastAt;

        private Vector3 remotePosition;
        private Vector2 remoteVelocity;
        private bool remoteIsWalking;
        private bool remoteFlipX;
        private int lastAppliedHitToken = int.MinValue;
        private Coroutine knockbackEffectRoutine;
        private Coroutine flashEffectRoutine;
        private float lastDamagePopupAt = -10f;
        private const float DAMAGE_POPUP_INTERVAL = 0.1f;

        private bool IsAuthoritative => !PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
            EnemySyncManager.Instance.RegisterEnemy(this);
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
            if (EnemySyncManager.Instance != null)
                EnemySyncManager.Instance.UnregisterEnemy(this);
        }

        private void Update()
        {
            if (!model.isInitialized)
                return;

            if (IsAuthoritative)
            {
                RefreshPotentialTargets();
                aiService.SetPotentialTargets(playerTargets);
                knockbackService.UpdateKnockbackTimer(Time.deltaTime);

                if (!knockbackService.IsKnockedBack())
                    aiService.UpdateBehavior(Time.deltaTime);

                if (healthService.IsDead())
                    HandleDeath(true);

                BroadcastEnemyStateIfNeeded();
            }
            else
            {
                ApplyRemoteState();

                if (healthService.IsDead())
                    HandleDeath(false);
            }
        }

        private void FixedUpdate()
        {
            if (!model.isInitialized)
                return;

            if (IsAuthoritative)
                aiService.UpdatePhysics(Time.fixedDeltaTime);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!model.isInitialized)
                return;

            if (!IsAuthoritative)
                return;

            if (IsPlayerContact(other))
            {
                combatService.DealDamageToPlayer(other, transform);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision == null)
                return;

            // Backward-compatible fallback while prefabs migrate to trigger hitbox.
            OnTriggerStay2D(collision.otherCollider);
        }

        private bool IsPlayerContact(Collider2D other)
        {
            if (other == null)
                return false;

            if (((1 << other.gameObject.layer) & model.playerLayer) != 0)
                return true;

            if (other.CompareTag("Player") || other.CompareTag("PlayerEntity"))
                return true;

            Transform parent = other.transform;
            while (parent != null)
            {
                if (parent.CompareTag("Player") || parent.CompareTag("PlayerEntity"))
                    return true;
                parent = parent.parent;
            }

            return false;
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
            enemyPhotonView = GetComponent<PhotonView>();
            
            if (animator == null)
                animator = GetComponent<Animator>();
            
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            view = GetComponent<EnemyView>();
            if (view == null)
            {
                view = gameObject.AddComponent<EnemyView>();
            }

            // ✅ NEW: Sync from EnemyDataSO instead of inspector
            SyncFromEnemyData();

            healthService = new EnemyHealthService(model);
            knockbackService = new EnemyKnockbackService(model);
            combatService = new EnemyCombatService(model);
            aiService = new EnemyAIService(model);

            healthService.Initialize(enemyData.maxHealth);
            knockbackService.Initialize(this);
            combatService.Initialize(damagePopupPrefab);
            aiService.Initialize(transform);

            if (string.IsNullOrWhiteSpace(model.runtimeEnemyId))
            {
                model.runtimeEnemyId = BuildDefaultRuntimeEnemyId();
            }

            EnemySyncManager.Instance.RegisterEnemy(this);

            remotePosition = transform.position;
            remoteVelocity = Vector2.zero;
            remoteIsWalking = false;
            remoteFlipX = spriteRenderer != null && spriteRenderer.flipX;

            model.isInitialized = true;

            if (view != null)
            {
                view.Initialize(this);
            }

            Debug.Log($"[EnemyPresenter] {gameObject.name} (ID: {enemyId}) initialized from {enemyData.name}");
        }

        // ✅ NEW: Load all settings from EnemyDataSO
        private void SyncFromEnemyData()
        {
            // Runtime references
            model.playerTransform = null;
            model.currentTarget = null;
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

        private void RefreshPotentialTargets()
        {
            playerScanTimer -= Time.deltaTime;
            if (playerScanTimer > 0f)
                return;

            playerScanTimer = 0.5f;
            playerTargets.Clear();

            GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                    playerTargets.Add(players[i].transform);
            }

            players = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && !playerTargets.Contains(players[i].transform))
                    playerTargets.Add(players[i].transform);
            }
        }

        #endregion

        #region Public API

        public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!model.isInitialized)
                return;

            if (PhotonNetwork.IsConnected && !IsAuthoritative)
            {
                if (enemyPhotonView != null)
                {
                    enemyPhotonView.RPC(
                        nameof(RPC_RequestTakeDamage),
                        RpcTarget.MasterClient,
                        damage,
                        knockbackDirection.x,
                        knockbackDirection.y,
                        knockbackForce);
                }

                return;
            }

            ApplyDamageInternal(damage, knockbackDirection, knockbackForce);
        }

        public bool IsDead() => healthService?.IsDead() ?? false;

        public void ApplyAuthoritativeHit(
            int damage,
            Vector2 knockbackDirection,
            float knockbackForce,
            int hitToken,
            int attackerActorNumber)
        {
            lastAppliedHitToken = hitToken;
            ApplyDamageInternal(damage, knockbackDirection, knockbackForce);
        }

        public void ApplyReplicatedHitState(
            int newHp,
            int maxHp,
            Vector2 knockbackDirection,
            float knockbackForce,
            int damage,
            int hitToken,
            bool isDead)
        {
            if (hitToken == lastAppliedHitToken)
                return;

            lastAppliedHitToken = hitToken;

            int currentHp = healthService?.GetCurrentHealth() ?? model.currentHealth;
            int hpDelta = newHp - currentHp;
            if (hpDelta != 0)
                healthService?.ChangeHealth(hpDelta);

            model.maxHealth = maxHp;

            if (!isDead)
            {
                aiService?.TakeKnockback(knockbackDirection, knockbackForce);
                PlayHitEffects();
                aiService?.OnHit();
            }

            TrySpawnDamagePopup(damage);
        }

        [PunRPC]
        private void RPC_RequestTakeDamage(int damage, float knockbackX, float knockbackY, float knockbackForce)
        {
            if (!IsAuthoritative)
                return;

            ApplyDamageInternal(damage, new Vector2(knockbackX, knockbackY), knockbackForce);
        }

        private void ApplyDamageInternal(int damage, Vector2 knockbackDirection, float knockbackForce)
        {
            if (!model.isInitialized)
                return;

            healthService.ChangeHealth(-damage);
            aiService.TakeKnockback(knockbackDirection, knockbackForce);

            PlayHitEffects();

            TrySpawnDamagePopup(damage);
            aiService.OnHit();
        }

        private void PlayHitEffects()
        {
            if (knockbackService == null)
                return;

            if (knockbackEffectRoutine != null)
                StopCoroutine(knockbackEffectRoutine);
            if (flashEffectRoutine != null)
                StopCoroutine(flashEffectRoutine);

            knockbackEffectRoutine = StartCoroutine(knockbackService.PlayKnockbackEffect());
            flashEffectRoutine = StartCoroutine(knockbackService.PlayFlashEffect());
        }

        private void TrySpawnDamagePopup(int damage)
        {
            if (damage <= 0)
                return;

            if (Time.time - lastDamagePopupAt < DAMAGE_POPUP_INTERVAL)
                return;

            lastDamagePopupAt = Time.time;
            DamagePopupPresenter.Spawn(transform.position, damage);
        }

        // ✅ NEW: Get enemy ID
        public string GetEnemyId() => enemyId;
        public string GetRuntimeEnemyId() => model.runtimeEnemyId;
        public EnemyDataSO GetEnemyData() => enemyData;

        public void SetRuntimeEnemyId(string runtimeId)
        {
            if (!string.IsNullOrWhiteSpace(runtimeId))
            {
                model.runtimeEnemyId = runtimeId;
                EnemySyncManager.Instance.RegisterEnemy(this);
            }
        }

        #endregion

        #region Death

        // ✅ NEW: Track if death has been handled
        private bool deathHandled = false;

        private void HandleDeath(bool authoritativeDeath)
        {
            // ✅ FIX: Only handle death ONCE
            if (deathHandled)
                return;

            deathHandled = true;

            Debug.Log($"[EnemyPresenter] {enemyId} died");

            if (authoritativeDeath)
            {
                // ✅ Fire achievement event with enemy ID - called ONCE
                GameEventBus.FireEnemyKilled(enemyId, 1);
            }

            aiService.Stop();

            if (model.animator != null)
            {
                model.animator.SetTrigger("Death");
            }

            Destroy(gameObject, 1f);
        }

        private string BuildDefaultRuntimeEnemyId()
        {
            string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "scene";
            Vector3 pos = transform.position;
            return $"{enemyId}_{sceneName}_{gameObject.name}_{pos.x:F2}_{pos.y:F2}";
        }

        private void BroadcastEnemyStateIfNeeded()
        {
            if (!PhotonNetwork.IsConnected)
                return;

            if (Time.unscaledTime < nextStateBroadcastAt)
                return;

            if (string.IsNullOrWhiteSpace(model.runtimeEnemyId))
                return;

            bool isWalking = model.animator != null && model.animator.GetBool("isWalking");
            bool flipX = model.spriteRenderer != null && model.spriteRenderer.flipX;

            object[] payload =
            {
                model.runtimeEnemyId,
                transform.position.x,
                transform.position.y,
                transform.position.z,
                model.rb != null ? model.rb.linearVelocity.x : 0f,
                model.rb != null ? model.rb.linearVelocity.y : 0f,
                model.currentHealth,
                model.maxHealth,
                (int)model.currentState,
                model.isAlerted,
                model.isKnockedBack,
                isWalking,
                flipX,
                model.facingDirection.x,
                model.facingDirection.y,
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(ENEMY_STATE_EVENT, payload, options, SendOptions.SendUnreliable);
            nextStateBroadcastAt = Time.unscaledTime + STATE_BROADCAST_INTERVAL;
        }

        private void ApplyRemoteState()
        {
            transform.position = Vector3.Lerp(
                transform.position,
                remotePosition,
                Mathf.Clamp01(Time.deltaTime * REMOTE_POSITION_LERP));

            if (model.rb != null)
                model.rb.linearVelocity = remoteVelocity;

            if (model.animator != null)
                model.animator.SetBool("isWalking", remoteIsWalking);

            if (model.spriteRenderer != null)
                model.spriteRenderer.flipX = remoteFlipX;
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != ENEMY_STATE_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 15)
                return;

            string runtimeId = payload[0] as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(runtimeId) || runtimeId != model.runtimeEnemyId)
                return;

            if (!TryGetFloat(payload, 1, out float posX) ||
                !TryGetFloat(payload, 2, out float posY) ||
                !TryGetFloat(payload, 3, out float posZ) ||
                !TryGetFloat(payload, 4, out float velX) ||
                !TryGetFloat(payload, 5, out float velY) ||
                !TryGetInt(payload, 6, out int hp) ||
                !TryGetInt(payload, 7, out int maxHp) ||
                !TryGetInt(payload, 8, out int stateValue) ||
                !TryGetBool(payload, 9, out bool isAlerted) ||
                !TryGetBool(payload, 10, out bool isKnockedBack) ||
                !TryGetBool(payload, 11, out bool isWalking) ||
                !TryGetBool(payload, 12, out bool flipX) ||
                !TryGetFloat(payload, 13, out float faceX) ||
                !TryGetFloat(payload, 14, out float faceY))
            {
                return;
            }

            remotePosition = new Vector3(posX, posY, posZ);
            remoteVelocity = new Vector2(velX, velY);
            remoteIsWalking = isWalking;
            remoteFlipX = flipX;
            model.currentHealth = hp;
            model.maxHealth = maxHp;
            model.currentState = (EnemyState)stateValue;
            model.isAlerted = isAlerted;
            model.isKnockedBack = isKnockedBack;
            model.facingDirection = new Vector2(faceX, faceY);
        }

        private static bool TryGetFloat(object[] payload, int index, out float value)
        {
            value = 0f;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is float f)
            {
                value = f;
                return true;
            }

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            return false;
        }

        private static bool TryGetInt(object[] payload, int index, out int value)
        {
            value = 0;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is int i)
            {
                value = i;
                return true;
            }

            if (payload[index] is byte b)
            {
                value = b;
                return true;
            }

            return false;
        }

        private static bool TryGetBool(object[] payload, int index, out bool value)
        {
            value = false;
            if (index < 0 || index >= payload.Length || payload[index] == null)
                return false;

            if (payload[index] is bool b)
            {
                value = b;
                return true;
            }

            return false;
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => model.isInitialized;
        public EnemyState GetCurrentState() => aiService?.GetCurrentState() ?? EnemyState.Guard;
        public bool IsAlerted() => aiService?.IsAlerted() ?? false;
        public bool IsKnockedBack() => knockbackService?.IsKnockedBack() ?? false;
        public int GetCurrentHealth() => healthService?.GetCurrentHealth() ?? 0;
        public int GetMaxHealth() => healthService?.GetMaxHealth() ?? 1;
        public int GetContactDamageAmount() => model.damageAmount;
        public float GetContactKnockbackForce() => model.knockbackForce;
        public float GetContactDamageThrottleTime() => model.damageThrottleTime;
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