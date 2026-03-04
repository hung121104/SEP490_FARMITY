using UnityEngine;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Player Pointer system.
    /// Updates pointer position and rotation based on mouse direction.
    /// </summary>
    public class PlayerPointerView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private PlayerPointerPresenter presenter;

        #region Unity Lifecycle

        private void Update()
        {
            if (presenter == null || !presenter.IsInitialized())
                return;

            UpdatePointerState();
        }

        #endregion

        #region Update Logic

        private void UpdatePointerState()
        {
            // Update direction from mouse input
            presenter.UpdateDirection();

            // Update pointer position and rotation
            UpdatePointerTransform();
        }

        private void UpdatePointerTransform()
        {
            Transform pointerTransform = presenter.GetPointerTransform();
            if (pointerTransform == null)
                return;

            // Update local position (orbiting around center)
            pointerTransform.localPosition = presenter.GetPointerLocalPosition();

            // Update rotation to face direction
            pointerTransform.rotation = presenter.GetPointerRotation();
        }

        #endregion
    }
}