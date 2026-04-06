using UnityEngine;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Combat Mode system.
    /// Displays combat mode state in console (UI handled by CombatModeIndicator).
    /// This is a minimal view since the visual indicator is separate.
    /// </summary>
    public class CombatModeView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private CombatModePresenter presenter;

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugLogs = true;

        #region Display Update

        public void UpdateDisplay()
        {
            if (presenter == null)
                return;

            bool isActive = presenter.IsCombatModeActive();

            if (showDebugLogs)
            {
                Debug.Log($"[CombatModeView] Combat Mode: {(isActive ? "ON ⚔️" : "OFF 📝")}");
            }
        }

        #endregion
    }
}