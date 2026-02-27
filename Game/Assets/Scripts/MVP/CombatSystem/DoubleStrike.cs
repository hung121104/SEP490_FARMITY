using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Double Strike skill - A two-hit dash attack.
/// Inherits global flow from SkillBase.
/// Each hit has its own charge, roll, confirm and dash.
/// </summary>
public class DoubleStrike : SkillBase
{
    #region Serialized Fields

    [Header("Double Strike Settings")]
    [SerializeField] private float movementDistance = 5f;
    [SerializeField] private int totalHits = 2;
    [SerializeField] private float attackAnimationDuration = 0.6f;

    #endregion

    #region Private Helper Class

    private class DiceRollData
    {
        public int value;

        public DiceRollData(int initialValue)
        {
            value = initialValue;
        }
    }

    #endregion

    #region Unity Lifecycle

    private new void Start()
    {
        base.Start();
        CacheDamagePopupPrefab();
        
        // Ensure enemyLayers is set
        if (enemyLayers == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy");
        }
    }

    #endregion

    #region Initialization

    private void CacheDamagePopupPrefab()
    {
        EnemyCombat enemyCombat = FindObjectOfType<EnemyCombat>();
        if (enemyCombat != null)
        {
            damagePopupPrefab = enemyCombat.DamagePopupPrefab;
        }
    }

    #endregion

    #region SkillBase Implementation

    protected override SkillIndicatorData GetIndicatorData()
        => SkillIndicatorData.Arrow(movementDistance);

    protected override IEnumerator OnExecute(int diceRoll)
    {
        playerHealth?.SetInvulnerable(true);

        HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>();
        DiceRollData rollData = new DiceRollData(diceRoll);

        for (int i = 0; i < totalHits; i++)
        {
            if (i > 0)
                yield return StartCoroutine(PrepareNextHit(rollData));

            hitEnemies.Clear();
            yield return StartCoroutine(DashAndDamage(rollData.value, hitEnemies));
        }

        playerHealth?.SetInvulnerable(false);
    }

    #endregion

    #region Hit Preparation

    private IEnumerator PrepareNextHit(DiceRollData rollData)
    {
        PlayNextHitCharge();
        yield return new WaitForSeconds(chargeDuration);

        int newRoll = RollAndDisplay();
        yield return new WaitForSeconds(rollDisplayDuration);
        rollData.value = newRoll;

        yield return StartCoroutine(WaitForConfirmation());

        if (!IsExecuting)
            yield break;

        PlayNextHitAttack();
        yield return new WaitForSeconds(0.1f);
    }

    private void PlayNextHitCharge()
    {
        if (anim == null) 
            return;

        anim.SetBool("isAttacking", false);
        anim.SetBool("isSkillCharging", true);
    }

    private void PlayNextHitAttack()
    {
        if (anim == null) 
            return;

        anim.SetBool("isSkillCharging", false);
        anim.SetBool("isAttacking", true);
    }

    #endregion

    #region Dash Logic

    private IEnumerator DashAndDamage(int diceRoll, HashSet<Collider2D> hitEnemies)
    {
        Vector3 dashDirection = pointerController?.GetPointerDirection() ?? Vector3.right;
        
        ClearPlayerVelocity();
        yield return StartCoroutine(PerformDash(dashDirection, hitEnemies, diceRoll));
        ClearPlayerVelocity();

        yield return new WaitForSeconds(attackAnimationDuration);
    }

    private void ClearPlayerVelocity()
    {
        if (playerMovement == null)
            return;

        Rigidbody2D rb = playerMovement.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator PerformDash(Vector3 dashDirection, HashSet<Collider2D> hitEnemies, int diceRoll)
    {
        Transform playerTransform = playerMovement.transform;
        Vector3 targetPosition = playerTransform.position + (dashDirection * movementDistance);
        float moveSpeed = movementDistance / 0.3f;

        while (Vector3.Distance(playerTransform.position, targetPosition) > 0.01f)
        {
            playerTransform.position = Vector3.MoveTowards(
                playerTransform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            DamageEnemiesAlongPath(hitEnemies, diceRoll, playerTransform.position);
            yield return null;
        }

        playerTransform.position = targetPosition;
    }

    private void DamageEnemiesAlongPath(HashSet<Collider2D> alreadyHit, int diceRoll, Vector3 currentPos)
    {
        if (statsManager == null || playerMovement == null) 
            return;

        float damageRadius = statsManager.attackRange;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            currentPos,
            damageRadius,
            enemyLayers
        );

        if (enemies.Length == 0) 
            return;

        int damage = DamageCalculator.CalculateSkillDamage(
            diceRoll,
            statsManager.strength,
            skillMultiplier
        );

        foreach (Collider2D enemy in enemies)
        {
            if (alreadyHit.Contains(enemy)) 
                continue;

            alreadyHit.Add(enemy);
            DamageEnemy(enemy, damage);
        }
    }

    #endregion

    #region Enemy Damage

    private void DamageEnemy(Collider2D enemy, int damage)
    {
        ApplyHealthDamage(enemy, damage);
        ApplyKnockback(enemy);
        ShowDamagePopup(enemy.transform.position, damage);
    }

    private void ApplyHealthDamage(Collider2D enemy, int damage)
    {
        EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
        if (enemyHealth != null)
            enemyHealth.ChangeHealth(-damage);
    }

    private void ApplyKnockback(Collider2D enemy)
    {
        EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
        if (enemyKnockback != null && statsManager != null)
            enemyKnockback.Knockback(playerMovement.transform, statsManager.knockbackForce);
    }

    private void ShowDamagePopup(Vector3 position, int damage)
    {
        if (damagePopupPrefab == null) 
            return;

        Vector3 spawnPos = position + Vector3.up * 0.8f;
        GameObject popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        
        TextMeshProUGUI damageText = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (damageText != null)
            damageText.text = damage.ToString();
    }

    #endregion
}