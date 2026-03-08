using UnityEngine;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Enemy system.
    /// Handles visual updates (animations, sprite flipping, visual feedback).
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        private EnemyPresenter presenter;

        #region Initialization

        public void Initialize(EnemyPresenter presenter)
        {
            this.presenter = presenter;
            Debug.Log($"[EnemyView] Initialized for {gameObject.name}");
        }

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
            if (presenter == null || !presenter.IsInitialized())
                return;

            UpdateVisuals();
        }

        #endregion

        #region Visual Updates

        private void UpdateVisuals()
        {
            UpdateAnimation();
            UpdateSpriteFlip();
        }

        private void UpdateAnimation()
        {
            Animator animator = presenter.GetAnimator();
            if (animator == null)
                return;

            // Animation is already handled by AIService
            // This is a placeholder for additional visual updates if needed
        }

        private void UpdateSpriteFlip()
        {
            SpriteRenderer spriteRenderer = presenter.GetSpriteRenderer();
            if (spriteRenderer == null)
                return;

            // Sprite flipping is already handled by AIService
            // This is a placeholder for additional visual updates if needed
        }

        #endregion
    }
}