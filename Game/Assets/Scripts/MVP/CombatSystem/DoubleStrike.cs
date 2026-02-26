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

    #region Private Fields

    private EnemyCombat enemyCombat;

    #endregion

    #region Unity Lifecycle

    private new void Start()
    {
        base.Start();
        enemyCombat = FindObjectOfType<EnemyCombat>();
        
        // Ensure enemyLayers is set
        if (enemyLayers == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy");
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
        if (anim == null) return;

        anim.SetBool("isAttacking", false);
        anim.SetBool("isSkillCharging", true);
    }

    private void PlayNextHitAttack()
    {
        if (anim == null) return;

        anim.SetBool("isSkillCharging", false);
        anim.SetBool("isAttacking", true);
    }

    #endregion

    #region Dash Logic

    private IEnumerator DashAndDamage(int diceRoll, HashSet<Collider2D> hitEnemies)
    {
        // Get dash direction from SkillBase's pointerController
        Vector3 dashDirection = pointerController?.GetPointerDirection() ?? Vector3.right;
        
        // Clear player velocity before dashing
        if (playerMovement != null)
        {
            Rigidbody2D rb = playerMovement.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        // Move the PLAYER in pointer direction
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

            // Check for enemies ALONG the entire dash path
            DamageEnemiesAlongPath(hitEnemies, diceRoll, playerTransform.position);

            yield return null;
        }

        playerTransform.position = targetPosition;

        // Clear velocity again after dash completes
        if (playerMovement != null)
        {
            Rigidbody2D rb = playerMovement.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(attackAnimationDuration);
    }

    private void DamageEnemiesAlongPath(HashSet<Collider2D> alreadyHit, int diceRoll, Vector3 currentPos)
    {
        if (statsManager == null || playerMovement == null) return;

        float damageRadius = statsManager.attackRange;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            currentPos,
            damageRadius,
            enemyLayers
        );

        if (enemies.Length == 0) return;

        int damage = DamageCalculator.CalculateSkillDamage(
            diceRoll,
            statsManager.strength,
            skillMultiplier
        );

        foreach (Collider2D enemy in enemies)
        {
            if (alreadyHit.Contains(enemy)) continue;

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
            enemyKnockback.Knockback(playerMovement.transform, statsManager.knockbackForce);

        ShowDamagePopup(enemy.transform.position, damage);
    }

    private void ShowDamagePopup(Vector3 position, int damage)
    {
        GameObject popupPrefab = enemyCombat?.damagePopupPrefab;
        if (popupPrefab == null) return;

        Vector3 spawnPos = position + Vector3.up * 0.8f;
        GameObject popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity);
        
        TextMeshProUGUI damageText = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (damageText != null)
            damageText.text = damage.ToString();
    }

    #endregion
}