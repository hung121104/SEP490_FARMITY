using UnityEngine;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Slash Hitbox system.
    /// Minimal view since hitbox logic is in Presenter.
    /// Can be extended for visual effects (glow, particles, etc.)
    /// </summary>
    public class SlashHitboxView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private SlashHitboxPresenter presenter;

        [Header("Visual Effects (Optional)")]
        [SerializeField] private SpriteRenderer slashSprite;
        [SerializeField] private ParticleSystem hitParticles;

        #region Unity Lifecycle

        private void Start()
        {
            if (presenter == null)
            {
                presenter = GetComponent<SlashHitboxPresenter>();
            }

            PlayVisualEffects();
        }

        #endregion

        #region Visual Effects

        private void PlayVisualEffects()
        {
            if (slashSprite != null)
            {
                // Optional: Add slash trail effect
            }

            if (hitParticles != null)
            {
                hitParticles.Play();
            }
        }

        #endregion
    }
}