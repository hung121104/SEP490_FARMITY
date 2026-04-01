using UnityEngine;
using CombatManager.Model;
using Photon.Pun;

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
        public static event System.Action<WeaponData> OnWeaponEquipped;

        /// <summary>Fired when weapon is unequipped.</summary>
        public static event System.Action OnWeaponUnequipped;

        #endregion

        #region Runtime State

        private WeaponData currentWeapon;
        private bool isWeaponEquipped = false;
        private PlayerAppearanceSync localAppearanceSync;

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

        private void Start()
        {
            localAppearanceSync = FindLocalAppearanceSync();
        }

        #endregion

        #region Equip / Unequip

        /// <summary>
        /// Equip a weapon. Fires OnWeaponEquipped and activates combat mode.
        /// </summary>
        public void EquipWeapon(WeaponData weaponData)
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

            // Ignore duplicate equip calls for the same weapon (common when attack shares click input).
            if (isWeaponEquipped && currentWeapon != null && currentWeapon.itemID == weaponData.itemID)
            {
                if (CombatModePresenter.Instance != null && !CombatModePresenter.Instance.IsCombatModeActive())
                    CombatModePresenter.Instance.SetCombatMode(true);
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

            PublishWeaponProperty(weaponData.itemID);

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

            PublishWeaponProperty(string.Empty);

            // Deactivate combat mode
            CombatModePresenter.Instance?.SetCombatMode(false);

            Debug.Log($"[WeaponEquipPresenter] '{weaponName}' unequipped! Combat mode OFF");
        }

        #endregion

        #region Public API

        public WeaponData GetCurrentWeapon() => currentWeapon;
        public bool IsWeaponEquipped() => isWeaponEquipped;
        public WeaponType GetCurrentWeaponType() => currentWeapon?.weaponType ?? WeaponType.None;

        private void PublishWeaponProperty(string weaponItemId)
        {
            if (!PhotonNetwork.IsConnected)
                return;

            if (localAppearanceSync == null)
                localAppearanceSync = FindLocalAppearanceSync();

            localAppearanceSync?.SetWeapon(weaponItemId ?? string.Empty);
        }

        private static PlayerAppearanceSync FindLocalAppearanceSync()
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                    return go.GetComponent<PlayerAppearanceSync>();
            }

            return null;
        }

        #endregion
    }
}