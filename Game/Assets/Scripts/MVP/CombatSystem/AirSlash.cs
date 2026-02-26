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
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileRange = 8f;
    [SerializeField] private float projectileHitRadius = 0.5f;
    [SerializeField] private float attackAnimationDuration = 0.5f;

    [Header("Combat References")]
    [SerializeField] private LayerMask airSlashEnemyLayers;
    [SerializeField] private GameObject airSlashDamagePopupPrefab;

    #endregion

    #region SkillBase Implementation

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

        // Spawn from centerPoint - same origin as indicator
        GameObject projectileGO = Instantiate(projectilePrefab, centerPoint.position, Quaternion.identity);

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

            projectile.Initialize(
                targetDirection,
                projectileSpeed,
                projectileRange,
                damage,
                statsManager.knockbackForce,
                transform,
                airSlashEnemyLayers,
                airSlashDamagePopupPrefab,
                projectileHitRadius
            );
        }
        else
        {
            Debug.LogWarning("AirSlash: AirSlashProjectile component missing on prefab!");
        }
    }

    #endregion
}