using UnityEngine;
using CombatManager.Presenter;

namespace CombatManager.Test
{
    /// <summary>
    /// Legacy debug helper kept only to satisfy project file includes.
    /// Runtime weapon activation is now driven by hotbar item selection.
    /// </summary>
    public class TestWeaponEquipper : MonoBehaviour
    {
            [SerializeField] private KeyCode unequipKey = KeyCode.Alpha8;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 360, 80));
            GUILayout.Label("=== WEAPON DEBUG (ITEM-DRIVEN) ===");
            GUILayout.Label("Select a weapon in hotbar to auto-activate it.");
            if (GUILayout.Button($"Force unequip [{unequipKey}]", GUILayout.Height(24)))
            {
                WeaponEquipPresenter.Instance?.UnequipWeapon();
            }
            GUILayout.EndArea();
        }
    }
}
