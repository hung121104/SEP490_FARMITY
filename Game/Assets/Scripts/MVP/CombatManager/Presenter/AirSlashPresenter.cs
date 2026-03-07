using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Presenter;

namespace CombatManager.Presenter
{
    public class AirSlashPresenter : SkillPresenter
    {
        #region Serialized Fields

        [Header("AirSlash Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float projectileRange = 8f;
        [SerializeField] private float attackAnimationDuration = 0.5f;

        #endregion

        #region SkillPresenter Implementation

        protected override CombatManager.Model.SkillIndicatorData GetIndicatorData()
        {
            return CombatManager.Model.SkillIndicatorData.Arrow(projectileRange);
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            FireProjectile(finalDamage, direction);
            yield return new WaitForSeconds(attackAnimationDuration);
        }

        #endregion

        #region Projectile Logic

        private void FireProjectile(int damage, Vector3 direction)
        {
            if (!ValidateSetup()) return;

            GameObject projectileGO = Instantiate(
                projectilePrefab,
                playerTransform.position,
                Quaternion.identity
            );

            AirSlashProjectileModel projectileModel = new AirSlashProjectileModel
            {
                direction       = direction.normalized,
                speed           = projectileSpeed,
                maxRange        = projectileRange,
                damage          = damage,
                knockbackForce  = GetKnockbackForce(),
                enemyLayers     = enemyLayers,
                playerTransform = playerTransform
            };

            AirSlashProjectilePresenter projectilePresenter =
                projectileGO.GetComponent<AirSlashProjectilePresenter>();

            if (projectilePresenter == null)
            {
                Debug.LogWarning("[AirSlash] AirSlashProjectilePresenter missing on prefab!");
                Destroy(projectileGO);
                return;
            }

            projectilePresenter.Initialize(projectileModel);

            Debug.Log($"[AirSlash] Fired! Damage: {damage} | Direction: {direction} | Range: {projectileRange}");
        }

        private bool ValidateSetup()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[AirSlash] Projectile prefab not assigned!");
                return false;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[AirSlash] PlayerTransform not found!");
                return false;
            }

            return true;
        }

        private float GetKnockbackForce()
        {
            if (statsPresenter != null)
                return statsPresenter.GetService().GetKnockbackForce();

            return 5f;
        }

        #endregion

        #region Virtual Overrides

        protected override void OnStart() =>
            Debug.Log("[AirSlash] Ready!");

        protected override void OnChargeStart() =>
            Debug.Log("[AirSlash] Charging...");

        protected override void OnAttackStart() =>
            Debug.Log("[AirSlash] Firing projectile!");

        protected override void OnAttackEnd() =>
            Debug.Log("[AirSlash] Done!");

        protected override void OnSkillCancelled() =>
            Debug.Log("[AirSlash] Cancelled!");

        #endregion
    }
}