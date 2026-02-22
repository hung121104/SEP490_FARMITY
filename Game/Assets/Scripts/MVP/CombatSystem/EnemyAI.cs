using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float fieldOfViewAngle = 120f; // Vision cone angle
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

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
    private Vector2 facingDirection = Vector2.right;

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
        // Detect player in range AND in line of sight
        if (distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Start wandering
        currentState = EnemyState.Wandering;
    }

    private void HandleWanderingState(float distanceToPlayer)
    {
        // Detect player in range AND in line of sight
        if (distanceToPlayer <= detectionRange && CanSeePlayer())
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

        // Lost player (out of range or out of sight)
        if (distanceToPlayer > detectionRange + 2f || !CanSeePlayer())
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

    #region Line of Sight

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Check if player is in field of view cone
        if (!IsInFieldOfView(directionToPlayer))
            return false;

        // Raycast to check for obstacles between enemy and player
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer,
            distanceToPlayer,
            obstacleLayer
        );

        // If raycast hit something before reaching player, can't see
        return hit.collider == null;
    }

    private bool IsInFieldOfView(Vector2 directionToPlayer)
    {
        // Calculate angle between facing direction and player direction
        float angle = Vector2.Angle(facingDirection, directionToPlayer);

        // Check if within field of view cone
        return angle <= fieldOfViewAngle / 2f;
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

        // Store facing direction and flip sprite
        facingDirection = direction;
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

        // Store facing direction and flip sprite
        facingDirection = direction;
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

            // Draw field of view cone
            DrawFieldOfViewCone();
        }
    }

    private void DrawFieldOfViewCone()
    {
        // Draw the viewing cone
        float halfFOV = fieldOfViewAngle / 2f * Mathf.Deg2Rad;
        
        Vector2 leftRay = new Vector2(
            Mathf.Cos(Mathf.Atan2(facingDirection.y, facingDirection.x) - halfFOV),
            Mathf.Sin(Mathf.Atan2(facingDirection.y, facingDirection.x) - halfFOV)
        );
        
        Vector2 rightRay = new Vector2(
            Mathf.Cos(Mathf.Atan2(facingDirection.y, facingDirection.x) + halfFOV),
            Mathf.Sin(Mathf.Atan2(facingDirection.y, facingDirection.x) + halfFOV)
        );

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + leftRay * detectionRange);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + rightRay * detectionRange);
    }

    #endregion
}