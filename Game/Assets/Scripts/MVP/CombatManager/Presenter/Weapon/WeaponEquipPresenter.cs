using UnityEngine;
using CombatManager.SO;
using CombatManager.Model;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Weapon Equip system.
    /// Manages equipping/unequipping weapons.
    /// Drives CombatMode ON/OFF based on weapon state.
    /// Bridge between item system and combat system.
    /// </summary>
    public class WeaponEquipPresenter : MonoBehaviour
    {
        #region Singleton

        public static WeaponEquipPresenter Instance { get; private set; }

        #endregion

        #region Events

        /// <summary>Fired when a weapon is equipped. Passes the weapon data.</summary>
        public static event System.Action<WeaponDataSO> OnWeaponEquipped;

        /// <summary>Fired when weapon is unequipped.</summary>
        public static event System.Action OnWeaponUnequipped;

        #endregion

        #region Runtime State

        private WeaponDataSO currentWeapon;
        private bool isWeaponEquipped = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Equip / Unequip

        /// <summary>
        /// Equip a weapon. Fires OnWeaponEquipped and activates combat mode.
        /// </summary>
        public void EquipWeapon(WeaponDataSO weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogWarning("[WeaponEquipPresenter] Cannot equip null weapon!");
                return;
            }

            if (!weaponData.IsValid())
            {
                Debug.LogWarning($"[WeaponEquipPresenter] Weapon '{weaponData.weaponName}' is invalid!");
                return;
            }

            // Unequip current weapon first if any
            if (isWeaponEquipped && currentWeapon != null)
            {
                UnequipWeapon();
            }

            currentWeapon = weaponData;
            isWeaponEquipped = true;

            Debug.Log($"[WeaponEquipPresenter] Equipping: {weaponData.weaponName} ({weaponData.weaponType}) Tier {weaponData.tier}");

            // Fire equip event FIRST (so listeners can prepare)
            OnWeaponEquipped?.Invoke(weaponData);

            // Activate combat mode
            CombatModePresenter.Instance?.SetCombatMode(true);

            Debug.Log($"[WeaponEquipPresenter] '{weaponData.weaponName}' equipped! Combat mode ON");
        }

        /// <summary>
        /// Unequip current weapon. Fires OnWeaponUnequipped and deactivates combat mode.
        /// </summary>
        public void UnequipWeapon()
        {
            if (!isWeaponEquipped)
            {
                Debug.Log("[WeaponEquipPresenter] No weapon equipped!");
                return;
            }

            string weaponName = currentWeapon?.weaponName ?? "Unknown";

            currentWeapon = null;
            isWeaponEquipped = false;

            // Fire unequip event FIRST
            OnWeaponUnequipped?.Invoke();

            // Deactivate combat mode
            CombatModePresenter.Instance?.SetCombatMode(false);

            Debug.Log($"[WeaponEquipPresenter] '{weaponName}' unequipped! Combat mode OFF");
        }

        #endregion

        #region Public API

        public WeaponDataSO GetCurrentWeapon() => currentWeapon;
        public bool IsWeaponEquipped() => isWeaponEquipped;
        public WeaponType GetCurrentWeaponType() => currentWeapon?.weaponType ?? WeaponType.None;

        #endregion
    }
}