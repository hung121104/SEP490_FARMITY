using UnityEngine;
using System.Collections;

public class PlayerSkill : MonoBehaviour
{
    #region Serialized Fields

    [Header("Double Strike Settings")]
    [SerializeField] private KeyCode skillKey = KeyCode.Alpha1;
    [SerializeField] private float skillCooldown = 3f;
    [SerializeField] private float movementDistance = 2f;

    [Header("Dice")]
    [SerializeField] private DiceTier skillTier = DiceTier.D6;
    [SerializeField] private float skillMultiplier = 1.5f;

    [Header("Charge Animation")]
    [SerializeField] private string chargeAnimationBool = "isCharging";
    [SerializeField] private string attackAnimationBool = "isUsingSkill";

    [Header("Timing")]
    [SerializeField] private float minRollDisplayTime = 0.5f; // Minimum time to show roll before allowing confirm

    #endregion

    #region Private Fields

    // Component References
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private StatsManager statsManager;
    private SkillInputHandler inputHandler;

    // Roll Display
    private RollDisplayController rollDisplayInstance;

    // Skill State
    private float skillTimer = 0f;
    private bool isExecuting = false;

    // Hit Tracking
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
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        inputHandler = GetComponent<SkillInputHandler>();

        if (inputHandler == null)
            inputHandler = gameObject.AddComponent<SkillInputHandler>();

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

    #region Input & Cooldown

    private void UpdateSkillCooldown()
    {
        if (skillTimer > 0)
            skillTimer -= Time.unscaledDeltaTime;
    }

    private void CheckSkillInput()
    {
        if (Input.GetKeyDown(skillKey) && !isExecuting)
        {
            TriggerDoubleStrike();
        }
    }

    #endregion

    #region Skill Execution

    private void TriggerDoubleStrike()
    {
        if (skillTimer > 0)
            return;

        isExecuting = true;
        skillTimer = skillCooldown;
        currentHitNumber = 0;

        DisablePlayerSystems();
        StartCoroutine(ExecuteDoubleStrikeSequence());
    }

    private IEnumerator ExecuteDoubleStrikeSequence()
    {
        EnableInvulnerability(true);

        yield return StartCoroutine(ExecuteSingleHit(1));
        yield return StartCoroutine(ExecuteSingleHit(2));

        EnableInvulnerability(false);
        EnablePlayerSystems();

        isExecuting = false;
    }

