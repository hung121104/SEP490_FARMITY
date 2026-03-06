using UnityEngine;
using CombatManager.SO;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for SkillHotbar.
    /// Mirrors SkillHotbarUI + SkillHotbarSlot from CombatSystem (kept for legacy).
    /// </summary>
    [System.Serializable]
    public class SkillHotbarModel
    {
        [Header("Slot Settings")]
        public int slotCount = 4;
        public KeyCode[] activationKeys = new KeyCode[]
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4
        };

        [Header("Drag Settings")]
        public float unequipDistance = 150f;

        [Header("Visual Settings")]
        public Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public Color occupiedSlotColor = Color.white;
        public Color hoverColor = new Color(1f, 1f, 0.7f, 1f);
        public Color dragColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);

        [Header("State")]
        public bool isInitialized = false;
        public bool isVisible = false;
    }
}