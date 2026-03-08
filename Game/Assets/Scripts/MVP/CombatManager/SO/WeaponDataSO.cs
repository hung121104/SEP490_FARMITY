using UnityEngine;
using CombatManager.Model;

namespace CombatManager.SO
{
    /// <summary>
    /// Weapon configuration as ScriptableObject.
    /// One asset per weapon (e.g. BronzeSword, IronSword, WoodenStaff).
    /// Same weapon TYPE shares same prefab but different stats/sprite.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Combat/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Unnamed Weapon";
        public string weaponId = "";
        public WeaponType weaponType = WeaponType.None;

        [Header("Visual")]
        public Sprite weaponIcon;
        [Tooltip("Prefab with SpriteRenderer + Animator. One per weapon TYPE.")]
        public GameObject weaponPrefab;

        [Header("Stats")]
        public int damage = 10;
        [Tooltip("1 = Bronze, 2 = Iron, 3 = Steel. Easy to extend.")]
        public int tier = 1;

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
                1 => "Bronze",
                2 => "Iron",
                3 => "Steel",
                _ => $"Tier {tier}"
            };
        }

        #endregion
    }
}