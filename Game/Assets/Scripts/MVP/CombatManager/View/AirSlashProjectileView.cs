using UnityEngine;

namespace CombatManager.View
{
    /// <summary>
    /// View for AirSlash projectile.
    /// Handles rotation visual only.
    /// Hit detection is in Presenter.
    /// </summary>
    public class AirSlashProjectileView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>
        /// Rotate sprite to face fire direction.
        /// </summary>
        public void SetDirection(Vector3 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;
        }

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        #endregion
    }
}