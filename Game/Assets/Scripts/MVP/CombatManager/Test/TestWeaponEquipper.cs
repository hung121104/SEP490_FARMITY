using UnityEngine;
using CombatManager.SO;
using CombatManager.Presenter;

namespace CombatManager.Test
{
    /// <summary>
    /// TEMPORARY TEST SCRIPT - Remove when item hotbar integration is done.
    /// Simulates weapon equip/unequip via keyboard.
    /// Attach to any GameObject in scene.
    /// </summary>
    public class TestWeaponEquipper : MonoBehaviour
    {
        [Header("Test Weapons - Assign in Inspector")]
        [SerializeField] private WeaponDataSO testSword;
        [SerializeField] private WeaponDataSO testStaff;
        [SerializeField] private WeaponDataSO testSpear;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode equipSwordKey = KeyCode.Alpha7;
        [SerializeField] private KeyCode equipStaffKey = KeyCode.Alpha8;
        [SerializeField] private KeyCode equipSpearKey = KeyCode.Alpha9;
        [SerializeField] private KeyCode unequipKey = KeyCode.Alpha0;

        private void Update()
        {
            if (Input.GetKeyDown(equipSwordKey))
                TryEquip(testSword, "Sword");

            if (Input.GetKeyDown(equipStaffKey))
                TryEquip(testStaff, "Staff");

            if (Input.GetKeyDown(equipSpearKey))
                TryEquip(testSpear, "Spear");

            if (Input.GetKeyDown(unequipKey))
                TryUnequip();
        }

        private void TryEquip(WeaponDataSO weapon, string typeName)
        {
            if (weapon == null)
            {
                Debug.LogWarning($"[TestWeaponEquipper] {typeName} not assigned in Inspector!");
                return;
            }

            if (WeaponEquipPresenter.Instance == null)
            {
                Debug.LogError("[TestWeaponEquipper] WeaponEquipPresenter.Instance is null!");
                return;
            }

            WeaponEquipPresenter.Instance.EquipWeapon(weapon);
        }

        private void TryUnequip()
        {
            if (WeaponEquipPresenter.Instance == null)
            {
                Debug.LogError("[TestWeaponEquipper] WeaponEquipPresenter.Instance is null!");
                return;
            }

            WeaponEquipPresenter.Instance.UnequipWeapon();
        }

        private void OnGUI()
        {
            // Debug overlay in Game view
            GUILayout.BeginArea(new Rect(10, 10, 300, 120));
            GUILayout.Label("=== TEST WEAPON EQUIPPER ===");
            GUILayout.Label($"[{equipSwordKey}] Equip Sword");
            GUILayout.Label($"[{equipStaffKey}] Equip Staff");
            GUILayout.Label($"[{equipSpearKey}] Equip Spear");
            GUILayout.Label($"[{unequipKey}] Unequip");

            if (WeaponEquipPresenter.Instance != null)
            {
                string status = WeaponEquipPresenter.Instance.IsWeaponEquipped()
                    ? $"Equipped: {WeaponEquipPresenter.Instance.GetCurrentWeapon()?.weaponName}"
                    : "No weapon equipped";
                GUILayout.Label($"Status: {status}");
            }
            GUILayout.EndArea();
        }
    }
}