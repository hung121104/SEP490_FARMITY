using UnityEngine;
using System.Collections.Generic;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for slash hitbox management service.
    /// Defines operations for hit detection, damage dealing, and lifecycle.
    /// </summary>
    public interface ISlashHitboxService
    {
        #region Initialization

        void Initialize(
            int damage,
            float knockbackForce,
            LayerMask enemyLayers,
            Transform ownerTransform,
            GameObject damagePopupPrefab,
            float animationDuration,
            PolygonCollider2D hitCollider,
            Animator animator);

        bool IsInitialized();

        #endregion

        #region Hit Detection

        void CheckHits();
        List<Collider2D> GetOverlappingEnemies();

        #endregion

        #region State

        bool IsActive();
        void SetActive(bool active);
        float GetAnimationDuration();

        #endregion
    }
}