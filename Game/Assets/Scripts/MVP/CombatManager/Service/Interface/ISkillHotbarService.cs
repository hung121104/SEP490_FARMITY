namespace CombatManager.Service
{
    /// <summary>
    /// Interface for SkillHotbar service.
    /// </summary>
    public interface ISkillHotbarService
    {
        void Initialize();
        bool IsInitialized();

        bool CanInteract();
        void SetVisible(bool visible);
        bool IsVisible();
    }
}