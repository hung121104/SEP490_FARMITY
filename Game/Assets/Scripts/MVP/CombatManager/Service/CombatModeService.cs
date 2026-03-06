using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for combat mode management.
    /// Handles combat mode toggling and state changes with event notifications.
    /// </summary>
    public class CombatModeService : ICombatModeService
    {
        private CombatModeModel model;

        // Event for combat mode changes
        public event System.Action<bool> OnCombatModeChanged;

        #region Constructor

        public CombatModeService(CombatModeModel model)
        {
            this.model = model;
        }

        #endregion

        #region Combat Mode Management

        public void ToggleCombatMode()
        {
            model.isCombatModeActive = !model.isCombatModeActive;
            
            Debug.Log($"[CombatModeService] Combat Mode: {(model.isCombatModeActive ? "ON" : "OFF")}");
            
            // Notify listeners
            OnCombatModeChanged?.Invoke(model.isCombatModeActive);
        }

        public void SetCombatMode(bool isActive)
        {
            if (model.isCombatModeActive == isActive)
                return;

            model.isCombatModeActive = isActive;
            
            Debug.Log($"[CombatModeService] Combat Mode set to: {(model.isCombatModeActive ? "ON" : "OFF")}");
            
            // Notify listeners
            OnCombatModeChanged?.Invoke(model.isCombatModeActive);
        }

        #endregion

        #region Queries

        public bool IsCombatModeActive()
        {
            return model.isCombatModeActive;
        }

        #endregion

        #region Events

        public void RegisterOnCombatModeChanged(System.Action<bool> callback)
        {
            OnCombatModeChanged += callback;
        }

        public void UnregisterOnCombatModeChanged(System.Action<bool> callback)
        {
            OnCombatModeChanged -= callback;
        }

        #endregion
    }
}