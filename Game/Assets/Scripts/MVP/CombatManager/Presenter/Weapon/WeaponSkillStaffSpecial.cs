using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Weapon skill for Staff type.
    /// Extends SkillPresenter - inherits dice roll, confirm/cancel, cooldown.
    /// Fires a projectile on execute.
    /// Only usable when Staff type weapon is equipped.
    /// Triggered by R key via SkillHotbarPresenter.
    /// </summary>
    public class WeaponSkillStaffSpecial : SkillPresenter
    {
        public static WeaponSkillStaffSpecial Instance { get; private set; }

        [Header("Projectile Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private float projectileRange = 8f;
        [SerializeField] private float projectileKnockback = 5f;

        #region Unity Lifecycle

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            base.Awake();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region SkillPresenter - Weapon Guard

        public override void TriggerSkill()
        {
            var currentWeapon = WeaponEquipPresenter.Instance?.GetCurrentWeapon();
            if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Staff)
            {
                Debug.LogWarning("[WeaponSkillStaffSpecial] Staff not equipped!");
                return;
            }

            // ✅ Pass to base - handles dice, confirm, cooldown, animation
            base.TriggerSkill();
        }

        #endregion

        #region SkillPresenter Abstract Implementation

        protected override CombatManager.Model.SkillIndicatorData GetIndicatorData()
        {
            // Future: add line/range indicator showing projectile path
            return null;
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            FireProjectile(finalDamage, direction);
            yield return null;
        }

        #endregion

        #region Projectile

        private void FireProjectile(int damage, Vector3 direction)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[WeaponSkillStaffSpecial] Projectile prefab not assigned!");
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[WeaponSkillStaffSpecial] playerTransform is null!");
                return;
            }

            GameObject projectileGO = Instantiate(
                projectilePrefab,
                playerTransform.position,
                Quaternion.identity
            );

            ProjectileModel projectileModel = new ProjectileModel
            {
                direction       = direction.normalized,
                speed           = projectileSpeed,
                maxRange        = projectileRange,
                damage          = damage,
                knockbackForce  = projectileKnockback,
                enemyLayers     = enemyLayers,
                playerTransform = playerTransform
            };

            ProjectilePresenter projectilePresenter =
                projectileGO.GetComponent<ProjectilePresenter>();

            if (projectilePresenter == null)
            {
                Debug.LogWarning("[WeaponSkillStaffSpecial] ProjectilePresenter missing on prefab!");
                Destroy(projectileGO);
                return;
            }

            projectilePresenter.Initialize(projectileModel);

            Debug.Log($"[WeaponSkillStaffSpecial] Projectile fired! " +
                      $"Damage={damage} | Dir={direction} | Range={projectileRange}");
        }

        #endregion
    }
}