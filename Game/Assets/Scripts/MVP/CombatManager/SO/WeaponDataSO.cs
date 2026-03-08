using UnityEngine;
using CombatManager.Model;

namespace CombatManager.SO
{
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Combat/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Item Identity")]
        public string weaponId = "";
        public string weaponName = "Unnamed Weapon";
        [TextArea(2, 4)]
        public string weaponDescription = "";
        public Sprite weaponIcon;

        [Header("Item System")]
        // ✅ Added for item hotbar compatibility
        public string itemType = "Weapon";
        public string itemCategory = "Equipment";
        public bool isStackable = false;
        public int maxStack = 1;

        [Header("Weapon Identity")]
        public WeaponType weaponType = WeaponType.None;
        [Tooltip("Prefab with SpriteRenderer + Animator.")]
        public GameObject weaponPrefab;

        [Header("Stats")]
        public int damage = 10;
        [Tooltip("1=Bronze/Wooden 2=Iron 3=Steel/Crystal 4=Legendary")]
        public int tier = 1;
        // ✅ Added for item system compatibility
        public int critChance = 5;

        [Header("Attack Behaviour")]
        public float attackCooldown = 0.5f;
        public float knockbackForce = 5f;

        [Header("Projectile Settings (Staff only)")]
        public float projectileSpeed = 10f;
        public float projectileRange = 8f;
        public float projectileKnockback = 4f;

        [Header("Skill")]
        public SkillData linkedSkill;

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