using UnityEngine;
using CombatManager.Model;
using CombatManager.SO;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for SkillManager.
    /// Mirrors SkillManager logic from CombatSystem (kept for legacy).
    /// Handles equip/unequip/swap logic only - no MonoBehaviour.
    /// </summary>
    public class SkillManagerService : ISkillManagerService
    {
        private SkillManagerModel model;

        #region Constructor

        public SkillManagerService(SkillManagerModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            model.isInitialized = true;
            Debug.Log("[SkillManagerService] Initialized");
        }

        public bool IsInitialized() => model.isInitialized;

        #endregion

        #region Equipment

        public void EquipSkill(int slotIndex, SkillData skillData)
        {
            if (!IsSlotValid(slotIndex)) return;

            model.equippedSkillsData[slotIndex] = skillData;

            if (skillData != null)
            {
                // Try link component
                string componentName = skillData.linkedComponentName;
                if (model.skillComponentsByName.TryGetValue(componentName, out SkillPresenterBase component))
                {
                    model.equippedSkillsComponents[slotIndex] = component;
                    Debug.Log($"[SkillManagerService] Equipped '{skillData.skillName}' → slot {slotIndex}");
                }
                else
                {
                    model.equippedSkillsComponents[slotIndex] = null;
                    Debug.LogWarning($"[SkillManagerService] Component '{componentName}' not found in scene!");
                }
            }
            else
            {
                model.equippedSkillsComponents[slotIndex] = null;
                Debug.Log($"[SkillManagerService] Cleared slot {slotIndex}");
            }
        }

        public void UnequipSkill(int slotIndex)
        {
            if (!IsSlotValid(slotIndex)) return;

            model.equippedSkillsData[slotIndex] = null;
            model.equippedSkillsComponents[slotIndex] = null;

            Debug.Log($"[SkillManagerService] Unequipped slot {slotIndex}");
        }

        public void SwapSkills(int slotA, int slotB)
        {
            if (!IsSlotValid(slotA) || !IsSlotValid(slotB)) return;

            // Swap data
            SkillData tempData = model.equippedSkillsData[slotA];
            model.equippedSkillsData[slotA] = model.equippedSkillsData[slotB];
            model.equippedSkillsData[slotB] = tempData;

            // Swap components
            SkillPresenterBase tempComponent = model.equippedSkillsComponents[slotA];
            model.equippedSkillsComponents[slotA] = model.equippedSkillsComponents[slotB];
            model.equippedSkillsComponents[slotB] = tempComponent;

            Debug.Log($"[SkillManagerService] Swapped slot {slotA} ↔ slot {slotB}");
        }

        #endregion

        #region Queries

        public SkillData GetSkillData(int slotIndex)
        {
            if (!IsSlotValid(slotIndex)) return null;
            return model.equippedSkillsData[slotIndex];
        }

        public SkillPresenterBase GetSkillComponent(int slotIndex)
        {
            if (!IsSlotValid(slotIndex)) return null;
            return model.equippedSkillsComponents[slotIndex];
        }

        public int GetSlotCount() => model.SlotCount;

        #endregion

        #region Validation

        public bool IsSlotValid(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= model.SlotCount)
            {
                Debug.LogWarning($"[SkillManagerService] Invalid slot: {slotIndex}");
                return false;
            }
            return true;
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            if (!IsSlotValid(slotIndex)) return true;
            return model.equippedSkillsData[slotIndex] == null;
        }

        #endregion
    }
}