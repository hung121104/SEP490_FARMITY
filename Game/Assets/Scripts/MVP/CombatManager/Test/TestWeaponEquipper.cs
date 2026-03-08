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
        [SerializeField] private WeaponDataSO testSwordBronze;
        [SerializeField] private WeaponDataSO testSwordIron;
        [SerializeField] private WeaponDataSO testSpear;
        [SerializeField] private WeaponDataSO testStaff;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode equipSwordBronzeKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode equipSwordIronKey   = KeyCode.Alpha5;
        [SerializeField] private KeyCode equipSpearKey       = KeyCode.Alpha6;
        [SerializeField] private KeyCode equipStaffKey       = KeyCode.Alpha7;
        [SerializeField] private KeyCode unequipKey          = KeyCode.Alpha8;

        private void Update()
        {
            if (Input.GetKeyDown(equipSwordBronzeKey))
                TryEquip(testSwordBronze, "Sword Bronze");

            if (Input.GetKeyDown(equipSwordIronKey))
                TryEquip(testSwordIron, "Sword Iron");

            if (Input.GetKeyDown(equipSpearKey))
                TryEquip(testSpear, "Spear");

            if (Input.GetKeyDown(equipStaffKey))
                TryEquip(testStaff, "Staff");

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
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("=== TEST WEAPON EQUIPPER ===");
            GUILayout.Label($"[{equipSwordBronzeKey}] Equip Sword Bronze");
            GUILayout.Label($"[{equipSwordIronKey}]   Equip Sword Iron");
            GUILayout.Label($"[{equipSpearKey}]   Equip Spear");
            GUILayout.Label($"[{equipStaffKey}]   Equip Staff");
            GUILayout.Label($"[{unequipKey}]   Unequip");

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