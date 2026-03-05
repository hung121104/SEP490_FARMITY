using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for enemy AI state machine, movement, and behavior.
    /// Integrated with physics (merged from EnemyMovement).
    /// </summary>
    public class EnemyAIService : IEnemyAIService
    {
        private EnemyModel model;
        private Transform enemyTransform;

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

        #endregion

        #region Behavior Update

        public void UpdateBehavior(float deltaTime, float distanceToPlayer)
        {
            if (model.playerTransform == null)
                return;

            // State machine
            switch (model.currentState)
            {
                case EnemyState.Guard:
                    HandleGuardState(distanceToPlayer);
                    break;

                case EnemyState.Wandering:
                    HandleWanderingState(distanceToPlayer);
                    break;

                case EnemyState.Chasing:
                    HandleChasingState(distanceToPlayer);
                    break;

                case EnemyState.Attacking:
                    HandleAttackingState(distanceToPlayer);
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
            if (distanceToPlayer <= model.attackRange)
            {
                model.currentState = EnemyState.Attacking;
                return;
            }

            if (distanceToPlayer > model.detectionRange + 2f)
            {
                model.currentState = EnemyState.Guard;
                StartGuard();
                model.isAlerted = false;
                return;
            }

            if (!CanSeePlayer() && distanceToPlayer > model.detectionRange)
            {
                model.currentState = EnemyState.Guard;
                StartGuard();
                model.isAlerted = false;
            }
        }

        private void HandleAttackingState(float distanceToPlayer)
        {
            if (distanceToPlayer > model.attackRange + 0.5f)
            {
                model.currentState = EnemyState.Chasing;
            }
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

            if (model.currentState != EnemyState.Chasing && model.currentState != EnemyState.Attacking)
            {
                model.currentState = EnemyState.Chasing;
            }

            Debug.Log("[EnemyAIService] Enemy hit! Now alerted and chasing.");
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

            if (model.spriteRenderer != null)
            {
                model.spriteRenderer.flipX = model.guardDirection > 0;
            }
        }

        #endregion

        #region Detection

        public bool CanSeePlayer()
        {
            if (model.playerTransform == null)
                return false;

            Vector2 directionToPlayer = (model.playerTransform.position - enemyTransform.position).normalized;
            float distanceToPlayer = Vector2.Distance(enemyTransform.position, model.playerTransform.position);

            if (!IsInFieldOfView(directionToPlayer))
                return false;

            RaycastHit2D hit = Physics2D.Raycast(
                enemyTransform.position,
                directionToPlayer,
                distanceToPlayer,
                model.obstacleLayer
            );

            return hit.collider == null;
        }

        private bool IsInFieldOfView(Vector2 directionToPlayer)
        {
            float angle = Vector2.Angle(model.facingDirection, directionToPlayer);
            return angle <= model.fieldOfViewAngle / 2f;
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
            if (model.spriteRenderer != null)
            {
                model.spriteRenderer.flipX = direction.x > 0;
            }
        }

        #endregion

        #region Movement

        private void MoveTowardsPlayer()
        {
            if (model.playerTransform == null || model.rb == null)
                return;

            Vector2 direction = (model.playerTransform.position - enemyTransform.position).normalized;
            model.rb.linearVelocity = direction * model.chaseSpeed;

            model.facingDirection = direction;
            if (model.spriteRenderer != null)
            {
                model.spriteRenderer.flipX = direction.x > 0;
            }
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

        #endregion
    }
}