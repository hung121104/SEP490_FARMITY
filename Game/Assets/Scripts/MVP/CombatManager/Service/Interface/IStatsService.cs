namespace CombatManager.Service
{
    /// <summary>
    /// Interface for stats management service.
    /// Defines operations for stat modification, point management, and calculations.
    /// </summary>
    public interface IStatsService
    {
        #region Stat Queries

        int GetStrength();
        int GetVitality();
        int GetLevel();
        int GetCurrentExp();
        int GetExpToNextLevel();
        float GetExpProgress01();

        #endregion

        #region Progression

        int AddExperience(int amount);
        void SetProgressionState(int level, int currentExp, int expToNextLevel);
        void SetBaseStats(int strength, int vitality);

        #endregion

        #region Combat Stats

        int GetAttackDamage();
        int GetMaxHealth();
        int GetCurrentHealth();
        void SetCurrentHealth(int value);
        float GetAttackRange();
        float GetKnockbackForce();
        float GetCooldownTime();
        float GetEaseSpeed();

        #endregion
    }
}