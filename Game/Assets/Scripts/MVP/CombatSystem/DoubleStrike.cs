using UnityEngine;

/// <summary>
/// Double Strike skill - A two-hit attack with time slowdown, dice roll display and mouse aiming.
/// Press skill key → slow motion → roll dice → aim with mouse → confirm/cancel → attack → repeat for second hit
/// </summary>
public class DoubleStrike : MonoBehaviour
{
    #region Enums

    public enum SkillState
    {
        Idle,
        Charging,
        WaitingConfirm,
        Attacking
    }

    #endregion

    #region Serialized Fields

    [Header("Input")]
    [SerializeField] private KeyCode skillKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode confirmKey = KeyCode.E;
    [SerializeField] private KeyCode cancelKey = KeyCode.Q;

    [Header("Skill Settings")]
    [SerializeField] private float skillCooldown = 3f;
    [SerializeField] private int totalHits = 2;

    [Header("Movement")]
    [SerializeField] private float movementDistance = 10f;

    [Header("Dice")]
    [SerializeField] private DiceTier skillTier = DiceTier.D6;
    [SerializeField] private float skillMultiplier = 1.5f;

    [Header("Timing")]
    [SerializeField] private float rollDisplayDuration = 0.4f;
    [SerializeField] private float attackAnimationDuration = 0.6f;

    #endregion

    #region Private Fields - Components

    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private StatsManager statsManager;
    private RollDisplayController rollDisplayInstance;

    #endregion

    #region Private Fields - State

    private SkillState currentState = SkillState.Idle;
    private bool isExecuting = false;
    private float skillTimer = 0f;
    private Vector3 targetDirection = Vector3.right;
    private float currentMovementDistance = 2f;

    #endregion

    #region Private Fields - Hit Tracking

