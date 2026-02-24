using UnityEngine;
using System.Collections;

/// <summary>
/// Air Slash skill - A ranged flying slash projectile.
/// Inherits global flow from SkillBase.
/// Only handles: spawning and launching projectile.
/// </summary>
public class AirSlash : SkillBase
{
    #region Serialized Fields

    [Header("Air Slash Settings")]
    [SerializeField] private float projectileRange = 8f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float attackAnimationDuration = 0.5f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    #endregion

    #region SkillBase Implementation

    protected override void OnStart()
    {
        // Use attackPoint as firePoint if not assigned
        if (firePoint == null && playerCombat != null)
            firePoint = playerCombat.attackPoint;
    }

    protected override SkillIndicatorData GetIndicatorData()
        => SkillIndicatorData.Arrow(projectileRange);

    protected override IEnumerator OnExecute(int diceRoll)
    {
        FireProjectile(diceRoll);
        yield return new WaitForSeconds(attackAnimationDuration);
    }

    #endregion

    #region Projectile Logic

    private void FireProjectile(int diceRoll)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("AirSlash: Projectile prefab not assigned!");
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject projectileGO = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Rotate to face direction
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        projectileGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        AirSlashProjectile projectile = projectileGO.GetComponent<AirSlashProjectile>();
        if (projectile != null)
        {
            int damage = DamageCalculator.CalculateSkillDamage(
                diceRoll,
                statsManager.strength,
                skillMultiplier
            );

            // Adjust range for firePoint offset
            float firePointOffset = Vector3.Distance(transform.position, spawnPos);
            float adjustedRange = projectileRange - firePointOffset;

            projectile.Initialize(
                targetDirection,
                projectileSpeed,
                adjustedRange,
                damage,
                statsManager.knockbackForce,
                transform,
                playerCombat.enemyLayers,
                playerCombat.damagePopupPrefab
            );
        }
        else
        {
            Debug.LogWarning("AirSlash: AirSlashProjectile component missing on prefab!");
        }
    }

    #endregion
}