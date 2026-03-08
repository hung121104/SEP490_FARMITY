using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Handles ALL projectile-type skills (SkillCategory.Projectile).
    /// Replaces: AirSlashPresenter + WeaponSkillStaffSpecial.
    /// Reads all settings from SkillData SO - no hardcoded values.
    /// Attach ONE instance to CombatSystem GameObject.
    /// SkillHotbarPresenter finds this by SkillCategory.Projectile.
    /// </summary>
    public class ProjectileSkillPresenter : SkillPresenter
    {
        public static ProjectileSkillPresenter Instance { get; private set; }

        // Current skill data being executed (set by SkillHotbarPresenter)
        private SkillData currentSkillData;

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

        #region Public API - Called by SkillHotbarPresenter

        /// <summary>
        /// Set which skill data to use before triggering.
        /// Called by SkillHotbarPresenter before TriggerSkill().
        /// </summary>
        public void SetSkillData(SkillData skillData)
        {
            currentSkillData = skillData;

            if (skillData != null)
            {
                // ✅ Override base SkillPresenter settings from SO data
                skillCooldown   = skillData.cooldown;
                skillTier       = skillData.diceTier;
                skillMultiplier = skillData.skillMultiplier;

                // Re-sync model with new values
                SyncModelFromSkillData();

                Debug.Log($"[ProjectileSkillPresenter] SkillData set: {skillData.skillName}");
            }
        }

        public SkillData GetCurrentSkillData() => currentSkillData;

        #endregion

        #region SkillPresenter Abstract Implementation

        protected override SkillIndicatorData GetIndicatorData()
        {
            if (currentSkillData == null) return null;
            return SkillIndicatorData.Arrow(currentSkillData.projectileRange);
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            if (currentSkillData == null)
            {
                Debug.LogWarning("[ProjectileSkillPresenter] No SkillData assigned!");
                yield break;
            }

            FireProjectile(finalDamage, direction);
            yield return new WaitForSeconds(0.1f);
        }

        #endregion

        #region Projectile Logic

        private void FireProjectile(int damage, Vector3 direction)
        {
            if (currentSkillData.projectilePrefab == null)
            {
                Debug.LogWarning($"[ProjectileSkillPresenter] " +
                                 $"projectilePrefab not assigned in {currentSkillData.skillName}!");
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[ProjectileSkillPresenter] playerTransform is null!");
                return;
            }

            GameObject projectileGO = Instantiate(
                currentSkillData.projectilePrefab,
                playerTransform.position,
                Quaternion.identity
            );

            ProjectileModel projectileModel = new ProjectileModel
            {
                direction       = direction.normalized,
                speed           = currentSkillData.projectileSpeed,
                maxRange        = currentSkillData.projectileRange,
                damage          = damage,
                knockbackForce  = currentSkillData.projectileKnockback,
                enemyLayers     = enemyLayers,
                playerTransform = playerTransform
            };

            ProjectilePresenter projectilePresenter =
                projectileGO.GetComponent<ProjectilePresenter>();

            if (projectilePresenter == null)
            {
                Debug.LogWarning("[ProjectileSkillPresenter] " +
                                 "ProjectilePresenter missing on prefab!");
                Destroy(projectileGO);
                return;
            }

            projectilePresenter.Initialize(projectileModel);

            Debug.Log($"[ProjectileSkillPresenter] Fired! " +
                      $"Skill={currentSkillData.skillName} | " +
                      $"Damage={damage} | Dir={direction} | " +
                      $"Speed={currentSkillData.projectileSpeed} | " +
                      $"Range={currentSkillData.projectileRange}");
        }

        #endregion

        #region Virtual Overrides

        protected override void OnStart() =>
            Debug.Log("[ProjectileSkillPresenter] Ready!");

        protected override void OnChargeStart() =>
            Debug.Log($"[ProjectileSkillPresenter] Charging: {currentSkillData?.skillName}");

        protected override void OnAttackStart() =>
            Debug.Log($"[ProjectileSkillPresenter] Firing: {currentSkillData?.skillName}");

        protected override void OnAttackEnd() =>
            Debug.Log($"[ProjectileSkillPresenter] Done: {currentSkillData?.skillName}");

        protected override void OnSkillCancelled() =>
            Debug.Log($"[ProjectileSkillPresenter] Cancelled: {currentSkillData?.skillName}");

        #endregion

        #region Private Helpers

        private void SyncModelFromSkillData()
        {
            model.skillCooldown   = skillCooldown;
            model.skillTier       = skillTier;
            model.skillMultiplier = skillMultiplier;
        }

        #endregion
    }
}