using UnityEngine;

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

    #endregion

    #region Private Fields

    // Component References
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private SpriteRenderer spriteRenderer;
    private StatsManager statsManager;

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
        
        statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();
    }

    #endregion

    #region Input & Cooldown

    private void UpdateSkillCooldown()
    {
        if (skillTimer > 0)
            skillTimer -= Time.deltaTime;
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

    private System.Collections.IEnumerator ExecuteDoubleStrikeSequence()
    {
        EnableInvulnerability(true);

        // Execute both hits
        yield return StartCoroutine(ExecuteSingleHit(1));
        yield return StartCoroutine(ExecuteSingleHit(2));

        EnableInvulnerability(false);
        EnablePlayerSystems();

        isExecuting = false;
    }

    private System.Collections.IEnumerator ExecuteSingleHit(int hitNumber)
    {
        currentHitNumber = hitNumber;
        hasDealtDamageThisHit = false;

        // Roll dice for this hit
        currentDiceRoll = DiceRoller.Roll(skillTier);
        Debug.Log($"Hit {hitNumber}: Rolled {currentDiceRoll}");

        // Play skill animation
        yield return StartCoroutine(PlaySkillAnimation());

        // Move forward during animation
        MoveForward();

        // Wait for animation to complete
        yield return new WaitForSeconds(1.4f);

        // Stop animation
        StopSkillAnimation();

        // Small delay before next hit
        yield return new WaitForSeconds(0.1f);
    }

    #endregion

    #region Animation Control

    private System.Collections.IEnumerator PlaySkillAnimation()
    {
        if (playerCombat == null || playerCombat.anim == null)
            yield break;

        // Reset other animation states
        playerCombat.anim.SetBool("isWalking", false);
        playerCombat.anim.SetBool("isAttacking", false);

        // Force reset the animation by toggling off then on
        playerCombat.anim.SetBool("isUsingSkill", false);

        // Wait one frame to ensure reset
        yield return null;

        playerCombat.anim.SetBool("isUsingSkill", true);
        Debug.Log($"Triggering skill animation for hit {currentHitNumber}");
    }

    private void StopSkillAnimation()
    {
        if (playerCombat != null && playerCombat.anim != null)
            playerCombat.anim.SetBool("isUsingSkill", false);
    }

    #endregion

    #region Movement

    private void MoveForward()
    {
        StartCoroutine(SmoothMoveForward());
    }

    private System.Collections.IEnumerator SmoothMoveForward()
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

    /// <summary>
    /// Called by Animation Event at slash frame
    /// </summary>
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

        // Apply damage
        enemyHealth.ChangeHealth(-damage);

        // Apply knockback
        EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
        if (enemyKnockback != null)
        {
            enemyKnockback.Knockback(transform, statsManager.knockbackForce);
        }

        // Show damage popup
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

    /// <summary>
    /// Called by Animation Event at end of animation
    /// </summary>
    public void OnSkillAnimationEnd()
    {
        StopSkillAnimation();
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