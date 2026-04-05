using UnityEngine;
using CombatManager.Model;
using Newtonsoft.Json;

namespace CombatManager.Model
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
        [Tooltip("Minimum player level required for this skill to appear in Skill Management panel.")]
        [JsonProperty("unlockLevel")]
        public int unlockLevel = 1;
        [Tooltip("Determines which presenter handles execution logic.")]
        [JsonProperty("category")]
        public SkillCategory skillCategory = SkillCategory.None;
        [Tooltip("For WeaponSkill only - which weapon type this belongs to.")]
        [JsonProperty("requiredWeaponType")]
        public WeaponType requiredWeaponType = WeaponType.None;

        [Header("Buff Settings (Category = Buff)")]
        [Tooltip("Small logic type under Buff category.")]
        [JsonProperty("buffSubCategory")]
        public BuffSkillSubCategory buffSubCategory = BuffSkillSubCategory.None;
        [Tooltip("Buff numeric magnitude. InstantHeal/HoT use HP points. StaminaRegen/MoveSpeedPercent use percent or multiplier based on buff type.")]
        [JsonProperty("buffValue")]
        public float buffValue = 0f;
        [Tooltip("Duration in seconds for timed buffs (HoT, stamina regen, move speed).")]
        [JsonProperty("buffDuration")]
        public float buffDuration = 0f;
        [Tooltip("Tick interval in seconds for periodic effects such as HealOverTime.")]
        [JsonProperty("buffTickInterval")]
        public float buffTickInterval = 1f;

        [Header("Gameplay")]
        [JsonProperty("cooldown")]
        public float cooldown = 3f;
        [JsonProperty("diceTier")]
        public DiceTier diceTier = DiceTier.D6;
        [JsonProperty("skillMultiplier")]
        public float skillMultiplier = 1.5f;

        [Header("Projectile Settings (Category = Projectile)")]
        [JsonProperty("projectileSpeed")]
        public float projectileSpeed = 10f;
        [JsonProperty("projectileRange")]
        public float projectileRange = 8f;
        [JsonProperty("projectileKnockback")]
        public float projectileKnockback = 5f;

        [Header("Slash Settings (Category = Slash)")]
        [JsonProperty("skillVisualConfigId")]
        public string skillVisualConfigId = "";
        [JsonProperty("slashVfxDuration")]
        public float slashVFXDuration = 0.5f;
        [JsonProperty("slashVfxSpawnOffset")]
        public float slashVFXSpawnOffset = 1.2f;
        [JsonProperty("slashVfxPositionOffsetX")]
        public float slashVfxPositionOffsetX = 0f;
        [JsonProperty("slashVfxPositionOffsetY")]
        public float slashVfxPositionOffsetY = 0f;
        [JsonProperty("slashKnockbackForce")]
        public float slashKnockbackForce = 5f;

        [Header("AoE Settings (Category = AoE)")]
        [JsonProperty("aoeCastRange")]
        public float aoeCastRange = 6f;
        [JsonProperty("aoeRadius")]
        public float aoeRadius = 2f;
        [JsonProperty("aoeVfxDuration")]
        public float aoeVfxDuration = 1f;

        #region Public Helpers

        public bool IsPlayerSkill => skillOwnership == SkillOwnership.PlayerSkill;
        public bool IsWeaponSkill => skillOwnership == SkillOwnership.WeaponSkill;
        public bool IsProjectile  => skillCategory  == SkillCategory.Projectile;
        public bool IsSlash       => skillCategory  == SkillCategory.Slash;
        public bool IsAoE         => skillCategory  == SkillCategory.AoE;
        public bool IsBuff        => skillCategory  == SkillCategory.Buff;
        public Vector2 SlashVfxPositionOffset => new Vector2(slashVfxPositionOffsetX, slashVfxPositionOffsetY);

        #endregion
    }
}