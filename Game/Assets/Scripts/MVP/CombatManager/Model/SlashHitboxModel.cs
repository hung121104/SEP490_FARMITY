using UnityEngine;
using System.Collections.Generic;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for slash hitbox state.
    /// Tracks damage, knockback, hit targets, and VFX lifecycle.
    /// </summary>
    [System.Serializable]
    public class SlashHitboxModel
    {
        #region Attack Properties

        [Header("Attack Properties")]
        public int damage = 0;
        public float knockbackForce = 0f;
        public LayerMask enemyLayers;
        public Transform ownerTransform = null;

        #endregion

        #region VFX References

        [Header("VFX References")]
        public GameObject damagePopupPrefab = null;
        public PolygonCollider2D hitCollider = null;
        public Animator animator = null;

        #endregion

        #region State

        [Header("State")]
        public HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();
        public bool isActive = false;
        public float animationDuration = 0.3f;

        #endregion

        #region Constructor

        public SlashHitboxModel()
        {
            damage = 0;
            knockbackForce = 0f;
            ownerTransform = null;
            damagePopupPrefab = null;
            hitCollider = null;
            animator = null;
            alreadyHit = new HashSet<Collider2D>();
            isActive = false;
            animationDuration = 0.3f;
        }

        #endregion
    }
}