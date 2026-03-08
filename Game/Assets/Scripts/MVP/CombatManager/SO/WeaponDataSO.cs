using UnityEngine;
using CombatManager.Model;

namespace CombatManager.SO
{
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Combat/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Unnamed Weapon";
        public string weaponId = "";
        public WeaponType weaponType = WeaponType.None;

        [Header("Visual")]
        public Sprite weaponIcon;
        [Tooltip("Prefab with SpriteRenderer + Animator. One per weapon TYPE + TIER.")]
        public GameObject weaponPrefab;

        [Header("Stats")]
        public int damage = 10;
        [Tooltip("1 = Bronze/Wooden, 2 = Iron, 3 = Steel/Crystal, 4 = Legendary")]
        public int tier = 1;

        [Header("Attack Behaviour")]
        [Tooltip("Seconds between normal attacks. Lower = faster.")]
        public float attackCooldown = 0.5f;
        [Tooltip("Force applied to enemy on hit.")]
        public float knockbackForce = 5f;

        [Header("Projectile Settings (Staff only)")]
        [Tooltip("How fast the projectile travels.")]
        public float projectileSpeed = 10f;
        [Tooltip("How far the projectile travels before despawning.")]
        public float projectileRange = 8f;
        [Tooltip("Knockback force applied by projectile hit.")]
        public float projectileKnockback = 4f;

        [Header("Skill")]
        [Tooltip("The skill unlocked when this weapon is equipped.")]
        public SkillData linkedSkill;

        [Header("Description")]
        [TextArea(2, 4)]
        public string weaponDescription = "";

        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(weaponName))
                weaponName = name;

            if (string.IsNullOrEmpty(weaponId))
                weaponId = name.ToLower().Replace(" ", "_");

            // Auto-apply suggested defaults per weapon type
            // so new SO assets start with sensible values
            if (attackCooldown <= 0f) attackCooldown = 0.5f;
            if (knockbackForce <= 0f) knockbackForce = 5f;
            if (projectileSpeed <= 0f) projectileSpeed = 10f;
            if (projectileRange <= 0f) projectileRange = 8f;
        }

        #endregion

        #region Public API

        public bool IsValid()
        {
            return weaponType != WeaponType.None
                && weaponPrefab != null
                && !string.IsNullOrEmpty(weaponName);
        }

        public string GetTierName()
        {
            return tier switch
            {
                1 => weaponType == WeaponType.Staff ? "Wooden" : "Bronze",
                2 => "Iron",
                3 => weaponType == WeaponType.Staff ? "Crystal" : "Steel",
                4 => "Legendary",
                _ => $"Tier {tier}"
            };
        }

        #endregion
    }
}