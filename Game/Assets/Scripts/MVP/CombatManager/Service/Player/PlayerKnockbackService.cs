using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for player knockback management.
    /// Handles knockback physics calculations and state management.
    /// </summary>
    public class PlayerKnockbackService : IPlayerKnockbackService
    {
        private PlayerKnockbackModel model;

        #region Constructor

        public PlayerKnockbackService(PlayerKnockbackModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(Transform playerEntity)
        {
            model.playerEntity = playerEntity;
            model.rigidbody = playerEntity.GetComponent<Rigidbody2D>();
            model.spriteRenderer = playerEntity.GetComponent<SpriteRenderer>();
            model.playerMovement = playerEntity.GetComponent<PlayerMovement>();
            model.originalScale = playerEntity.localScale;

            if (model.spriteRenderer != null)
            {
                model.originalColor = model.spriteRenderer.color;
            }

            model.isInitialized = true;
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Knockback

        public void ApplyKnockback(Transform enemyTransform, float knockbackForce)
        {
            if (enemyTransform == null)
            {
                Debug.LogWarning("[PlayerKnockbackService] Enemy transform missing, cannot apply knockback");
                return;
            }

            ApplyKnockbackFromPosition(enemyTransform.position, knockbackForce);
        }

        public void ApplyKnockbackFromPosition(Vector2 sourcePosition, float knockbackForce)
        {
            if (!model.isInitialized || model.playerEntity == null)
            {
                Debug.LogWarning("[PlayerKnockbackService] Not initialized, cannot apply knockback");
                return;
            }

            // Calculate knockback direction
            Vector2 direction = ((Vector2)model.playerEntity.position - sourcePosition).normalized;
            Vector2 velocity = direction * knockbackForce;

            // Set knockback active flag (used by Presenter to trigger coroutines)
            model.isKnockbackActive = true;

            // Apply velocity to rigidbody
            if (model.rigidbody != null)
            {
                model.rigidbody.linearVelocity = velocity;
            }
        }

        public bool IsKnockbackActive()
        {
            return model.isKnockbackActive;
        }

        #endregion

        #region Getters

        public Transform GetPlayerEntity() => model.playerEntity;
        public Rigidbody2D GetRigidbody() => model.rigidbody;
        public SpriteRenderer GetSpriteRenderer() => model.spriteRenderer;
        public PlayerMovement GetPlayerMovement() => model.playerMovement;
        public Color GetOriginalColor() => model.originalColor;
        public Vector3 GetOriginalScale() => model.originalScale;

        #endregion

        #region Settings Getters

        public float GetKnockbackDuration() => model.knockbackDuration;
        public float GetSquashPixels() => model.squashPixels;
        public float GetStretchPixels() => model.stretchPixels;
        public float GetWaveDuration() => model.waveDuration;
        public float GetFlashDuration() => model.flashDuration;
        public int GetFlashCount() => model.flashCount;

        #endregion

        #region State Management

        public void SetKnockbackActive(bool active)
        {
            model.isKnockbackActive = active;
        }

        #endregion
    }
}