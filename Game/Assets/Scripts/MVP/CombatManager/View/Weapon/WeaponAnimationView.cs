using UnityEngine;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Weapon Animation system.
    /// Updates weapon rotation to follow mouse and keeps pivot anchored to player.
    /// </summary>
    public class WeaponAnimationView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private WeaponAnimationPresenter presenter;

        #region Unity Lifecycle

        private void Update()
        {
            if (presenter == null || !presenter.IsInitialized() || !presenter.IsWeaponActive())
                return;

            UpdateWeaponTransform();
        }

        #endregion

        #region Update Logic

        private void UpdateWeaponTransform()
        {
            GameObject pivotRoot = presenter.GetPivotRoot();
            Transform centerPoint = presenter.GetCenterPoint();

            if (pivotRoot == null || centerPoint == null)
                return;

            // Keep pivot anchored to player (handled by parent, but ensure position sync)
            // Note: This is mostly handled by Unity's parenting, but we can ensure it here

            // Rotate pivot to face mouse
            Vector3 mouseDirection = presenter.GetMouseDirection();
            float angle = presenter.GetRotationAngle(mouseDirection);
            pivotRoot.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        #endregion
    }
}