    private IEnumerator ExecuteSingleHit(int hitNumber)
    {
        currentHitNumber = hitNumber;
        hasDealtDamageThisHit = false;

        // Roll dice
        currentDiceRoll = DiceRoller.Roll(skillTier);
        Debug.Log($"Hit {hitNumber}: Rolled {currentDiceRoll}");

        // Enter slow motion FIRST
        TimeManager.Instance.EnterSlowMotion();
        
        // Wait one frame for time scale to apply
        yield return null;

        // Play charge animation
        PlayChargeAnimation();

        // Show dice roll
        ShowRollDisplay(currentDiceRoll);

        // Wait minimum time for roll to be visible (using unscaled time)
        yield return new WaitForSecondsRealtime(minRollDisplayTime);

        // Wait for player confirmation
        yield return StartCoroutine(WaitForPlayerConfirmation());

        // Stop charge animation BEFORE checking confirmation
        StopChargeAnimation();

        // If player confirmed, execute attack
        if (inputHandler.IsConfirmed())
        {
            Debug.Log($"Hit {hitNumber}: Confirmed - Attacking");

            // Resume normal time
            TimeManager.Instance.ResumeNormalTime();

            // Wait one frame for time to resume
            yield return null;

            // Play attack animation
            PlayAttackAnimation();

            // Move forward
            MoveForward();

            // Wait for attack animation to complete
            yield return new WaitForSeconds(0.6f);

            // Stop attack animation
            StopAttackAnimation();
        }
        else
        {
            Debug.Log($"Hit {hitNumber}: Cancelled");
            
            // Player cancelled - resume time
            TimeManager.Instance.ResumeNormalTime();
        }

        // Small delay before next hit
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator WaitForPlayerConfirmation()
    {
        inputHandler.StartWaiting();

        // Wait until player makes a decision (using unscaled time for slow motion)
        while (!inputHandler.HasInput())
        {
            yield return null;
        }

        inputHandler.StopWaiting();
    }

    #endregion

    #region Roll Display

    private void ShowRollDisplay(int rollValue)
    {
        EnsureRollDisplay();
        if (rollDisplayInstance == null)
            return;

        rollDisplayInstance.Show();
        float duration = DiceDisplayManager.Instance.GetRollAnimationDuration();
        rollDisplayInstance.PlayRoll(rollValue, skillTier, duration);
    }

    #endregion

    #region Animation Control

    private void PlayChargeAnimation()
    {
        if (playerCombat == null || playerCombat.anim == null)
            return;

        // Reset all other animation states to prevent conflicts
        playerCombat.anim.SetBool("isWalking", false);
        playerCombat.anim.SetBool("isAttacking", false);
        playerCombat.anim.SetBool("isUsingSkill", false);
        
        // Enable charge animation
        playerCombat.anim.SetBool(chargeAnimationBool, true);
    }

    private void StopChargeAnimation()
    {
        if (playerCombat == null || playerCombat.anim == null)
            return;

        playerCombat.anim.SetBool(chargeAnimationBool, false);
    }

    private void PlayAttackAnimation()
    {
        if (playerCombat == null || playerCombat.anim == null)
            return;

        // Make sure charge is off
        playerCombat.anim.SetBool(chargeAnimationBool, false);
        
        // Enable attack animation
        playerCombat.anim.SetBool(attackAnimationBool, true);
    }

    private void StopAttackAnimation()
    {
        if (playerCombat == null || playerCombat.anim == null)
            return;

        playerCombat.anim.SetBool(attackAnimationBool, false);
    }

    #endregion

    #region Movement

    private void MoveForward()
    {
        StartCoroutine(SmoothMoveForward());
    }

    private IEnumerator SmoothMoveForward()
    {
        float direction = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        Vector3 targetPosition = transform.position + new Vector3(direction * movementDistance, 0f, 0f);
        float moveSpeed = movementDistance / 0.3f;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
    }

    #endregion

    #region Damage Handling

    public void OnSkillHit()
    {
        if (playerCombat == null || statsManager == null || hasDealtDamageThisHit)
            return;

        hasDealtDamageThisHit = true;

        int skillDamage = DamageCalculator.CalculateSkillDamage(
            currentDiceRoll,
            statsManager.strength,
            skillMultiplier
        );

        Debug.Log($"Hit {currentHitNumber}: Applying Damage {skillDamage}");

        ApplyDamageToEnemies(skillDamage);
    }

    private void ApplyDamageToEnemies(int damage)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            playerCombat.attackPoint.position,
            statsManager.attackRange,
            playerCombat.enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            DamageEnemy(enemy, damage);
        }
    }

    private void DamageEnemy(Collider2D enemy, int damage)
    {
        EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
        if (enemyHealth == null)
            return;

        enemyHealth.ChangeHealth(-damage);

        EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
        if (enemyKnockback != null)
        {
            enemyKnockback.Knockback(transform, statsManager.knockbackForce);
        }

        ShowDamagePopup(enemy.transform.position, damage);
    }

    private void ShowDamagePopup(Vector3 position, int damage)
    {
        if (playerCombat.damagePopupPrefab == null)
            return;

        Vector3 spawnPos = position + Vector3.up * 0.8f;
        GameObject damagePopup = Instantiate(playerCombat.damagePopupPrefab, spawnPos, Quaternion.identity);
        damagePopup.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = damage.ToString();
    }

    #endregion

    #region Animation Events

    public void OnSkillAnimationEnd()
    {
        StopAttackAnimation();
    }

    #endregion

    #region Player State Management

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

    #endregion
}