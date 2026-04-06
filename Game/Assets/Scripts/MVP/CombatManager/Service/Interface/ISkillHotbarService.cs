namespace CombatManager.Service
{
    /// <summary>
    /// Interface for SkillHotbar service.
    /// </summary>
    public interface ISkillHotbarService
    {
        void Initialize();
        bool IsInitialized();

        void SetHoveredSlot(int slotIndex);
        void ClearHoveredSlot();
        int GetHoveredSlotIndex();
    }
}