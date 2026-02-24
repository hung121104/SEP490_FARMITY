using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    #region SkillBase Implementation

    protected override SkillIndicatorData GetIndicatorData()
        => SkillIndicatorData.Arrow(movementDistance);

    protected override IEnumerator OnExecute(int diceRoll)
    {
        playerHealth?.SetInvulnerable(true);

        HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>();

        for (int i = 0; i < totalHits; i++)
        {
            if (i > 0)
            {
                PlayNextHitCharge();
                yield return new WaitForSeconds(chargeDuration);

                int newRoll = RollAndDisplay();
                yield return new WaitForSeconds(rollDisplayDuration);
                diceRoll = newRoll;

                yield return StartCoroutine(WaitForConfirmation());

                if (!IsExecuting)
                    yield break;

                PlayNextHitAttack();
                yield return new WaitForSeconds(0.1f);
            }

            hitEnemies.Clear();
            yield return StartCoroutine(DashAndDamage(diceRoll, hitEnemies));
        }

        playerHealth?.SetInvulnerable(false);
    }

    #endregion

    #region Animation Helpers

    private void PlayNextHitCharge()
    {
        if (playerCombat?.anim == null) return;

        playerCombat.anim.SetBool("isAttacking", false);
        playerCombat.anim.SetBool("isSkillCharging", true);
    }

    private void PlayNextHitAttack()
    {
        if (playerCombat?.anim == null) return;

        playerCombat.anim.SetBool("isSkillCharging", false);
        playerCombat.anim.SetBool("isAttacking", true);
    }

    #endregion

    #region Dash Logic

    private IEnumerator DashAndDamage(int diceRoll, HashSet<Collider2D> hitEnemies)
    {
        Vector3 targetPosition = transform.position + (targetDirection * movementDistance);
        float moveSpeed = movementDistance / 0.3f;

        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            DamageEnemiesAlongPath(hitEnemies, diceRoll);

            yield return null;
        }

        transform.position = targetPosition;
        yield return new WaitForSeconds(attackAnimationDuration);
    }

    private void DamageEnemiesAlongPath(HashSet<Collider2D> alreadyHit, int diceRoll)
    {
        if (playerCombat == null || statsManager == null)
            return;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            playerCombat.attackPoint.position,
            statsManager.attackRange,
            playerCombat.enemyLayers
        );

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

    private void DamageEnemy(Collider2D enemy, int damage)
    {
        EnemiesHealth enemyHealth = enemy.GetComponent<EnemiesHealth>();
        if (enemyHealth != null)
            enemyHealth.ChangeHealth(-damage);

        EnemyKnockback enemyKnockback = enemy.GetComponent<EnemyKnockback>();
        if (enemyKnockback != null)
            enemyKnockback.Knockback(transform, statsManager.knockbackForce);

        ShowDamagePopup(enemy.transform.position, damage);
    }

    private void ShowDamagePopup(Vector3 position, int damage)
    {
        if (playerCombat.damagePopupPrefab == null) return;

        Vector3 spawnPos = position + Vector3.up * 0.8f;
        GameObject popup = Instantiate(
            playerCombat.damagePopupPrefab,
            spawnPos,
            Quaternion.identity
        );
        popup.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = damage.ToString();
    }

    #endregion
}