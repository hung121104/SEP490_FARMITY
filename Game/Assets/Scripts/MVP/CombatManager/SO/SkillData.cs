using UnityEngine;
using CombatManager.Model;
using Newtonsoft.Json;

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
        [JsonProperty("skillId")]
        public string skillId = "";
        [JsonProperty("skillName")]
        public string skillName = "Unnamed Skill";
        [JsonProperty("skillDescription")]
        public string skillDescription = "";
        [JsonProperty("iconUrl")]
        public string iconUrl = "";

        [Header("UI Display")]
        public Sprite skillIcon;
        public Color skillColor = Color.white;

        [Header("Ownership & Category")]
        [Tooltip("PlayerSkill = hotbar slots. WeaponSkill = weapon slot (R key) only.")]
        [JsonProperty("ownership")]
        public SkillOwnership skillOwnership = SkillOwnership.PlayerSkill;
        [Tooltip("Determines which presenter handles execution logic.")]
        [JsonProperty("category")]
        public SkillCategory skillCategory = SkillCategory.None;
        [Tooltip("For WeaponSkill only - which weapon type this belongs to.")]
        [JsonProperty("requiredWeaponType")]
        public WeaponType requiredWeaponType = WeaponType.None;

        [Header("Gameplay")]
        [JsonProperty("cooldown")]
        public float cooldown = 3f;
        [JsonProperty("diceTier")]
        public DiceTier diceTier = DiceTier.D6;
        [JsonProperty("skillMultiplier")]
        public float skillMultiplier = 1.5f;

        [Header("Projectile Settings (Category = Projectile)")]
        [Tooltip("Projectile prefab with ProjectilePresenter attached.")]
        [JsonProperty("projectilePrefabKey")]
        public string projectilePrefabKey = "";
        public GameObject projectilePrefab;
        [JsonProperty("projectileSpeed")]
        public float projectileSpeed = 10f;
        [JsonProperty("projectileRange")]
        public float projectileRange = 8f;
        [JsonProperty("projectileKnockback")]
        public float projectileKnockback = 5f;

        [Header("Slash Settings (Category = Slash)")]
        [Tooltip("VFX prefab with SlashHitboxPresenter attached.")]
        [JsonProperty("slashVfxKey")]
        public string slashVfxKey = "";
        public GameObject slashVFXPrefab;
        [JsonProperty("slashVfxDuration")]
        public float slashVFXDuration = 0.5f;
        [JsonProperty("slashVfxSpawnOffset")]
        public float slashVFXSpawnOffset = 1.2f;
        [JsonProperty("slashVfxPositionOffsetX")]
        public float slashVfxPositionOffsetX = 0f;
        [JsonProperty("slashVfxPositionOffsetY")]
        public float slashVfxPositionOffsetY = 0f;
        public Vector2 slashVFXPositionOffset = Vector2.zero;
        [JsonProperty("slashKnockbackForce")]
        public float slashKnockbackForce = 5f;
        [JsonProperty("damagePopupPrefabKey")]
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