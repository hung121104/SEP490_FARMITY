using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float fieldOfViewAngle = 120f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Wandering")]
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float wanderRange = 5f;

    [Header("Guarding")]
    [SerializeField] private float guardDuration = 2f;
    [SerializeField] private float guardLookDuration = 1f;

    [Header("Combat")]
    [SerializeField] private float hitAlertDuration = 5f; // How long to chase after being hit

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Transform player;
    private Rigidbody2D rb;
    private EnemyState currentState = EnemyState.Guard;
    
    // Wandering
    private Vector3 startPosition;
    private Vector3 wanderTarget;
    private Vector2 currentWanderDirection = Vector2.right;
    private Vector2 facingDirection = Vector2.right;

    // Guarding
    private float guardTimer = 0f;
    private GuardBehavior guardBehavior;
    private int guardDirection = 1;
    private float guardLookTimer = 0f;
    private bool isLookingLeft = false;

    // Combat alert
    private bool isAlerted = false;
    private float alertTimer = 0f;

    private enum EnemyState
    {
        Guard,
        Wandering,
        Chasing,
        Attacking
    }

    private enum GuardBehavior
    {
        NoCheck,      // Just stand, no looking
        OneCheck,     // Look one direction
        BothCheck     // Look both directions
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
        
        // Start with guard state
        currentState = EnemyState.Guard;
        StartGuard();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Handle combat alert
        if (isAlerted)
        {
            alertTimer -= Time.deltaTime;
            if (alertTimer <= 0f)
            {
                isAlerted = false;
            }
        }

        // State machine
        switch (currentState)
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

            case EnemyState.Guard:
            case EnemyState.Attacking:
                // Stop moving
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
                break;
        }
    }

    #endregion

    #region State Handlers

    private void HandleGuardState(float distanceToPlayer)
    {
        // If alerted (hit), immediately start chasing
        if (isAlerted)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Detect player in range AND in line of sight
        if (distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Update guard timer
        guardTimer -= Time.deltaTime;

        // Guard duration finished, start wandering
        if (guardTimer <= 0f)
        {
            currentState = EnemyState.Wandering;
            GenerateNewWanderTarget();
        }
    }

    private void HandleWanderingState(float distanceToPlayer)
    {
        // If alerted (hit), immediately start chasing
        if (isAlerted)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Detect player in range AND in line of sight
        if (distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
            return;
        }

        // Check if reached wander target
        if (Vector2.Distance(transform.position, wanderTarget) < 0.3f)
        {
            // Switch to guarding
            currentState = EnemyState.Guard;
            StartGuard();
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

        // Lost player - check if still alerted and within pursuit range
        if (distanceToPlayer > detectionRange + 2f)
        {
            // If no longer alerted, go back to guarding
            if (!isAlerted)
            {
                currentState = EnemyState.Guard;
                StartGuard();
                return;
            }
            else
            {
                // If still alerted but out of range, stop chasing and go back
                // This prevents endless pursuit
                currentState = EnemyState.Guard;
                StartGuard();
                isAlerted = false; // Reset alert so they don't immediately chase again
                return;
            }
        }

        // Can't see player through obstacles but still in range
        if (!CanSeePlayer() && distanceToPlayer > detectionRange)
        {
            currentState = EnemyState.Guard;
            StartGuard();
            isAlerted = false;
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

    #region Hit Detection

    /// <summary>
    /// Called when enemy takes damage - triggers combat alert
    /// </summary>
    public void OnHit()
    {
        isAlerted = true;
        alertTimer = hitAlertDuration;

        // Immediately start chasing if not already
        if (currentState != EnemyState.Chasing && currentState != EnemyState.Attacking)
        {
            currentState = EnemyState.Chasing;
        }
    }

    #endregion

    #region Guarding

    private void StartGuard()
    {
        // Randomly pick guard behavior
        int randomBehavior = Random.Range(0, 3);
        guardBehavior = (GuardBehavior)randomBehavior;

        // Base guard duration
        guardTimer = guardDuration;

        // Add extra time for looking behaviors
        if (guardBehavior == GuardBehavior.OneCheck)
        {
            guardTimer += guardLookDuration;
        }
        else if (guardBehavior == GuardBehavior.BothCheck)
        {
            guardTimer += guardLookDuration * 2f;
        }

        guardLookTimer = 0f;
        isLookingLeft = false;
        guardDirection = Random.Range(0, 2) == 0 ? -1 : 1;
    }

    private void UpdateGuardFacing()
    {
        switch (guardBehavior)
        {
            case GuardBehavior.NoCheck:
                // Just stand, keep current direction
                break;

            case GuardBehavior.OneCheck:
                // Look one direction for guardLookDuration then stand
                if (guardLookTimer < guardLookDuration)
                {
                    guardDirection = isLookingLeft ? -1 : 1;
                    guardLookTimer += Time.deltaTime;
                }
                else
                {
                    // Stop looking, go back to neutral
                    guardDirection = 1;
                }
                break;

            case GuardBehavior.BothCheck:
                // Look left, then right, taking longer
                float totalLookTime = guardLookDuration * 2f;
                guardLookTimer += Time.deltaTime;

                if (guardLookTimer < guardLookDuration)
                {
                    // First half: look left
                    guardDirection = -1;
                    isLookingLeft = true;
                }
                else if (guardLookTimer < totalLookTime)
                {
                    // Second half: look right
                    guardDirection = 1;
                    isLookingLeft = false;
                }
                else
                {
                    // Done looking, return to neutral
                    guardDirection = 1;
                }
                break;
        }

        // Update facing direction
        facingDirection = new Vector2(guardDirection, 0f);

        // Flip sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = guardDirection > 0;
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

        // Update guard facing when in guard state
        if (currentState == EnemyState.Guard)
        {
            UpdateGuardFacing();
        }

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

        // Wander range
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPosition, wanderRange);

            // Draw line to current wander target
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, wanderTarget);

            // Draw field of view cone
            DrawFieldOfViewCone();

            // Show alert status
            if (isAlerted)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
    }

    private void DrawFieldOfViewCone()
    {
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