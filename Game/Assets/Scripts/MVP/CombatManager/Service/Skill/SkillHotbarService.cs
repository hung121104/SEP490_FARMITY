using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for SkillHotbar.
    /// Pure logic - no MonoBehaviour.
    /// </summary>
    public class SkillHotbarService : ISkillHotbarService
    {
        private SkillHotbarModel model;

        public SkillHotbarService(SkillHotbarModel model)
        {
            this.model = model;
        }

        public void Initialize()
        {
            model.isInitialized = true;
            Debug.Log("[SkillHotbarService] Initialized");
        }

        public bool IsInitialized() => model.isInitialized;

        public void SetHoveredSlot(int slotIndex)
        {
            model.hoveredSlotIndex = slotIndex;
        }

        public void ClearHoveredSlot()
        {
            model.hoveredSlotIndex = -1;
        }

        public int GetHoveredSlotIndex() => model.hoveredSlotIndex;
    }
}