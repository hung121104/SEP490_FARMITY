using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Wandering")]
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float wanderChangeInterval = 3f;
    [SerializeField] private float wanderRange = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform player;
    private Rigidbody2D rb;
    private EnemyState currentState = EnemyState.Idle;
    
    // Wandering
    private Vector3 startPosition;
    private Vector3 wanderTarget;
    private float wanderTimer = 0f;
    private Vector2 currentWanderDirection = Vector2.right;

    private enum EnemyState
    {
        Idle,
        Wandering,
        Chasing,
        Attacking
    }

    #region Unity Lifecycle

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Find player with PlayerEntity tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (playerObj != null)
            player = playerObj.transform;

        // Setup wandering
        startPosition = transform.position;
        GenerateNewWanderTarget();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // State machine
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState(distanceToPlayer);
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

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Wandering:
                MoveWander();
                break;

            case EnemyState.Chasing:
                MoveTowardsPlayer();
                break;

            case EnemyState.Idle:
            case EnemyState.Attacking:
                // Stop moving
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
                break;
        }
    }

    #endregion

    #region State Handlers

    private void HandleIdleState(float distanceToPlayer)
    {
        // Detect player in range
        if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Start wandering
        currentState = EnemyState.Wandering;
    }

    private void HandleWanderingState(float distanceToPlayer)
    {
        // Detect player in range
        if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Check if reached wander target
        if (Vector2.Distance(transform.position, wanderTarget) < 0.3f)
        {
            GenerateNewWanderTarget();
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

        // Lost player
        if (distanceToPlayer > detectionRange + 2f)
        {
            currentState = EnemyState.Wandering;
            GenerateNewWanderTarget();
        }
    }

    private void HandleAttackingState(float distanceToPlayer)
    {
        // Player moved out of attack range
        if (distanceToPlayer > attackRange + 0.5f)
        {
            currentState = EnemyState.Chasing;
        }
    }

    #endregion

    #region Wandering

    private void GenerateNewWanderTarget()
    {
        // Random angle
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Random distance within wander range
        float randomDistance = Random.Range(1f, wanderRange);
        
        // Calculate new target relative to start position
        wanderTarget = startPosition + new Vector3(
            Mathf.Cos(randomAngle) * randomDistance,
            Mathf.Sin(randomAngle) * randomDistance,
            0f
        );

        // Store direction for animation
        currentWanderDirection = (wanderTarget - transform.position).normalized;
    }

    private void MoveWander()
    {
        if (rb == null)
            return;

        Vector2 direction = (wanderTarget - transform.position).normalized;
        rb.linearVelocity = direction * wanderSpeed;

        // Flip sprite based on wander direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x > 0;
        }
    }

    #endregion

    #region Movement

    private void MoveTowardsPlayer()
    {
        if (player == null || rb == null)
            return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;

        // Flip sprite based on movement direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x > 0;
        }
    }

    #endregion

    #region Animation

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        bool isMoving = currentState == EnemyState.Wandering || currentState == EnemyState.Chasing;
        animator.SetBool("isWalking", isMoving);
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Wander range (circle around start position)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPosition, wanderRange);

            // Draw line to current wander target
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, wanderTarget);
        }
    }

    #endregion
}