using UnityEngine;
using System.Collections;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for enemy knockback visual effects (wave + flash).
    /// </summary>
    public class EnemyKnockbackService : IEnemyKnockbackService
    {
        private EnemyModel model;
        private MonoBehaviour coroutineRunner;

        public EnemyKnockbackService(EnemyModel model)
        {
            this.model = model;
        }

        public void Initialize(MonoBehaviour coroutineRunner)
        {
            this.coroutineRunner = coroutineRunner;
            
            // Store original values
            if (model.spriteRenderer != null)
            {
                model.originalColor = model.spriteRenderer.color;
            }
            
            if (coroutineRunner != null)
            {
                model.originalScale = coroutineRunner.transform.localScale;
            }
        }

        public IEnumerator PlayKnockbackEffect()
        {
            if (coroutineRunner == null)
                yield break;

            float elapsed = 0f;
            float targetStretch = model.originalScale.y + model.stretchPixels;
            float targetSquash = model.originalScale.y - model.squashPixels;

            Transform transform = coroutineRunner.transform;

            // Stretch Y
            while (elapsed < model.waveDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (model.waveDuration / 2);
                transform.localScale = new Vector3(
                    model.originalScale.x,
                    Mathf.Lerp(model.originalScale.y, targetStretch, progress),
                    model.originalScale.z
                );
                yield return null;
            }

            elapsed = 0f;

            // Squash Y
            while (elapsed < model.waveDuration / 2)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (model.waveDuration / 2);
                transform.localScale = new Vector3(
                    model.originalScale.x,
                    Mathf.Lerp(targetStretch, targetSquash, progress),
                    model.originalScale.z
                );
                yield return null;
            }

            elapsed = 0f;

            // Return to normal
            while (elapsed < model.waveDuration / 4)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (model.waveDuration / 4);
                transform.localScale = new Vector3(
                    model.originalScale.x,
                    Mathf.Lerp(targetSquash, model.originalScale.y, progress),
                    model.originalScale.z
                );
                yield return null;
            }

            transform.localScale = model.originalScale;
        }

        public IEnumerator PlayFlashEffect()
        {
            if (model.spriteRenderer == null)
                yield break;

            for (int i = 0; i < model.flashCount; i++)
            {
                model.spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(model.flashDuration / 2);

                model.spriteRenderer.color = model.originalColor;
                yield return new WaitForSeconds(model.flashDuration / 2);
            }
        }

        public bool IsKnockedBack() => model.isKnockedBack;

        public void UpdateKnockbackTimer(float deltaTime)
        {
            if (model.isKnockedBack)
            {
                model.knockbackTimer -= deltaTime;
                if (model.knockbackTimer <= 0f)
                {
                    model.isKnockedBack = false;
                }
            }
        }
    }
}