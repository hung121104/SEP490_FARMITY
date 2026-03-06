using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Slash Hitbox system.
    /// Connects SlashHitboxModel and SlashHitboxService to SlashHitboxView.
    /// Manages hitbox lifecycle and coordinates damage dealing.
    /// </summary>
    public class SlashHitboxPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private SlashHitboxModel model = new SlashHitboxModel();

        private ISlashHitboxService service;

        #region Initialization (Called by PlayerAttackPresenter)

        public void Initialize(
            int damage,
            float knockbackForce,
            LayerMask enemyLayers,
            Transform ownerTransform,
            GameObject damagePopupPrefab,
            float animationDuration)
        {
            // Get references from this GameObject
            PolygonCollider2D hitCollider = GetComponent<PolygonCollider2D>();
            Animator animator = GetComponent<Animator>();

            if (hitCollider == null)
            {
                Debug.LogError("[SlashHitboxPresenter] PolygonCollider2D not found!");
                return;
            }

            // Initialize service
            service = new SlashHitboxService(model);
            service.Initialize(
                damage,
                knockbackForce,
                enemyLayers,
                ownerTransform,
                damagePopupPrefab,
                animationDuration,
                hitCollider,
                animator
            );

            // Start lifecycle
            StartCoroutine(HitboxLifecycle());

            Debug.Log($"[SlashHitboxPresenter] Initialized with damage: {damage}");
        }

        #endregion

        #region Hitbox Lifecycle

        private IEnumerator HitboxLifecycle()
        {
            if (service == null)
                yield break;

            float elapsed = 0f;
            float duration = service.GetAnimationDuration();

            // Check hits every frame during animation
            while (elapsed < duration)
            {
                service.CheckHits();
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Deactivate and destroy
            service.SetActive(false);
            Destroy(gameObject);

            Debug.Log("[SlashHitboxPresenter] Hitbox destroyed");
        }

        #endregion

        #region Public API

        public bool IsActive() => service?.IsActive() ?? false;

        #endregion
    }
}