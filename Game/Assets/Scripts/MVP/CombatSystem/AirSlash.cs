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
        if (!ValidateProjectileSetup())
            return;

        Vector3 fireDirection = pointerController?.GetPointerDirection() ?? Vector3.right;
        GameObject projectileGO = Instantiate(projectilePrefab, playerMovement.transform.position, Quaternion.identity);

        RotateProjectile(projectileGO, fireDirection);
        InitializeProjectile(projectileGO, fireDirection, diceRoll);
    }

    private bool ValidateProjectileSetup()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[AirSlash] Projectile prefab not assigned!");
            return false;
        }

        if (playerMovement == null)
        {
            Debug.LogWarning("[AirSlash] PlayerMovement not found!");
            return false;
        }

        if (statsManager == null)
        {
            Debug.LogWarning("[AirSlash] StatsManager not found!");
            return false;
        }

        return true;
    }

    private void RotateProjectile(GameObject projectileGO, Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectileGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void InitializeProjectile(GameObject projectileGO, Vector3 fireDirection, int diceRoll)
    {
        AirSlashProjectile projectile = projectileGO.GetComponent<AirSlashProjectile>();
        if (projectile == null)
        {
            Debug.LogWarning("[AirSlash] AirSlashProjectile component missing on prefab!");
            Destroy(projectileGO);
            return;
        }

        int damage = DamageCalculator.CalculateSkillDamage(
            diceRoll,
            statsManager.strength,
            skillMultiplier
        );

        projectile.Initialize(
            fireDirection,
            projectileSpeed,
            projectileRange,
            damage,
            statsManager.knockbackForce,
            playerMovement.transform,
            enemyLayers,
            damagePopupPrefab,
            projectileHitRadius
        );
    }

    #endregion
}