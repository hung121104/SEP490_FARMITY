namespace CombatManager.Service
{
    /// <summary>
    /// Interface for combat mode management service.
    /// Defines operations for toggling combat mode and querying state.
    /// </summary>
    public interface ICombatModeService
    {
        #region Combat Mode Management

        void ToggleCombatMode();
        void SetCombatMode(bool isActive);

        #endregion

        #region Queries

        bool IsCombatModeActive();

        #endregion

        #region Events

        // Event delegate for combat mode changes
        void RegisterOnCombatModeChanged(System.Action<bool> callback);
        void UnregisterOnCombatModeChanged(System.Action<bool> callback);

        #endregion
    }
}