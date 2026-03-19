using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for player knockback management service.
    /// Defines operations for knockback physics and visual effects.
    /// </summary>
    public interface IPlayerKnockbackService
    {
        #region Initialization

        void Initialize(Transform playerEntity);
        bool IsInitialized();

        #endregion

        #region Knockback

        void ApplyKnockback(Transform enemyTransform, float knockbackForce);
        bool IsKnockbackActive();

        #endregion

        #region Getters

        Transform GetPlayerEntity();
        Rigidbody2D GetRigidbody();
        SpriteRenderer GetSpriteRenderer();
        PlayerMovement GetPlayerMovement();
        Color GetOriginalColor();
        Vector3 GetOriginalScale();

        #endregion

        #region Settings Getters

        float GetKnockbackDuration();
        float GetSquashPixels();
        float GetStretchPixels();
        float GetWaveDuration();
        float GetFlashDuration();
        int GetFlashCount();

        #endregion

        #region State Management

        void SetKnockbackActive(bool active);

        #endregion
    }
}