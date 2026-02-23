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
    [SerializeField] private float hitAlertDuration = 5f;
    [SerializeField] private float knockbackDuration = 0.3f;

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

    // Knockback
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    private enum EnemyState
    {
        Guard,
        Wandering,
        Chasing,
        Attacking
    }

    private enum GuardBehavior
    {
        NoCheck,
        OneCheck,
        BothCheck
    }

    #region Unity Lifecycle

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (playerObj != null)
            player = playerObj.transform;

        startPosition = transform.position;
        GenerateNewWanderTarget();
        
        currentState = EnemyState.Guard;
        StartGuard();
    }

    private void Update()
    {
        if (player == null)
            return;

        // Handle knockback timer
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
            return; // Skip all AI logic while knocked back
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Handle combat alert timer
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
        // Don't override velocity while knocked back
        if (isKnockedBack)
            return;

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
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
                break;
        }
    }

    #endregion

    #region State Handlers

    private void HandleGuardState(float distanceToPlayer)
    {
        if (isAlerted)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        if (distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
            return;
        }

        guardTimer -= Time.deltaTime;

        if (guardTimer <= 0f)
        {
            currentState = EnemyState.Wandering;
            GenerateNewWanderTarget();
        }
    }

    private void HandleWanderingState(float distanceToPlayer)
    {
        if (isAlerted)
        {
            currentState = EnemyState.Chasing;
            return;
        }

        if (distanceToPlayer <= detectionRange && CanSeePlayer())
        {
            currentState = EnemyState.Chasing;
            return;
        }

        if (Vector2.Distance(transform.position, wanderTarget) < 0.3f)
        {
            currentState = EnemyState.Guard;
            StartGuard();
        }
    }

    private void HandleChasingState(float distanceToPlayer)
    {
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
            return;
        }

        if (distanceToPlayer > detectionRange + 2f)
        {
            currentState = EnemyState.Guard;
            StartGuard();
            isAlerted = false;
            return;
        }

        if (!CanSeePlayer() && distanceToPlayer > detectionRange)
        {
            currentState = EnemyState.Guard;
            StartGuard();
            isAlerted = false;
        }
    }

    private void HandleAttackingState(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange + 0.5f)
        {
            currentState = EnemyState.Chasing;
        }
    }

    #endregion

    #region Hit & Knockback

    /// <summary>
    /// Called when enemy takes damage - triggers combat alert
    /// </summary>
    public void OnHit()
    {
        // Trigger alert
        isAlerted = true;
        alertTimer = hitAlertDuration;

        // Trigger knockback pause
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        // Immediately start chasing after knockback
        if (currentState != EnemyState.Chasing && currentState != EnemyState.Attacking)
        {
            currentState = EnemyState.Chasing;
        }
    }

    #endregion

    #region Guarding

    private void StartGuard()
    {
        int randomBehavior = Random.Range(0, 3);
        guardBehavior = (GuardBehavior)randomBehavior;

        guardTimer = guardDuration;

        if (guardBehavior == GuardBehavior.OneCheck)
            guardTimer += guardLookDuration;
        else if (guardBehavior == GuardBehavior.BothCheck)
            guardTimer += guardLookDuration * 2f;

        guardLookTimer = 0f;
        isLookingLeft = false;
        guardDirection = Random.Range(0, 2) == 0 ? -1 : 1;
    }

    private void UpdateGuardFacing()
    {
        switch (guardBehavior)
        {
            case GuardBehavior.NoCheck:
                break;

            case GuardBehavior.OneCheck:
                if (guardLookTimer < guardLookDuration)
                {
                    guardDirection = isLookingLeft ? -1 : 1;
                    guardLookTimer += Time.deltaTime;
                }
                else
                {
                    guardDirection = 1;
                }
                break;

            case GuardBehavior.BothCheck:
                float totalLookTime = guardLookDuration * 2f;
                guardLookTimer += Time.deltaTime;

                if (guardLookTimer < guardLookDuration)
                {
                    guardDirection = -1;
                    isLookingLeft = true;
                }
                else if (guardLookTimer < totalLookTime)
                {
                    guardDirection = 1;
                    isLookingLeft = false;
                }
                else
                {
                    guardDirection = 1;
                }
                break;
        }

        facingDirection = new Vector2(guardDirection, 0f);

        if (spriteRenderer != null)
            spriteRenderer.flipX = guardDirection > 0;
    }

    #endregion

    #region Line of Sight

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (!IsInFieldOfView(directionToPlayer))
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer,
            distanceToPlayer,
            obstacleLayer
        );

        return hit.collider == null;
    }

    private bool IsInFieldOfView(Vector2 directionToPlayer)
    {
        float angle = Vector2.Angle(facingDirection, directionToPlayer);
        return angle <= fieldOfViewAngle / 2f;
    }

    #endregion

    #region Wandering

    private void GenerateNewWanderTarget()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float randomDistance = Random.Range(1f, wanderRange);
        
        wanderTarget = startPosition + new Vector3(
            Mathf.Cos(randomAngle) * randomDistance,
            Mathf.Sin(randomAngle) * randomDistance,
            0f
        );

        currentWanderDirection = (wanderTarget - transform.position).normalized;
    }

    private void MoveWander()
    {
        if (rb == null)
            return;

        Vector2 direction = (wanderTarget - transform.position).normalized;
        rb.linearVelocity = direction * wanderSpeed;

        facingDirection = direction;
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x > 0;
    }

    #endregion

    #region Movement

    private void MoveTowardsPlayer()
    {
        if (player == null || rb == null)
            return;

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = direction * chaseSpeed;

        facingDirection = direction;
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction.x > 0;
    }

    #endregion

    #region Animation

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        if (currentState == EnemyState.Guard)
            UpdateGuardFacing();

        bool isMoving = currentState == EnemyState.Wandering || currentState == EnemyState.Chasing;
        animator.SetBool("isWalking", isMoving);
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPosition, wanderRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, wanderTarget);

            DrawFieldOfViewCone();

            // Red dot = alerted, Orange dot = knocked back
            if (isAlerted)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }

            if (isKnockedBack)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
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