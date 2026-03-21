using UnityEngine;
using CombatManager.Model;

namespace CombatManager.SO
{
    /// <summary>
    /// Runtime combat skill model loaded from database catalog.
    /// Kept in existing namespace to avoid broad refactors.
    /// </summary>
    [System.Serializable]
    public class SkillData
    {
        [Header("Identification")]
        public string skillId = "";
        public string skillName = "Unnamed Skill";
        public string skillDescription = "";
        public string iconUrl = "";

        [Header("UI Display")]
        public Sprite skillIcon;
        public Color skillColor = Color.white;

        [Header("Ownership & Category")]
        [Tooltip("PlayerSkill = hotbar slots. WeaponSkill = weapon slot (R key) only.")]
        public SkillOwnership skillOwnership = SkillOwnership.PlayerSkill;
        [Tooltip("Determines which presenter handles execution logic.")]
        public SkillCategory skillCategory = SkillCategory.None;
        [Tooltip("For WeaponSkill only - which weapon type this belongs to.")]
        public WeaponType requiredWeaponType = WeaponType.None;

        [Header("Gameplay")]
        public float cooldown = 3f;
        public DiceTier diceTier = DiceTier.D6;
        public float skillMultiplier = 1.5f;

        [Header("Projectile Settings (Category = Projectile)")]
        [Tooltip("Projectile prefab with ProjectilePresenter attached.")]
        public string projectilePrefabKey = "";
        public GameObject projectilePrefab;
        public float projectileSpeed = 10f;
        public float projectileRange = 8f;
        public float projectileKnockback = 5f;

        [Header("Slash Settings (Category = Slash)")]
        [Tooltip("VFX prefab with SlashHitboxPresenter attached.")]
        public string slashVfxKey = "";
        public GameObject slashVFXPrefab;
        public float slashVFXDuration = 0.5f;
        public float slashVFXSpawnOffset = 1.2f;
        public float slashVfxPositionOffsetX = 0f;
        public float slashVfxPositionOffsetY = 0f;
        public Vector2 slashVFXPositionOffset = Vector2.zero;
        public float slashKnockbackForce = 5f;
        public string damagePopupPrefabKey = "";
        public GameObject damagePopupPrefab;

        #region Public Helpers

        public bool IsPlayerSkill => skillOwnership == SkillOwnership.PlayerSkill;
        public bool IsWeaponSkill => skillOwnership == SkillOwnership.WeaponSkill;
        public bool IsProjectile  => skillCategory  == SkillCategory.Projectile;
        public bool IsSlash       => skillCategory  == SkillCategory.Slash;

        #endregion
    }
}