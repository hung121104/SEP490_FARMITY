using UnityEngine;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Player Attack system.
    /// Displays attack UI feedback (cooldown indicator, combo counter, etc.)
    /// </summary>
    public class PlayerAttackView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private PlayerAttackPresenter presenter;

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = true;

        #region Unity Lifecycle

        private void Update()
        {
            if (presenter == null || !presenter.IsInitialized())
                return;

            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        #endregion

        #region Debug Display

        private void DisplayDebugInfo()
        {
            // Optional: Display combo step and cooldown in UI
            // For now, just log occasionally
            if (Input.GetMouseButtonDown(0))
            {
                int comboStep = presenter.GetCurrentComboStep();
                bool canAttack = presenter.CanAttack();
                float cooldown = presenter.GetCooldownPercent();

                Debug.Log($"[PlayerAttackView] Combo: {comboStep}, CanAttack: {canAttack}, Cooldown: {cooldown:P0}");
            }
        }

        #endregion
    }
}