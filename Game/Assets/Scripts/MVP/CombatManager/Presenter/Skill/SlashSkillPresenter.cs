using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Handles ALL slash-type skills (SkillCategory.Slash).
    /// Replaces: WeaponSkillSwordSpecial + WeaponSkillSpearSpecial.
    /// Reads all settings from SkillData SO - no hardcoded values.
    /// Attach ONE instance to CombatSystem GameObject.
    /// SkillHotbarPresenter finds this by SkillCategory.Slash.
    /// </summary>
    public class SlashSkillPresenter : SkillPresenter
    {
        public static SlashSkillPresenter Instance { get; private set; }

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

                Debug.Log($"[SlashSkillPresenter] SkillData set: {skillData.skillName}");
            }
        }

        public SkillData GetCurrentSkillData() => currentSkillData;

        #endregion

        #region SkillPresenter Abstract Implementation

        protected override SkillIndicatorData GetIndicatorData()
        {
            // No indicator for melee slash skills
            // Future: add arc/cone indicator
            return null;
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            if (currentSkillData == null)
            {
                Debug.LogWarning("[SlashSkillPresenter] No SkillData assigned!");
                yield break;
            }

            SpawnSlashVFX(finalDamage, direction);
            yield return new WaitForSeconds(
                currentSkillData.slashVFXDuration > 0
                    ? currentSkillData.slashVFXDuration
                    : 0.1f
            );
        }

        #endregion

        #region Slash VFX Logic

        private void SpawnSlashVFX(int damage, Vector3 direction)
        {
            if (currentSkillData.slashVFXPrefab == null)
            {
                Debug.LogWarning($"[SlashSkillPresenter] " +
                                 $"slashVFXPrefab not assigned in {currentSkillData.skillName}!");
                return;
            }

            if (centerPoint == null)
            {
                Debug.LogWarning("[SlashSkillPresenter] CenterPoint not found!");
                return;
            }

            Vector3 spawnPos = centerPoint.position
                             + direction * currentSkillData.slashVFXSpawnOffset
                             + (Vector3)currentSkillData.slashVFXPositionOffset;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject vfxObj = Instantiate(
                currentSkillData.slashVFXPrefab,
                spawnPos,
                Quaternion.Euler(0f, 0f, angle)
            );

            // ✅ Flip fix for left-facing direction
            if (direction.x < 0)
            {
                Vector3 scale = vfxObj.transform.localScale;
                scale.y *= -1;
                vfxObj.transform.localScale = scale;
            }

            SlashHitboxPresenter hitbox = vfxObj.GetComponent<SlashHitboxPresenter>();
            if (hitbox != null)
            {
                hitbox.Initialize(
                    damage,
                    currentSkillData.slashKnockbackForce,
                    enemyLayers,
                    playerTransform,
                    currentSkillData.damagePopupPrefab,
                    currentSkillData.slashVFXDuration
                );
                Debug.Log($"[SlashSkillPresenter] VFX spawned! " +
                          $"Skill={currentSkillData.skillName} | Damage={damage}");
            }
            else
            {
                Debug.LogWarning("[SlashSkillPresenter] " +
                                 "SlashHitboxPresenter missing on VFX prefab!");
            }

            Destroy(vfxObj, currentSkillData.slashVFXDuration);
        }

        #endregion

        #region Virtual Overrides

        protected override void OnStart() =>
            Debug.Log("[SlashSkillPresenter] Ready!");

        protected override void OnChargeStart() =>
            Debug.Log($"[SlashSkillPresenter] Charging: {currentSkillData?.skillName}");

        protected override void OnAttackStart() =>
            Debug.Log($"[SlashSkillPresenter] Slashing: {currentSkillData?.skillName}");

        protected override void OnAttackEnd() =>
            Debug.Log($"[SlashSkillPresenter] Done: {currentSkillData?.skillName}");

        protected override void OnSkillCancelled() =>
            Debug.Log($"[SlashSkillPresenter] Cancelled: {currentSkillData?.skillName}");

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