    private int currentHitNumber = 0;
    private int currentDiceRoll = 0;
    private bool hasDealtDamageThisHit = false;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
    }

    private void Update()
    {
        UpdateSkillCooldown();
        CheckSkillInput();
        HandleStateInput();
        UpdateAiming();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();

        EnsureRollDisplay();
    }

    private void EnsureRollDisplay()
    {
        if (rollDisplayInstance != null)
            return;

        GameObject rollDisplayGO = new GameObject("RollDisplay");
        rollDisplayGO.transform.SetParent(transform);
        rollDisplayGO.transform.localPosition = Vector3.zero;

        rollDisplayInstance = rollDisplayGO.AddComponent<RollDisplayController>();
        rollDisplayInstance.AttachTo(transform, DiceDisplayManager.Instance.GetRollDisplayOffset());
    }

    #endregion

    #region Input Handling

    private void CheckSkillInput()
    {
        if (Input.GetKeyDown(skillKey) && CanTriggerSkill())
        {
            TriggerDoubleStrike();
        }
    }

    private void HandleStateInput()
    {
        if (currentState != SkillState.WaitingConfirm)
            return;

        if (Input.GetKeyDown(confirmKey))
        {
            ConfirmAttack();
        }
        else if (Input.GetKeyDown(cancelKey))
        {
            CancelSkill();
        }
    }

    #endregion

    #region Aiming System

    private void UpdateAiming()
    {
        if (currentState != SkillState.WaitingConfirm)
            return;

        // Just track mouse direction, no ShowIndicator here!
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        Vector3 direction = mousePos - transform.position;
        direction.z = 0f;

        if (direction.magnitude > 0.01f)
            targetDirection = direction.normalized;

        currentMovementDistance = movementDistance;

        if (spriteRenderer != null)
            spriteRenderer.flipX = targetDirection.x < 0;
    }

    #endregion

    #region Cooldown Management

    private void UpdateSkillCooldown()
    {
        if (skillTimer > 0)
            skillTimer -= Time.deltaTime;
    }

    private bool CanTriggerSkill()
    {
        return !isExecuting && currentState == SkillState.Idle && skillTimer <= 0;
    }

    #endregion

    #region Skill Execution Flow

    private void TriggerDoubleStrike()
    {
        isExecuting = true;
        skillTimer = skillCooldown;
        currentHitNumber = 0;

        DisablePlayerSystems();
        StartCoroutine(ExecuteDoubleStrikeSequence());
    }

    private System.Collections.IEnumerator ExecuteDoubleStrikeSequence()
    {
        EnableInvulnerability(true);

        for (int i = 1; i <= totalHits; i++)
        {
            yield return StartCoroutine(ExecuteSingleHit(i));

            if (!isExecuting)
                break;
        }

        EndSkillExecution();
    }

    private System.Collections.IEnumerator ExecuteSingleHit(int hitNumber)
    {
        currentHitNumber = hitNumber;
        hasDealtDamageThisHit = false;

        // === CHARGE PHASE ===
        TimeManager.Instance.SetSlowMotion();
        PlayChargeAnimation();
        yield return new WaitForSecondsRealtime(0.2f);

        // === ROLL PHASE ===
        currentDiceRoll = DiceRoller.Roll(skillTier);
        ShowRollDisplay(currentDiceRoll);
        yield return new WaitForSecondsRealtime(rollDisplayDuration);

        // === WAIT FOR CONFIRMATION ===
        currentState = SkillState.WaitingConfirm;

        // Show indicator ONCE here
        SkillIndicatorManager.Instance?.ShowIndicator(
            SkillIndicatorData.Arrow(movementDistance)
        );

        while (currentState == SkillState.WaitingConfirm && isExecuting)
        {
            yield return null;
        }

        // Hide indicator
        SkillIndicatorManager.Instance?.HideAll();

        // Check if cancelled
        if (!isExecuting)
        {
            TimeManager.Instance.SetNormalSpeed();
            yield break;
        }

        // === ATTACK PHASE ===
        TimeManager.Instance.SetNormalSpeed();
        currentState = SkillState.Attacking;

        PlayAttackAnimation();
        yield return new WaitForSeconds(0.1f);

        MoveForward();

        yield return new WaitForSeconds(attackAnimationDuration);
    }

    private void ConfirmAttack()
    {
        if (currentState == SkillState.WaitingConfirm)
        {
            currentState = SkillState.Attacking;
        }
    }

    private void CancelSkill()
    {
        if (!isExecuting) return;

        isExecuting = false;
        currentState = SkillState.Idle;

        TimeManager.Instance.SetNormalSpeed();
        StopSkillAnimation();
        SkillIndicatorManager.Instance?.HideAll();
        EnablePlayerSystems();
    }

    private void EndSkillExecution()
    {
        EnableInvulnerability(false);
        EnablePlayerSystems();
        StopSkillAnimation();
        SkillIndicatorManager.Instance?.HideAll();

        isExecuting = false;
        currentState = SkillState.Idle;
        TimeManager.Instance.SetNormalSpeed();
    }

    #endregion

    #region Roll Display UI

    private void ShowRollDisplay(int rollValue)
    {
        EnsureRollDisplay();

        if (rollDisplayInstance == null)
            return;

        rollDisplayInstance.Show();
        rollDisplayInstance.PlayRoll(rollValue, skillTier, rollDisplayDuration);
    }

    #endregion

    #region Animation Control

    private void PlayChargeAnimation()
    {
        if (playerCombat?.anim == null)
            return;

        playerCombat.anim.SetBool("isWalking", false);
        playerCombat.anim.SetBool("isAttacking", false);
        playerCombat.anim.SetBool("isSkillCharging", true);
        playerCombat.anim.SetBool("isSkillAttacking", false);
    }

    private void PlayAttackAnimation()
    {
        if (playerCombat?.anim == null)
            return;

        playerCombat.anim.SetBool("isSkillCharging", false);
        playerCombat.anim.SetBool("isSkillAttacking", true);
    }

    private void StopSkillAnimation()
    {
        if (playerCombat?.anim == null)
            return;

        playerCombat.anim.SetBool("isSkillCharging", false);
        playerCombat.anim.SetBool("isSkillAttacking", false);
    }

    #endregion

    #region Movement

    private void MoveForward()
    {
        StartCoroutine(SmoothMoveForward());
    }

    private System.Collections.IEnumerator SmoothMoveForward()
    {
        Vector3 targetPosition = transform.position + (targetDirection * currentMovementDistance);
        float moveSpeed = currentMovementDistance / 0.3f;

        System.Collections.Generic.HashSet<Collider2D> hitEnemies =
            new System.Collections.Generic.HashSet<Collider2D>();

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            DamageEnemiesAlongPath(hitEnemies);

            yield return null;
        }

        transform.position = targetPosition;
    }

    private void DamageEnemiesAlongPath(System.Collections.Generic.HashSet<Collider2D> alreadyHit)
    {
        if (playerCombat == null || statsManager == null)
            return;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            playerCombat.attackPoint.position,
            statsManager.attackRange,
            playerCombat.enemyLayers
        );

        int skillDamage = DamageCalculator.CalculateSkillDamage(
            currentDiceRoll,
            statsManager.strength,
            skillMultiplier
        );

        foreach (Collider2D enemy in enemies)
        {
            if (alreadyHit.Contains(enemy))
                continue;

            alreadyHit.Add(enemy);
            DamageEnemy(enemy, skillDamage);
        }
    }

    #endregion

    #region Damage System

    private void DamageEnemy(Collider2D enemy, int damage)
    {
        EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
        if (enemyHealth == null)
            return;

        enemyHealth.ChangeHealth(-damage);

        EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
        if (enemyKnockback != null)
            enemyKnockback.Knockback(transform, statsManager.knockbackForce);

        ShowDamagePopup(enemy.transform.position, damage);
    }

    private void ShowDamagePopup(Vector3 position, int damage)
    {
        if (playerCombat.damagePopupPrefab == null)
            return;

        Vector3 spawnPos = position + Vector3.up * 0.8f;
        GameObject damagePopup = Instantiate(
            playerCombat.damagePopupPrefab,
            spawnPos,
            Quaternion.identity
        );

        damagePopup.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = damage.ToString();
    }

    #endregion

    #region Player System Management

    private void DisablePlayerSystems()
    {
        if (playerCombat != null)
            playerCombat.enabled = false;

        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    private void EnablePlayerSystems()
    {
        if (playerCombat != null)
            playerCombat.enabled = true;

        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    private void EnableInvulnerability(bool enable)
    {
        if (playerHealth != null)
            playerHealth.SetInvulnerable(enable);
    }

    #endregion

    #region Public API

    public float GetSkillCooldownPercent() => Mathf.Clamp01(1f - (skillTimer / skillCooldown));
    public bool IsExecuting => isExecuting;
    public SkillState GetCurrentState => currentState;

    #endregion
}