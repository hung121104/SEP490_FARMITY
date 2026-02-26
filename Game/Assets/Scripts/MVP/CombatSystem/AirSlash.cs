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
            Debug.LogWarning("[AirSlash] Projectile prefab not assigned!");
            return;
        }

        if (playerMovement == null)
        {
            Debug.LogWarning("[AirSlash] PlayerMovement not found!");
            return;
        }

        // Get direction from pointerController (current mouse direction)
        Vector3 fireDirection = pointerController?.GetPointerDirection() ?? Vector3.right;

        // Spawn from player position
        GameObject projectileGO = Instantiate(projectilePrefab, playerMovement.transform.position, Quaternion.identity);

        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        projectileGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        AirSlashProjectile projectile = projectileGO.GetComponent<AirSlashProjectile>();
        if (projectile != null)
        {
            int damage = DamageCalculator.CalculateSkillDamage(
                diceRoll,
                statsManager.strength,
                skillMultiplier
            );

            // Get damage popup prefab from EnemyCombat
            GameObject popupPrefab = enemyCombat?.damagePopupPrefab;

            projectile.Initialize(
                fireDirection,
                projectileSpeed,
                projectileRange,
                damage,
                statsManager.knockbackForce,
                playerMovement.transform,
                enemyLayers,
                popupPrefab,
                projectileHitRadius
            );
        }
        else
        {
            Debug.LogWarning("[AirSlash] AirSlashProjectile component missing on prefab!");
        }
    }

    #endregion
}