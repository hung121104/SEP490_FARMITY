using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.SO;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Weapon skill for Sword type.
    /// Extends SkillPresenter - inherits dice roll, confirm/cancel, cooldown.
    /// Only usable when Sword type weapon is equipped.
    /// Triggered by R key via SkillHotbarPresenter.
    /// </summary>
    public class WeaponSkillSwordSpecial : SkillPresenter
    {
        public static WeaponSkillSwordSpecial Instance { get; private set; }

        [Header("VFX")]
        [SerializeField] private GameObject swordSpecialVFXPrefab;
        [SerializeField] private float vfxDuration = 0.5f;
        [SerializeField] private float vfxSpawnOffset = 1.2f;
        [SerializeField] private Vector2 vfxPositionOffset = Vector2.zero;

        [Header("Hitbox Settings")]
        [SerializeField] private float knockbackForce = 5f;
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
            if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Sword)
            {
                Debug.LogWarning("[WeaponSkillSwordSpecial] Sword not equipped!");
                return;
            }

            // ✅ Pass to base - handles dice, confirm, cooldown, animation
            base.TriggerSkill();
        }

        #endregion

        #region SkillPresenter Abstract Implementation

        protected override CombatManager.Model.SkillIndicatorData GetIndicatorData()
        {
            // No indicator for weapon skill (melee range)
            // Future: add cone/arc indicator for sword sweep
            return null;
        }

        protected override IEnumerator OnExecute(int finalDamage, Vector3 direction)
        {
            SpawnSwordVFX(finalDamage, direction);
            yield return null;
        }

        #endregion

        #region VFX

        private void SpawnSwordVFX(int damage, Vector3 direction)
        {
            if (swordSpecialVFXPrefab == null)
            {
                Debug.LogWarning("[WeaponSkillSwordSpecial] VFX prefab not assigned!");
                return;
            }

            if (centerPoint == null)
            {
                Debug.LogWarning("[WeaponSkillSwordSpecial] CenterPoint not found!");
                return;
            }

            Vector3 spawnPos = centerPoint.position
                             + direction * vfxSpawnOffset
                             + (Vector3)vfxPositionOffset;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            GameObject vfxObj = Instantiate(
                swordSpecialVFXPrefab,
                spawnPos,
                Quaternion.Euler(0f, 0f, angle)
            );

            // ✅ Same flip fix as normal attack
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
                    knockbackForce,
                    enemyLayers,
                    playerTransform,
                    damagePopupPrefab,
                    vfxDuration
                );
                Debug.Log($"[WeaponSkillSwordSpecial] VFX spawned | damage={damage}");
            }
            else
            {
                Debug.LogWarning("[WeaponSkillSwordSpecial] SlashHitboxPresenter missing on VFX!");
            }

            Destroy(vfxObj, vfxDuration);
        }

        #endregion
    }
}