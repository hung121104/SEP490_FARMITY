using UnityEngine;

namespace CombatManager.View
{
    /// <summary>
    /// Abstract base view for all skills.
    /// Handles animation stubs for charge, attack, stop.
    /// Each skill can override to add specific VFX.
    /// </summary>
    public abstract class SkillView : MonoBehaviour
    {
        [Header("Animation References")]
        [SerializeField] protected Animator animator;

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        #endregion

        #region Animation - Charge

        public virtual void PlayChargeAnimation()
        {
            // TODO: SPAWN_VFX - Spawn charge VFX prefab here
            // e.g. Instantiate(chargeVFXPrefab, transform.position, Quaternion.identity)
        }

        public virtual void StopChargeAnimation()
        {
            // TODO: SPAWN_VFX - Destroy/hide charge VFX here
        }

        #endregion

        #region Animation - Attack

        public virtual void PlayAttackAnimation()
        {
            // TODO: SPAWN_VFX - Spawn skill attack VFX prefab here
            // e.g. Instantiate(attackVFXPrefab, transform.position, Quaternion.identity)
        }

        public virtual void StopAttackAnimation()
        {
            // TODO: SPAWN_VFX - Destroy/hide attack VFX here
        }

        #endregion

        #region Animation - Stop All

        public virtual void StopAllAnimations()
        {
            StopChargeAnimation();
            StopAttackAnimation();
        }

        #endregion
    }
}