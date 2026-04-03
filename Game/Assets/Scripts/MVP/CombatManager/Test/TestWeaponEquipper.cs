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
        private string lastResult = "";

        [ContextMenu("Add All Weapons To Inventory")]
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
                bool ok = inventory.AddItem(weapons[i].itemID, 1);
                if (ok) added++;
                else failed++;
            }

            lastResult = $"Added weapons: {added}, failed: {failed}";
            Debug.Log($"[TestWeaponEquipper] {lastResult}");
        }

        [ContextMenu("Force Unequip Weapon")]
        private void ForceUnequipWeapon()
        {
            WeaponEquipPresenter.Instance?.UnequipWeapon();
            lastResult = "Force unequip requested.";
            Debug.Log($"[TestWeaponEquipper] {lastResult}");
        }
    }
}
