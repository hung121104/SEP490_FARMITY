using UnityEngine;

/// <summary>
/// Enemy AI with wandering, player detection (FOV + raycast), chasing, and attacking states.
/// </summary>
public class EnemyAI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float fieldOfViewAngle = 120f; // FOV cone in degrees

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Wandering")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderChangeInterval = 3f;
    [SerializeField] private float idleTimeMin = 1f;
    [SerializeField] private float idleTimeMax = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    #endregion

    #region Private Fields

    private Transform player;
    private Rigidbody2D rb;
    private EnemyState currentState = EnemyState.Wandering;

    private Vector3 spawnPoint;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private float idleTimer;
    private bool isIdleWaiting;

    #endregion

    #region Enums

    private enum EnemyState
    {
        Wandering,
        Chasing,
        Attacking
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        UpdateState(distanceToPlayer);
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Wandering:
                HandleWanderMovement();
                break;

            case EnemyState.Chasing:
                MoveTowardsPlayer();
                break;

            case EnemyState.Attacking:
                StopMoving();
                break;
        }
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (playerObj != null)
            player = playerObj.transform;

        // Store spawn point
        spawnPoint = transform.position;
        PickNewWanderTarget();
    }

    #endregion

    #region State Management

    private void UpdateState(float distanceToPlayer)
    {
        switch (currentState)
        {
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
    }

    private void HandleWanderingState(float distanceToPlayer)
    {
        // Detect player
        if (distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Handle idle waiting
        if (isIdleWaiting)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
            {
                isIdleWaiting = false;
                PickNewWanderTarget();
            }
            return;
        }

        // Check if reached wander target
        float distanceToTarget = Vector2.Distance(transform.position, wanderTarget);
        if (distanceToTarget < 0.5f)
        {
            isIdleWaiting = true;
            idleTimer = Random.Range(idleTimeMin, idleTimeMax);
            return;
        }

        // Change wander target periodically
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0)
        {
            PickNewWanderTarget();
        }
    }

    private void HandleChasingState(float distanceToPlayer)
    {
        // Enter attack range
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        // Lost player - return to wandering
        if (distanceToPlayer > detectionRange + 2f || !CanSeePlayer())
        {
            currentState = EnemyState.Wandering;
            PickNewWanderTarget();
        }
    }

    private void HandleAttackingState(float distanceToPlayer)
    {
        // Player moved out of range
        if (distanceToPlayer > attackRange + 0.5f)
        {
            currentState = EnemyState.Chasing;
        }
    }

    #endregion

    #region Wandering

    private void PickNewWanderTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(0f, wanderRadius);
        wanderTarget = (Vector2)spawnPoint + (randomDirection * randomDistance);
        wanderTimer = wanderChangeInterval;
    }

    private void HandleWanderMovement()
    {
        if (isIdleWaiting)
        {
            StopMoving();
            return;
        }

        Vector2 direction = ((Vector3)wanderTarget - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
        FaceDirection(direction);
    }

    #endregion

    #region Movement

    private void MoveTowardsPlayer()
    {
        if (player == null || rb == null)
            return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;
        FaceDirection(direction);
    }

    private void StopMoving()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
    }

    private void FaceDirection(Vector2 direction)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0;
    }

    #endregion

    #region Vision/Sight

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 directionToPlayer = (player.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;

        // Check distance
        if (distanceToPlayer > detectionRange)
            return false;

        // Check FOV using dot product
        Vector3 enemyLookDirection = spriteRenderer.flipX ? Vector3.left : Vector3.right;
        float dotProduct = Vector3.Dot(enemyLookDirection, directionToPlayer.normalized);

        // dotProduct > 0 means in front, higher value = more centered
        // 0.5 ≈ 60 degree cone (half FOV)
        float fovThreshold = Mathf.Cos(fieldOfViewAngle / 2f * Mathf.Deg2Rad);
        
        if (dotProduct < fovThreshold)
            return false;

        // Raycast for line of sight
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer.normalized,
            distanceToPlayer
        );

        // Debug visualization
        Debug.DrawRay(transform.position, directionToPlayer.normalized * distanceToPlayer, Color.green);

        // Check if hit player
        if (hit.collider != null && hit.collider.CompareTag("PlayerEntity"))
            return true;

        return false;
    }

    #endregion

    #region Animation

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool isMoving = (currentState == EnemyState.Chasing) || 
                       (currentState == EnemyState.Wandering && !isIdleWaiting);
        
        animator.SetBool("isWalking", isMoving);
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        // Wander radius
        Gizmos.color = Color.blue;
        Vector3 center = Application.isPlaying ? spawnPoint : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // FOV cone visualization
        if (Application.isPlaying && spriteRenderer != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 lookDirection = spriteRenderer.flipX ? Vector3.left : Vector3.right;
            float halfFOV = fieldOfViewAngle / 2f;

            Vector3 leftEdge = Quaternion.AngleAxis(halfFOV, Vector3.forward) * lookDirection * detectionRange;
            Vector3 rightEdge = Quaternion.AngleAxis(-halfFOV, Vector3.forward) * lookDirection * detectionRange;

            Gizmos.DrawLine(transform.position, transform.position + leftEdge);
            Gizmos.DrawLine(transform.position, transform.position + rightEdge);
        }
    }

    #endregion
}