namespace CombatManager.Service
{
    /// <summary>
    /// Interface for enemy health management.
    /// </summary>
    public interface IEnemyHealthService
    {
        void Initialize(int maxHealth);
        void ChangeHealth(int amount);
        int GetCurrentHealth();
        int GetMaxHealth();
        bool IsDead();
        void ResetHealth();
    }
}