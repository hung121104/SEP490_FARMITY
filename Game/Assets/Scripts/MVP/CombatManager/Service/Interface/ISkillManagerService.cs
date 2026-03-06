using CombatManager.Model;
using CombatManager.SO;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for SkillManager service.
    /// Handles equipping, unequipping, querying skills.
    /// </summary>
    public interface ISkillManagerService
    {
        void Initialize();
        bool IsInitialized();

        // Equipment
        void EquipSkill(int slotIndex, SkillData skillData);
        void UnequipSkill(int slotIndex);
        void SwapSkills(int slotA, int slotB);

        // Queries
        SkillData GetSkillData(int slotIndex);
        SkillPresenterBase GetSkillComponent(int slotIndex);
        int GetSlotCount();

        // Validation
        bool IsSlotValid(int slotIndex);
        bool IsSlotEmpty(int slotIndex);
    }
}