using UnityEngine;
using System.Collections;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Player Knockback system.
    /// Handles visual effects (wave squash/stretch, red flash) during knockback.
    /// </summary>
    public class PlayerKnockbackView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private PlayerKnockbackPresenter presenter;

        private Coroutine knockbackRoutine;

        #region Public API

        public void StartKnockbackEffects()
        {
            if (presenter == null || !presenter.IsInitialized())
            {
                Debug.LogWarning("[PlayerKnockbackView] Cannot start effects - presenter not initialized");
                return;
            }

            // Stop any existing knockback
            if (knockbackRoutine != null)
            {
                StopCoroutine(knockbackRoutine);
            }

            // Start all visual effects
            knockbackRoutine = StartCoroutine(ApplyKnockback());
            StartCoroutine(WaveEffect());
            StartCoroutine(FlashRed());
        }

        #endregion

        #region Knockback Coroutines

        private IEnumerator ApplyKnockback()
        {
            PlayerMovement playerMovement = presenter.GetPlayerMovement();

            if (playerMovement != null)
                playerMovement.enabled = false;

            yield return new WaitForSeconds(presenter.GetKnockbackDuration());

            if (playerMovement != null)
                playerMovement.enabled = true;
        }

        private IEnumerator WaveEffect()
        {
            Transform playerEntity = presenter.GetPlayerEntity();
            if (playerEntity == null) yield break;

            Vector3 originalScale = presenter.GetOriginalScale();
            float squashPixels = presenter.GetSquashPixels();
            float stretchPixels = presenter.GetStretchPixels();
            float waveDuration = presenter.GetWaveDuration();

            float elapsed = 0f;
            float targetStretch = originalScale.y + stretchPixels;
            float targetSquash = originalScale.y - squashPixels;

            // Stretch
            while (elapsed < waveDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (waveDuration / 2);
                playerEntity.localScale = new Vector3(
                    originalScale.x,
                    Mathf.Lerp(originalScale.y, targetStretch, progress),
                    originalScale.z
                );
                yield return null;
            }

            elapsed = 0f;

            // Squash
            while (elapsed < waveDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (waveDuration / 2);
                playerEntity.localScale = new Vector3(
                    originalScale.x,
                    Mathf.Lerp(targetStretch, targetSquash, progress),
                    originalScale.z
                );
                yield return null;
            }

            elapsed = 0f;

            // Return to normal
            while (elapsed < waveDuration / 4)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (waveDuration / 4);
                playerEntity.localScale = new Vector3(
                    originalScale.x,
                    Mathf.Lerp(targetSquash, originalScale.y, progress),
                    originalScale.z
                );
                yield return null;
            }

            playerEntity.localScale = originalScale;
        }

        private IEnumerator FlashRed()
        {
            SpriteRenderer spriteRenderer = presenter.GetSpriteRenderer();
            if (spriteRenderer == null) yield break;

            Color originalColor = presenter.GetOriginalColor();
            float flashDuration = presenter.GetFlashDuration();
            int flashCount = presenter.GetFlashCount();

            for (int i = 0; i < flashCount; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(flashDuration / 2);

                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(flashDuration / 2);
            }
        }

        #endregion
    }
}