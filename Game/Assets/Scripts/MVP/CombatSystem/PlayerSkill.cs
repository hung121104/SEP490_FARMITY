using UnityEngine;

public class PlayerSkill : MonoBehaviour
{
    [Header("Double Strike Settings")]
    [SerializeField] private KeyCode skillKey = KeyCode.Alpha1; // "1" key
    [SerializeField] private float skillCooldown = 3f;
    [SerializeField] private float movementDistance = 2f; // Units to move forward per attack

    [Header("Dice")]
    [SerializeField] private DiceTier skillTier = DiceTier.D6;
    [SerializeField] private float skillMultiplier = 1.5f;

    private PlayerCombat playerCombat;
    private SpriteRenderer spriteRenderer;
    private StatsManager statsManager;

    private float skillTimer = 0f;
    private bool isExecuting = false;

    private void Start()
    {
        playerCombat = GetComponent<PlayerCombat>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        statsManager = StatsManager.Instance;

        if (statsManager == null)
            statsManager = FindObjectOfType<StatsManager>();
    }

    private void Update()
    {
        if (skillTimer > 0)
            skillTimer -= Time.deltaTime;

        if (Input.GetKeyDown(skillKey) && !isExecuting)
        {
            TriggerDoubleStrike();
        }
    }

    private void TriggerDoubleStrike()
    {
        if (skillTimer > 0)
            return; // Still on cooldown

        isExecuting = true;
        skillTimer = skillCooldown;

        // Start coroutine to execute both attacks with movement
        StartCoroutine(ExecuteDoubleStrikeSequence());
    }

    private System.Collections.IEnumerator ExecuteDoubleStrikeSequence()
    {
        if (playerCombat != null)
            playerCombat.enabled = false;

        // First attack (Hit 1)
        ExecuteSkillAttack(1);
        MoveForward();

        yield return new WaitForSeconds(0.5f);

        // Second attack (Hit 2)
        ExecuteSkillAttack(2);
        MoveForward();

        yield return new WaitForSeconds(0.5f);

        if (playerCombat != null)
            playerCombat.enabled = true;

        isExecuting = false;
    }

    private void ExecuteSkillAttack(int hitNumber)
    {
        if (playerCombat == null || statsManager == null)
            return;

        int diceRoll = DiceRoller.Roll(skillTier);

        int skillDamage = DamageCalculator.CalculateSkillDamage(diceRoll, statsManager.strength, skillMultiplier);

        Debug.Log($"Hit {hitNumber}: Rolled {diceRoll}, Damage: {skillDamage}");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            playerCombat.attackPoint.position,
            statsManager.attackRange,
            playerCombat.enemyLayers
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ChangeHealth(-skillDamage);

                EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
                if (enemyKnockback != null)
                {
                    enemyKnockback.Knockback(transform, statsManager.knockbackForce);
                }

                if (playerCombat.damagePopupPrefab != null)
                {
                    Vector3 spawnPos = enemy.transform.position + Vector3.up * 0.8f;
                    GameObject damagePopup = Instantiate(playerCombat.damagePopupPrefab, spawnPos, Quaternion.identity);
                    damagePopup.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = skillDamage.ToString();
                }
            }
        }
    }

    private void MoveForward()
    {
        StartCoroutine(SmoothMoveForward());
    }

    private System.Collections.IEnumerator SmoothMoveForward()
    {
        float direction = spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        Vector3 targetPosition = transform.position + new Vector3(direction * movementDistance, 0f, 0f);
        float moveSpeed = movementDistance / 0.3f; // Complete movement in 0.3 seconds

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
    }

    public float GetSkillCooldownPercent() => Mathf.Clamp01(1f - (skillTimer / skillCooldown));
}