using UnityEngine;
using Photon.Pun;
using CombatManager.Model;
using System.Collections.Generic;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for enemy AI state machine, movement, and behavior.
    /// Integrated with physics (merged from EnemyMovement).
    /// </summary>
    public class EnemyAIService : IEnemyAIService
    {
        private const float AlertTargetExtraRange = 4f;

        private EnemyModel model;
        private Transform enemyTransform;
        private readonly List<Transform> potentialTargets = new List<Transform>();

        public EnemyAIService(EnemyModel model)
        {
            this.model = model;
        }

        #region Initialization

        public void Initialize(Transform enemyTransform)
        {
            this.enemyTransform = enemyTransform;
            model.startPosition = enemyTransform.position;
            GenerateNewWanderTarget();
            model.currentState = EnemyState.Guard;
            StartGuard();
        }

        public void SetPotentialTargets(IReadOnlyList<Transform> players)
        {
            potentialTargets.Clear();
            if (players == null)
                return;

            for (int i = 0; i < players.Count; i++)
            {
                Transform target = players[i];
                if (target != null)
                    potentialTargets.Add(target);
            }
        }

        #endregion

        #region Behavior Update

        public void UpdateBehavior(float deltaTime)
        {
            ResolveCurrentTarget();

            float distanceToTarget = model.currentTarget != null
                ? Vector2.Distance(enemyTransform.position, model.currentTarget.position)
                : float.MaxValue;

            if (model.currentTarget == null && !model.isAlerted &&
                (model.currentState == EnemyState.Chasing || model.currentState == EnemyState.Attacking))
            {
                BeginReturnToGuardRange();
            }

            // State machine
            switch (model.currentState)
            {
                case EnemyState.Guard:
                    HandleGuardState(distanceToTarget);
                    break;

                case EnemyState.Wandering:
                    HandleWanderingState(distanceToTarget);
                    break;

                case EnemyState.Chasing:
                    HandleChasingState(distanceToTarget);
                    break;

                case EnemyState.Attacking:
                    HandleAttackingState(distanceToTarget);
                    break;
            }

            // Update timers
            if (model.isAlerted)
            {
                model.alertTimer -= deltaTime;
                if (model.alertTimer <= 0f)
                {
                    model.isAlerted = false;
                }
            }

            if (model.currentState == EnemyState.Guard)
            {
                model.guardTimer -= deltaTime;
            }

            UpdateAnimation();
        }

        public void UpdatePhysics(float fixedDeltaTime)
        {
            if (model.rb == null)
                return;

            // Don't override velocity while knocked back
            if (model.isKnockedBack)
            {
                ApplyFriction();
                ClampVelocity();
                return;
            }

            // State-based movement
            switch (model.currentState)
            {
                case EnemyState.Wandering:
                    MoveWander();
                    break;

                case EnemyState.Chasing:
                    MoveTowardsPlayer();
                    break;

                case EnemyState.Guard:
                case EnemyState.Attacking:
                    ApplyFriction();
                    break;
            }

                    ApplyEnemySeparation();

            ClampVelocity();
        }

        #endregion

        #region State Handlers

        private void HandleGuardState(float distanceToPlayer)
        {
            if (model.isAlerted)
            {
                model.currentState = EnemyState.Chasing;
                return;
            }

            if (distanceToPlayer <= model.detectionRange && CanSeePlayer())
            {
                model.currentState = EnemyState.Chasing;
                return;
            }

            if (model.guardTimer <= 0f)
            {
                model.currentState = EnemyState.Wandering;
                GenerateNewWanderTarget();
            }
        }

        private void HandleWanderingState(float distanceToPlayer)
        {
            if (model.isAlerted)
            {
                model.currentState = EnemyState.Chasing;
                return;
            }

            if (distanceToPlayer <= model.detectionRange && CanSeePlayer())
            {
                model.currentState = EnemyState.Chasing;
                return;
            }

            if (Vector2.Distance(enemyTransform.position, model.wanderTarget) < 0.3f)
            {
                model.currentState = EnemyState.Guard;
                StartGuard();
            }
        }

        private void HandleChasingState(float distanceToPlayer)
        {
            if (model.currentTarget == null)
            {
                if (!model.isAlerted)
                {
                    BeginReturnToGuardRange();
                }
                return;
            }

            if (distanceToPlayer <= model.attackRange)
            {
                model.currentState = EnemyState.Attacking;
                return;
            }

            if (distanceToPlayer > model.detectionRange + 2f)
            {
                BeginReturnToGuardRange();
                return;
            }

            if (!CanSeePlayer() && distanceToPlayer > model.detectionRange)
            {
                BeginReturnToGuardRange();
            }
        }

        private void HandleAttackingState(float distanceToPlayer)
        {
            if (model.currentTarget == null)
            {
                model.isAttackAnimating = false;
                if (model.isAlerted)
                {
                    model.currentState = EnemyState.Chasing;
                }
                else
                {
                    BeginReturnToGuardRange();
                }
                return;
            }

            UpdateFacingToTarget(model.currentTarget);

            if (model.isAttackAnimating && Time.time >= model.attackTimeoutAt)
            {
                CompleteAttackAnimation();
            }

            if (distanceToPlayer > model.attackRange + 0.5f)
            {
                model.isAttackAnimating = false;
                model.currentState = EnemyState.Chasing;
                return;
            }

            if (!model.useActiveAttack)
                return;

            if (model.isAttackAnimating)
                return;

            if (Time.time < model.nextAttackTime)
                return;

            if (!IsTargetInFront(model.currentTarget))
                return;

            StartAttackAnimation();
        }

        private bool IsTargetInFront(Transform target)
        {
            if (target == null)
                return false;

            Vector2 toTarget = ((Vector2)(target.position - enemyTransform.position)).normalized;
            Vector2 forward = model.facingDirection.sqrMagnitude > 0.0001f
                ? model.facingDirection.normalized
                : Vector2.right;

            float dot = Vector2.Dot(forward, toTarget);
            return dot >= model.attackFrontDotThreshold;
        }

        private void StartAttackAnimation()
        {
            model.isAttackAnimating = true;
            model.hasAppliedImpactThisAttack = false;
            model.pendingAttackTrigger = true;
            model.nextAttackTime = Time.time + Mathf.Max(0.05f, model.attackCooldown + model.attackRecovery);
            model.attackTimeoutAt = Time.time + Mathf.Max(0.35f, model.attackCooldown + 0.5f);
            model.attackSequence++;

            if (model.rb != null)
                model.rb.linearVelocity = Vector2.zero;
        }

        public bool ConsumePendingAttackTrigger()
        {
            if (!model.pendingAttackTrigger)
                return false;

            model.pendingAttackTrigger = false;
            return true;
        }

        public bool TryConsumeAttackImpact()
        {
            if (!model.useActiveAttack || !model.isAttackAnimating || model.hasAppliedImpactThisAttack)
                return false;

            model.hasAppliedImpactThisAttack = true;
            return true;
        }

        public void CompleteAttackAnimation()
        {
            model.isAttackAnimating = false;
            model.hasAppliedImpactThisAttack = false;
            model.pendingAttackTrigger = false;
            model.attackTimeoutAt = 0f;

            if (model.currentTarget == null)
            {
                BeginReturnToGuardRange();
            }
            else if (model.currentState == EnemyState.Attacking)
            {
                model.currentState = EnemyState.Chasing;
            }
        }

        private void ResolveCurrentTarget()
        {
            Transform bestTarget = null;
            float bestDistance = float.MaxValue;
            float maxTargetRange = model.detectionRange + (model.isAlerted ? AlertTargetExtraRange : 2f);

            for (int i = 0; i < potentialTargets.Count; i++)
            {
                Transform candidate = potentialTargets[i];
                if (candidate == null)
                    continue;

                if (IsTargetDefeated(candidate))
                    continue;

                float distance = Vector2.Distance(enemyTransform.position, candidate.position);
                if (distance > maxTargetRange)
                    continue;

                bool hasLineOfSight = CanSeeTarget(candidate);
                bool inCloseCombatRange = distance <= model.attackRange + 1f;
                bool alertedFallback = model.isAlerted && distance <= model.detectionRange + AlertTargetExtraRange;
                if (!hasLineOfSight && !inCloseCombatRange && !alertedFallback)
                    continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = candidate;
                }
            }

            model.currentTarget = bestTarget;
            model.playerTransform = bestTarget;

            if (bestTarget != null && model.currentState == EnemyState.Guard)
            {
                model.currentState = EnemyState.Chasing;
            }
        }

        private void BeginReturnToGuardRange()
        {
            model.isAlerted = false;
            model.currentTarget = null;
            model.playerTransform = null;
            model.currentState = EnemyState.Wandering;
            model.wanderTarget = model.startPosition;
            model.currentWanderDirection = (model.startPosition - enemyTransform.position).normalized;
        }

        #endregion

        #region Physics (Merged from EnemyMovement)

        public void ApplyFriction()
        {
            if (model.rb == null)
                return;

            model.rb.linearVelocity = Vector2.Lerp(
                model.rb.linearVelocity,
                Vector2.zero,
                Time.fixedDeltaTime * model.friction
            );
        }

        public void ClampVelocity()
        {
            if (model.rb == null)
                return;

            if (model.rb.linearVelocity.magnitude > model.maxVelocity)
            {
                model.rb.linearVelocity = model.rb.linearVelocity.normalized * model.maxVelocity;
            }
        }

        public void TakeKnockback(Vector2 direction, float force)
        {
            if (model.rb == null)
                return;

            model.rb.linearVelocity = direction * force;
            model.isKnockedBack = true;
            model.knockbackTimer = model.knockbackDuration;
        }

        public void Stop()
        {
            if (model.rb == null)
                return;

            model.rb.linearVelocity = Vector2.zero;
        }

        #endregion

        #region Hit Response

        public void OnHit()
        {
            model.isAlerted = true;
            model.alertTimer = model.hitAlertDuration;
            model.isKnockedBack = true;
            model.knockbackTimer = model.knockbackDuration;
            model.isAttackAnimating = false;
            model.pendingAttackTrigger = false;
            model.hasAppliedImpactThisAttack = false;

            if (model.currentState != EnemyState.Chasing && model.currentState != EnemyState.Attacking)
            {
                model.currentState = EnemyState.Chasing;
            }
        }

        #endregion

        #region Guard Behavior

        private void StartGuard()
        {
            int randomBehavior = Random.Range(0, 3);
            model.guardBehavior = (GuardBehavior)randomBehavior;
            model.guardTimer = model.guardDuration;

            if (model.guardBehavior == GuardBehavior.OneCheck)
                model.guardTimer += model.guardLookDuration;
            else if (model.guardBehavior == GuardBehavior.BothCheck)
                model.guardTimer += model.guardLookDuration * 2f;

            model.guardLookTimer = 0f;
            model.isLookingLeft = false;
            model.guardDirection = Random.Range(0, 2) == 0 ? -1 : 1;
        }

        private void UpdateGuardFacing()
        {
            switch (model.guardBehavior)
            {
                case GuardBehavior.NoCheck:
                    break;

                case GuardBehavior.OneCheck:
                    if (model.guardLookTimer < model.guardLookDuration)
                    {
                        model.guardDirection = model.isLookingLeft ? -1 : 1;
                        model.guardLookTimer += Time.deltaTime;
                    }
                    else
                    {
                        model.guardDirection = 1;
                    }
                    break;

                case GuardBehavior.BothCheck:
                    float totalLookTime = model.guardLookDuration * 2f;
                    model.guardLookTimer += Time.deltaTime;

                    if (model.guardLookTimer < model.guardLookDuration)
                    {
                        model.guardDirection = -1;
                        model.isLookingLeft = true;
                    }
                    else if (model.guardLookTimer < totalLookTime)
                    {
                        model.guardDirection = 1;
                        model.isLookingLeft = false;
                    }
                    else
                    {
                        model.guardDirection = 1;
                    }
                    break;
            }

            model.facingDirection = new Vector2(model.guardDirection, 0f);
            ApplyFacingFromDirectionX(model.guardDirection);
        }

        #endregion

        #region Detection

        public bool CanSeePlayer()
        {
            if (model.currentTarget == null)
                return false;

            return CanSeeTarget(model.currentTarget);
        }

        private bool CanSeeTarget(Transform target)
        {
            if (target == null)
                return false;

            if (IsTargetDefeated(target))
                return false;

            Vector2 directionToTarget = (target.position - enemyTransform.position).normalized;
            float distanceToTarget = Vector2.Distance(enemyTransform.position, target.position);

            if (!IsInFieldOfView(directionToTarget))
                return false;

            RaycastHit2D hit = Physics2D.Raycast(
                enemyTransform.position,
                directionToTarget,
                distanceToTarget,
                model.obstacleLayer
            );

            return hit.collider == null;
        }

        private bool IsInFieldOfView(Vector2 directionToPlayer)
        {
            float angle = Vector2.Angle(model.facingDirection, directionToPlayer);
            return angle <= model.fieldOfViewAngle / 2f;
        }

        private static bool IsTargetDefeated(Transform target)
        {
            if (target == null || !PhotonNetwork.IsConnected)
                return false;

            PhotonView pv = target.GetComponent<PhotonView>() ?? target.GetComponentInChildren<PhotonView>(true);
            if (pv?.Owner == null)
                return false;

            if (!pv.Owner.CustomProperties.TryGetValue("isDefeated", out object raw))
                return false;

            return raw is bool isDefeated && isDefeated;
        }

        #endregion

        #region Wandering

        private void GenerateNewWanderTarget()
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomDistance = Random.Range(1f, model.wanderRange);

            model.wanderTarget = model.startPosition + new Vector3(
                Mathf.Cos(randomAngle) * randomDistance,
                Mathf.Sin(randomAngle) * randomDistance,
                0f
            );

            model.currentWanderDirection = (model.wanderTarget - enemyTransform.position).normalized;
        }

        private void MoveWander()
        {
            if (model.rb == null)
                return;

            Vector2 direction = (model.wanderTarget - enemyTransform.position).normalized;
            model.rb.linearVelocity = direction * model.wanderSpeed;

            model.facingDirection = direction;
            ApplyFacingFromDirectionX(direction.x);
        }

        #endregion

        #region Movement

        private void MoveTowardsPlayer()
        {
            if (model.currentTarget == null || model.rb == null)
                return;

            Vector2 direction = (model.currentTarget.position - enemyTransform.position).normalized;
            model.rb.linearVelocity = direction * model.chaseSpeed;

            model.facingDirection = direction;
            ApplyFacingFromDirectionX(direction.x);
        }

        private void ApplyEnemySeparation()
        {
            if (model.rb == null || !model.enableSeparation || model.separationRadius <= 0.01f || model.separationForce <= 0f)
                return;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(enemyTransform.position, model.separationRadius);
            if (overlaps == null || overlaps.Length == 0)
                return;

            Vector2 separation = Vector2.zero;

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider2D col = overlaps[i];
                if (col == null)
                    continue;

                EnemyPresenter otherEnemy = col.GetComponentInParent<EnemyPresenter>();
                if (otherEnemy == null)
                    continue;

                Transform otherTransform = otherEnemy.transform;
                if (otherTransform == enemyTransform)
                    continue;

                Vector2 away = (Vector2)(enemyTransform.position - otherTransform.position);
                float distance = away.magnitude;
                if (distance < 0.001f)
                    continue;

                float weight = 1f - Mathf.Clamp01(distance / model.separationRadius);
                separation += away.normalized * weight;
            }

            if (separation.sqrMagnitude < 0.0001f)
                return;

            model.rb.linearVelocity += separation.normalized * model.separationForce;
        }

        private void UpdateFacingToTarget(Transform target)
        {
            if (target == null)
                return;

            Vector2 direction = (target.position - enemyTransform.position);
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            direction.Normalize();
            model.facingDirection = direction;
            ApplyFacingFromDirectionX(direction.x);
        }

        private void ApplyFacingFromDirectionX(float directionX)
        {
            if (enemyTransform == null || Mathf.Abs(directionX) <= 0.0001f)
                return;

            // Enemy sprites are authored with opposite default orientation, so we invert X sign.
            float sign = directionX > 0f ? -1f : 1f;
            Vector3 scale = enemyTransform.localScale;
            float absX = Mathf.Abs(scale.x);
            if (absX <= 0.0001f)
                absX = 1f;

            scale.x = absX * sign;
            enemyTransform.localScale = scale;

            if (model.spriteRenderer != null)
                model.spriteRenderer.flipX = false;
        }

        #endregion

        #region Animation

        private void UpdateAnimation()
        {
            if (model.animator == null)
                return;

            if (model.currentState == EnemyState.Guard)
            {
                UpdateGuardFacing();
            }

            bool isMoving = model.currentState == EnemyState.Wandering || model.currentState == EnemyState.Chasing;
            model.animator.SetBool("isWalking", isMoving);
        }

        #endregion

        #region Getters

        public EnemyState GetCurrentState() => model.currentState;
        public bool IsAlerted() => model.isAlerted;
        public bool IsKnockedBack() => model.isKnockedBack;
        public bool IsAttackAnimating() => model.isAttackAnimating;
        public int GetAttackSequence() => model.attackSequence;

        #endregion
    }
}