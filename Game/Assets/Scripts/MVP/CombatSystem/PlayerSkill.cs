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

        statsManager = StatsManager.Instance;
        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();

        EnsureRollDisplay();
    }

    private void EnsureRollDisplay()
    {
        if (rollDisplayInstance != null)
            return;

        // Create RollDisplayController as a new GameObject
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

        currentDiceRoll = DiceRoller.Roll(skillTier);
        ShowRollDisplay(currentDiceRoll);

        yield return StartCoroutine(PlaySkillAnimation());

        MoveForward();

        yield return new WaitForSeconds(1.4f);

        StopSkillAnimation();

        yield return new WaitForSeconds(0.1f);
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

    private System.Collections.IEnumerator PlaySkillAnimation()
    {
        if (playerCombat == null || playerCombat.anim == null)
            yield break;

        playerCombat.anim.SetBool("isWalking", false);
        playerCombat.anim.SetBool("isAttacking", false);

        playerCombat.anim.SetBool("isUsingSkill", false);
        yield return null;

        playerCombat.anim.SetBool("isUsingSkill", true);
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