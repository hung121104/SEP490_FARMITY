using UnityEngine;
using CombatManager.Presenter;
using System.Collections.Generic;

namespace CombatManager.Test
{
    /// <summary>
    /// Legacy debug helper kept only to satisfy project file includes.
    /// Runtime weapon activation is now driven by hotbar item selection.
    /// </summary>
    public class TestWeaponEquipper : MonoBehaviour
    {
        [SerializeField] private KeyCode unequipKey = KeyCode.Alpha8;

        private string lastResult = "";

        private void AddAllWeaponsToInventory()
        {
            if (ItemCatalogService.Instance == null || !ItemCatalogService.Instance.IsReady)
            {
                lastResult = "ItemCatalog not ready yet.";
                return;
            }

            InventoryGameView inventory = FindFirstObjectByType<InventoryGameView>();
            if (inventory == null)
            {
                lastResult = "InventoryGameView not found in scene.";
                return;
            }

            List<ItemData> weapons = ItemCatalogService.Instance.GetItemsByType(ItemType.Weapon);
            if (weapons.Count == 0)
            {
                lastResult = "No weapon items found in catalog.";
                return;
            }

            int added = 0;
            int failed = 0;
            for (int i = 0; i < weapons.Count; i++)
            {
                bool ok = inventory.AddItem(weapons[i], 1);
                if (ok) added++;
                else failed++;
            }

            lastResult = $"Added weapons: {added}, failed: {failed}";
            Debug.Log($"[TestWeaponEquipper] {lastResult}");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 140));
            GUILayout.Label("=== WEAPON DEBUG (ITEM-DRIVEN) ===");
            GUILayout.Label("Select a weapon in hotbar to auto-activate it.");
            if (GUILayout.Button("Add all weapon items to inventory", GUILayout.Height(24)))
            {
                AddAllWeaponsToInventory();
            }
            if (GUILayout.Button($"Force unequip [{unequipKey}]", GUILayout.Height(24)))
            {
                WeaponEquipPresenter.Instance?.UnequipWeapon();
            }
            if (!string.IsNullOrEmpty(lastResult))
            {
                GUILayout.Label(lastResult);
            }
            GUILayout.EndArea();
        }
    }
}
