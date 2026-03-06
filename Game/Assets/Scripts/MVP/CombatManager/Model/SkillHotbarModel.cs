using UnityEngine;
using CombatManager.SO;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for SkillHotbar.
    /// Pure data - no logic.
    /// </summary>
    [System.Serializable]
    public class SkillHotbarModel
    {
        [Header("Settings")]
        public int slotCount = 4;
        public KeyCode[] activationKeys = new KeyCode[]
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4
        };

        [Header("Visual")]
        public Color emptySlotColor = new Color(1f, 1f, 1f, 0.3f);
        public Color occupiedSlotColor = Color.white;
        public Color hoveredSlotColor = new Color(0.8f, 1f, 0.8f, 1f);
        public Color draggingSlotColor = new Color(1f, 1f, 1f, 0.5f);

        [Header("State")]
        public bool isInitialized = false;
        public int hoveredSlotIndex = -1;
    }
}