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

        public static event System.Action<string, string, Vector3> OnEnemyAuthoritativeDeath;

        [Header("Model")]
        [SerializeField] private EnemyModel model = new EnemyModel();

        // ✅ NEW: Enemy data reference
        [Header("Enemy Data")]
        [SerializeField] private EnemyDataSO enemyData;

        [Header("Dependencies")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private GameObject damagePopupPrefab;
        [SerializeField] private EnemyAttackHitbox attackHitbox;

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
        private bool remoteFacingRight;
        private int remoteAttackSequence;
        private int lastAppliedRemoteAttackSequence = -1;
        private Vector3 attackHitboxBaseLocalPosition;
        private bool attackHitboxPositionCaptured;
        private int lastAppliedHitToken = int.MinValue;
        private Coroutine knockbackEffectRoutine;
        private Coroutine flashEffectRoutine;
        private Coroutine deathFinalizeFallbackRoutine;
        private float lastDamagePopupAt = -10f;
        private const float DAMAGE_POPUP_INTERVAL = 0.1f;
        private const string ATTACK_TRIGGER = "Attack";
        private bool hasGuardAnchorOverride;
        private Vector3 guardAnchorOverride;
        private bool hasRuntimeProgressionOverride;
        private int runtimeEnemyLevel = 1;
        private int runtimeBaseExp = 10;

        private readonly List<Collider2D> activeAttackTargets = new List<Collider2D>();

        [Header("Death Animation")]
        [SerializeField] private string deathTriggerName = "Death";
        [SerializeField] private bool finishDeathByAnimationEvent = true;
        [SerializeField] private float deathDespawnFallbackSeconds = 1.2f;

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

            if (deathFinalizeFallbackRoutine != null)
            {
                StopCoroutine(deathFinalizeFallbackRoutine);
                deathFinalizeFallbackRoutine = null;
            }
        }

        private void Update()
        {
            if (!model.isInitialized)
                return;

            UpdateAttackHitboxFacing();

            if (IsAuthoritative)
            {
                RefreshPotentialTargets();
                aiService.SetPotentialTargets(playerTargets);
                knockbackService.UpdateKnockbackTimer(Time.deltaTime);

                if (!knockbackService.IsKnockedBack())
                    aiService.UpdateBehavior(Time.deltaTime);

                TryOutOfCombatRegeneration();

                TryTriggerAttackAnimation();

                if (healthService.IsDead())
                    HandleDeath(true);

                BroadcastEnemyStateIfNeeded();
            }
            else
            {
                if (healthService.IsDead())
                {
                    HandleDeath(false);
                    return;
                }

                ApplyRemoteState();
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

            if (model.useActiveAttack)
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

            Collider2D mainCollider = GetComponent<Collider2D>();
            if (mainCollider != null && mainCollider.isTrigger)
            {
                Debug.LogWarning($"[EnemyPresenter] {gameObject.name} main collider is Trigger. Enemies will not physically block each other.");
            }

            view = GetComponent<EnemyView>();
            if (view == null)
            {
                view = gameObject.AddComponent<EnemyView>();
            }

            // ✅ NEW: Sync from EnemyDataSO instead of inspector
            SyncFromEnemyData();
            ApplyRuntimeProgression();

            healthService = new EnemyHealthService(model);
            knockbackService = new EnemyKnockbackService(model);
            combatService = new EnemyCombatService(model);
            aiService = new EnemyAIService(model);

            healthService.Initialize(model.maxHealth);
            knockbackService.Initialize(this);
            combatService.Initialize(damagePopupPrefab);
            aiService.Initialize(transform);

            ApplyGuardAnchorOverrideIfPresent();

            if (string.IsNullOrWhiteSpace(model.runtimeEnemyId))
            {
                model.runtimeEnemyId = BuildDefaultRuntimeEnemyId();
            }

            EnemySyncManager.Instance.RegisterEnemy(this);

            remotePosition = transform.position;
            remoteVelocity = Vector2.zero;
            remoteIsWalking = false;
            remoteFacingRight = transform.localScale.x >= 0f;
            remoteAttackSequence = 0;

            if (attackHitbox != null)
            {
                attackHitboxBaseLocalPosition = attackHitbox.transform.localPosition;
                attackHitboxPositionCaptured = true;
            }

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
            model.lastHitAt = -999f;
            model.regenProgress = 0f;

            model.enableOutOfCombatRegen = enemyData.enableOutOfCombatRegen;
            model.regenDelaySeconds = enemyData.regenDelaySeconds;
            model.regenHpPerSecond = enemyData.regenHpPerSecond;
            model.regenRequireNearGuardAnchor = enemyData.regenRequireNearGuardAnchor;
            model.regenGuardProximity = enemyData.regenGuardProximity;

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
            model.enableSeparation = enemyData.enableSeparation;
            model.separationRadius = enemyData.separationRadius;
            model.separationForce = enemyData.separationForce;

            // Guard
            model.guardDuration = enemyData.guardDuration;
            model.guardLookDuration = enemyData.guardLookDuration;

            // Combat
            model.damageAmount = enemyData.damageAmount;
            model.baseExp = Mathf.Max(1, enemyData.baseExp);
            model.enemyLevel = Mathf.Max(1, runtimeEnemyLevel);
            model.knockbackForce = enemyData.knockbackForce;
            model.damageThrottleTime = enemyData.damageThrottleTime;
            model.useActiveAttack = enemyData.useActiveAttack;
            model.attackCooldown = enemyData.attackCooldown;
            model.attackRecovery = enemyData.attackRecovery;
            model.attackFrontDotThreshold = enemyData.attackFrontDotThreshold;

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

        private void ApplyRuntimeProgression()
        {
            int level = Mathf.Max(1, hasRuntimeProgressionOverride ? runtimeEnemyLevel : 1);
            int baseExp = Mathf.Max(1, hasRuntimeProgressionOverride ? runtimeBaseExp : enemyData.baseExp);

            int levelDelta = Mathf.Max(0, level - 1);
            float hpMultiplier = 1f + (levelDelta * 0.2f);
            float damageMultiplier = 1f + (levelDelta * 0.12f);

            model.enemyLevel = level;
            model.baseExp = baseExp;
            model.maxHealth = Mathf.Max(1, Mathf.RoundToInt(enemyData.maxHealth * hpMultiplier));
            model.currentHealth = model.maxHealth;
            model.damageAmount = Mathf.Max(1, Mathf.RoundToInt(enemyData.damageAmount * damageMultiplier));
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
                TryAddPotentialTarget(players[i]);
            }

            players = GameObject.FindGameObjectsWithTag("Player");
            for (int i = 0; i < players.Length; i++)
            {
                TryAddPotentialTarget(players[i]);
            }
        }

        private void TryAddPotentialTarget(GameObject candidate)
        {
            if (candidate == null || !candidate.activeInHierarchy)
                return;

            PlayerMovement movement = candidate.GetComponent<PlayerMovement>() ?? candidate.GetComponentInParent<PlayerMovement>();
            if (movement == null)
                return;

            Transform targetRoot = movement.transform;

            if (PhotonNetwork.IsConnected)
            {
                PhotonView pv = targetRoot.GetComponent<PhotonView>() ?? targetRoot.GetComponentInChildren<PhotonView>(true);
                if (pv == null || pv.Owner == null)
                    return;
            }

            if (!playerTargets.Contains(targetRoot))
                playerTargets.Add(targetRoot);
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
            model.lastHitAt = Time.time;
            model.regenProgress = 0f;
            aiService.TakeKnockback(knockbackDirection, knockbackForce);

            PlayHitEffects();

            TrySpawnDamagePopup(damage);
            aiService.OnHit();
        }

        private void TryOutOfCombatRegeneration()
        {
            if (!IsAuthoritative || !model.enableOutOfCombatRegen || healthService == null || aiService == null)
                return;

            if (healthService.IsDead())
                return;

            int currentHp = healthService.GetCurrentHealth();
            int maxHp = healthService.GetMaxHealth();
            if (currentHp >= maxHp)
            {
                model.regenProgress = 0f;
                return;
            }

            if (Time.time < model.lastHitAt + Mathf.Max(0f, model.regenDelaySeconds))
            {
                model.regenProgress = 0f;
                return;
            }

            if (model.currentTarget != null || aiService.IsAlerted())
            {
                model.regenProgress = 0f;
                return;
            }

            EnemyState state = aiService.GetCurrentState();
            if (state == EnemyState.Chasing || state == EnemyState.Attacking)
            {
                model.regenProgress = 0f;
                return;
            }

            if (model.regenRequireNearGuardAnchor)
            {
                float distanceToGuard = Vector2.Distance(transform.position, model.startPosition);
                if (distanceToGuard > Mathf.Max(0f, model.regenGuardProximity))
                {
                    model.regenProgress = 0f;
                    return;
                }
            }

            float regenRate = Mathf.Max(0f, model.regenHpPerSecond);
            if (regenRate <= 0f)
                return;

            model.regenProgress += regenRate * Time.deltaTime;
            int healAmount = Mathf.FloorToInt(model.regenProgress);
            if (healAmount <= 0)
                return;

            model.regenProgress -= healAmount;
            healthService.ChangeHealth(healAmount);
            TrySpawnHealingPopup(healAmount);
        }

        private void TrySpawnHealingPopup(int healAmount)
        {
            if (healAmount <= 0)
                return;

            if (Time.time - lastDamagePopupAt < DAMAGE_POPUP_INTERVAL)
                return;

            lastDamagePopupAt = Time.time;
            DamagePopupPresenter.Spawn(transform.position, healAmount, PopupType.Heal);
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

        private void TryTriggerAttackAnimation()
        {
            if (aiService == null || model.animator == null || !model.useActiveAttack)
                return;

            if (!aiService.ConsumePendingAttackTrigger())
                return;

            model.animator.SetTrigger(ATTACK_TRIGGER);
        }

        // Called by enemy attack animation event at impact frame.
        public void OnAttackImpactAnimationEvent()
        {
            if (!model.isInitialized || !model.useActiveAttack)
                return;

            if (!IsAuthoritative || aiService == null || combatService == null)
                return;

            if (!aiService.TryConsumeAttackImpact())
                return;

            if (attackHitbox == null)
                return;

            attackHitbox.CollectOverlappingPlayers(activeAttackTargets);
            combatService.DealDamageToPlayers(activeAttackTargets, transform);
        }

        // Called by enemy attack animation event at end frame.
        public void OnAttackAnimationEndEvent()
        {
            if (!model.isInitialized || !model.useActiveAttack)
                return;

            if (!IsAuthoritative || aiService == null)
                return;

            aiService.CompleteAttackAnimation();
        }

        // ✅ NEW: Get enemy ID
        public string GetEnemyId() => enemyId;
        public string GetEnemyDisplayName()
        {
            if (enemyData != null && !string.IsNullOrWhiteSpace(enemyData.enemyName))
                return enemyData.enemyName;

            return string.IsNullOrWhiteSpace(enemyId) ? "Enemy" : enemyId;
        }
        public string GetRuntimeEnemyId() => model.runtimeEnemyId;
        public EnemyDataSO GetEnemyData() => enemyData;
        public int GetEnemyLevel() => model.enemyLevel;
        public int GetBaseExp() => model.baseExp;

        public void SetRuntimeEnemyId(string runtimeId)
        {
            if (!string.IsNullOrWhiteSpace(runtimeId))
            {
                model.runtimeEnemyId = runtimeId;
                EnemySyncManager.Instance.RegisterEnemy(this);
            }
        }

        public void SetGuardAnchor(Vector3 anchorWorldPosition)
        {
            hasGuardAnchorOverride = true;
            guardAnchorOverride = anchorWorldPosition;
            ApplyGuardAnchorOverrideIfPresent();
        }

        public void SetRuntimeProgression(int enemyLevel, int baseExp)
        {
            hasRuntimeProgressionOverride = true;
            runtimeEnemyLevel = Mathf.Max(1, enemyLevel);
            runtimeBaseExp = Mathf.Max(1, baseExp);

            if (!model.isInitialized)
                return;

            ApplyRuntimeProgression();
        }

        private void ApplyGuardAnchorOverrideIfPresent()
        {
            if (!hasGuardAnchorOverride)
                return;

            model.startPosition = guardAnchorOverride;
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
                EnemySyncManager.Instance.ProcessEnemyDeathReward(model.runtimeEnemyId, model.enemyLevel, model.baseExp);
                OnEnemyAuthoritativeDeath?.Invoke(model.runtimeEnemyId, enemyId, transform.position);
            }

            HardStopEnemyForDeath();

            if (model.animator != null)
            {
                model.animator.SetBool("isWalking", false);
                model.animator.ResetTrigger(ATTACK_TRIGGER);
                model.animator.SetTrigger(string.IsNullOrWhiteSpace(deathTriggerName) ? "Death" : deathTriggerName);
            }

            if (deathFinalizeFallbackRoutine != null)
                StopCoroutine(deathFinalizeFallbackRoutine);

            deathFinalizeFallbackRoutine = StartCoroutine(DeathFinalizeFallbackCoroutine());
        }

        // Called by enemy death animation event at the final frame.
        public void OnDeathAnimationFinishedEvent()
        {
            if (!deathHandled)
                return;

            FinalizeDeathDestroy();
        }

        private IEnumerator DeathFinalizeFallbackCoroutine()
        {
            float waitSeconds = Mathf.Max(0.05f, deathDespawnFallbackSeconds);
            yield return new WaitForSeconds(waitSeconds);

            // If animator event was missed/not configured, fallback still cleans up.
            FinalizeDeathDestroy();
        }

        private void FinalizeDeathDestroy()
        {
            if (this == null || gameObject == null)
                return;

            if (deathFinalizeFallbackRoutine != null)
            {
                StopCoroutine(deathFinalizeFallbackRoutine);
                deathFinalizeFallbackRoutine = null;
            }

            Destroy(gameObject);
        }

        private void HardStopEnemyForDeath()
        {
            aiService?.Stop();

            if (knockbackEffectRoutine != null)
            {
                StopCoroutine(knockbackEffectRoutine);
                knockbackEffectRoutine = null;
            }

            if (flashEffectRoutine != null)
            {
                StopCoroutine(flashEffectRoutine);
                flashEffectRoutine = null;
            }

            // If knockback flash coroutine was interrupted mid-red frame,
            // force visual state back to the enemy's base sprite color before death anim starts.
            if (model.spriteRenderer != null)
                model.spriteRenderer.color = model.originalColor;

            // If squash/stretch was interrupted, restore base scale for clean death VFX.
            transform.localScale = model.originalScale;

            model.isKnockedBack = false;
            model.isAttackAnimating = false;
            model.pendingAttackTrigger = false;
            model.hasAppliedImpactThisAttack = false;

            if (model.rb != null)
            {
                model.rb.linearVelocity = Vector2.zero;
                model.rb.angularVelocity = 0f;
                model.rb.simulated = false;
            }

            if (attackHitbox != null)
                attackHitbox.enabled = false;

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
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
            bool facingRight = transform.localScale.x >= 0f;

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
                facingRight,
                model.facingDirection.x,
                model.facingDirection.y,
                aiService != null ? aiService.GetAttackSequence() : 0,
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

            TryApplyRemoteAttackAnimation();

            ApplyRemoteFacing(remoteFacingRight);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != ENEMY_STATE_EVENT)
                return;

            if (photonEvent.CustomData is not object[] payload || payload.Length < 16)
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
                !TryGetBool(payload, 12, out bool facingRight) ||
                !TryGetFloat(payload, 13, out float faceX) ||
                !TryGetFloat(payload, 14, out float faceY) ||
                !TryGetInt(payload, 15, out int attackSequence))
            {
                return;
            }

            remotePosition = new Vector3(posX, posY, posZ);
            remoteVelocity = new Vector2(velX, velY);
            remoteIsWalking = isWalking;
            remoteFacingRight = facingRight;
            model.currentHealth = hp;
            model.maxHealth = maxHp;
            model.currentState = (EnemyState)stateValue;
            model.isAlerted = isAlerted;
            model.isKnockedBack = isKnockedBack;
            model.facingDirection = new Vector2(faceX, faceY);
            remoteAttackSequence = attackSequence;
        }

        private void TryApplyRemoteAttackAnimation()
        {
            if (model.animator == null || !model.useActiveAttack)
                return;

            if (remoteAttackSequence <= 0 || remoteAttackSequence == lastAppliedRemoteAttackSequence)
                return;

            lastAppliedRemoteAttackSequence = remoteAttackSequence;
            model.animator.SetTrigger(ATTACK_TRIGGER);
        }

        private void ApplyRemoteFacing(bool facingRight)
        {
            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);
            if (absX <= 0.0001f)
                absX = 1f;

            scale.x = facingRight ? absX : -absX;
            transform.localScale = scale;

            if (model.spriteRenderer != null)
                model.spriteRenderer.flipX = false;
        }

        private void UpdateAttackHitboxFacing()
        {
            if (attackHitbox == null)
                return;

            if (!attackHitboxPositionCaptured)
            {
                attackHitboxBaseLocalPosition = attackHitbox.transform.localPosition;
                attackHitboxPositionCaptured = true;
            }

            float facingX = model.facingDirection.x;
            if (Mathf.Abs(facingX) < 0.001f)
                return;

            // Desired world-space side comes from facingDirection.
            // Because enemy visual facing now uses transform.localScale.x mirroring,
            // local hitbox X must compensate for parent scale sign.
            float desiredWorldSign = facingX >= 0f ? 1f : -1f;
            float parentScaleSign = transform.localScale.x >= 0f ? 1f : -1f;
            float localDirectionSign = desiredWorldSign * parentScaleSign;

            Vector3 local = attackHitbox.transform.localPosition;
            local.x = Mathf.Abs(attackHitboxBaseLocalPosition.x) * localDirectionSign;
            attackHitbox.transform.localPosition = local;
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