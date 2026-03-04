namespace CombatManager.Service
{
    /// <summary>
    /// Interface for stats management service.
    /// Defines operations for stat modification, point management, and calculations.
    /// </summary>
    public interface IStatsService
    {
        #region Point Management

        void AddPoints(int amount);
        bool HasAvailablePoints(int requiredPoints);

        #endregion

        #region Stat Modification (Temporary)

        bool IncreaseTempStrength();
        bool IncreaseTempVitality();
        bool DecreaseTempStrength();
        bool DecreaseTempVitality();

        #endregion

        #region Apply/Cancel Stats

        void ApplyStats();
        void CancelStats();

        #endregion

        #region Stat Queries

        int GetStrength();
        int GetVitality();
        int GetTempStrength();
        int GetTempVitality();
        int GetCurrentPoints();
        int GetPointsSpent();

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