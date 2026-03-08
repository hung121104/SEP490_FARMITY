using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Weapon skill for Spear type.
    /// Extends SkillPresenter - inherits dice roll, confirm/cancel, cooldown.
    /// Only usable when Spear type weapon is equipped.
    /// Triggered by R key via SkillHotbarPresenter.
    /// </summary>
    public class WeaponSkillSpearSpecial : SkillPresenter
    {
        public static WeaponSkillSpearSpecial Instance { get; private set; }

        [Header("VFX")]
        [SerializeField] private GameObject spearSpecialVFXPrefab;
        [SerializeField] private float vfxDuration = 0.5f;
        [SerializeField] private float vfxSpawnOffset = 1.5f;
        [SerializeField] private Vector2 vfxPositionOffset = Vector2.zero;

        [Header("Hitbox Settings")]
        [SerializeField] private float knockbackForce = 7f;
        [SerializeField] private GameObject damagePopupPrefab;

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
            if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Spear)
            {
                Debug.LogWarning("[WeaponSkillSpearSpecial] Spear not equipped!");
                return;
            }

            // ✅ Pass to base - handles dice, confirm, cooldown, animation
            base.TriggerSkill();
        }

        #endregion

        #region SkillPresenter Abstract Implementation

        protected override CombatManager.Model.SkillIndicatorData GetIndicatorData()
        {
            // No indicator for now
            // Future: add long line indicator for spear thrust range
            return null;
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            SpawnSpearVFX(finalDamage, direction);
            yield return null;
        }

        #endregion

        #region VFX

        private void SpawnSpearVFX(int damage, Vector3 direction)
        {
            if (spearSpecialVFXPrefab == null)
            {
                Debug.LogWarning("[WeaponSkillSpearSpecial] VFX prefab not assigned!");
                return;
            }

            if (centerPoint == null)
            {
                Debug.LogWarning("[WeaponSkillSpearSpecial] CenterPoint not found!");
                return;
            }

            Vector3 spawnPos = centerPoint.position
                             + direction * vfxSpawnOffset
                             + (Vector3)vfxPositionOffset;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject vfxObj = Instantiate(
                spearSpecialVFXPrefab,
                spawnPos,
                Quaternion.Euler(0f, 0f, angle)
            );

            SlashHitboxPresenter hitbox = vfxObj.GetComponent<SlashHitboxPresenter>();
            if (hitbox != null)
            {
                hitbox.Initialize(
                    damage,
                    knockbackForce,
                    enemyLayers,
                    playerTransform,
                    damagePopupPrefab,
                    vfxDuration
                );
                Debug.Log($"[WeaponSkillSpearSpecial] VFX spawned | damage={damage}");
            }
            else
            {
                Debug.LogWarning("[WeaponSkillSpearSpecial] SlashHitboxPresenter missing on VFX!");
            }

            Destroy(vfxObj, vfxDuration);
        }

        #endregion
    }
}