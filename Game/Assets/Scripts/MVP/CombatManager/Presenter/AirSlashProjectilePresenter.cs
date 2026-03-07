using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for AirSlash projectile.
    /// Movement + hit detection via PolygonCollider2D trigger.
    /// Sits on AirSlashProjectile prefab.
    /// </summary>
    public class AirSlashProjectilePresenter : MonoBehaviour
    {
        private AirSlashProjectileModel model;
        private AirSlashProjectileView view;

        private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        #region Unity Lifecycle

        private void Awake()
        {
            view = GetComponent<AirSlashProjectileView>();
            if (view == null)
                view = gameObject.AddComponent<AirSlashProjectileView>();

            // ✅ Ensure PolygonCollider2D is trigger
            PolygonCollider2D col = GetComponent<PolygonCollider2D>();
            if (col != null)
                col.isTrigger = true;
            else
                Debug.LogWarning("[AirSlashProjectile] PolygonCollider2D missing on prefab!");
        }

        private void Update()
        {
            if (model == null || !model.isInitialized || model.isDestroyed)
                return;

            // Move forward
            transform.position += model.direction * model.speed * Time.deltaTime;

            // Check max range
            float distanceTravelled = Vector3.Distance(model.spawnPosition, transform.position);
            if (distanceTravelled >= model.maxRange)
            {
                DestroyProjectile();
                return;
            }
        }

        // ✅ NEW: Trigger instead of OverlapCircleAll
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (model == null || !model.isInitialized || model.isDestroyed)
                return;

            // Check enemy layer
            if ((model.enemyLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            // Prevent double hit
            if (alreadyHit.Contains(other)) return;
            alreadyHit.Add(other);

            HitEnemy(other);
            DestroyProjectile();
        }

        #endregion

        #region Initialization

        public void Initialize(AirSlashProjectileModel projectileModel)
        {
            model = projectileModel;
            model.spawnPosition = transform.position;
            model.isInitialized = true;
            model.isDestroyed = false;

            view?.SetDirection(model.direction);

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            Debug.Log($"[AirSlashProjectile] Initialized → " +
                      $"Dir: {model.direction} | " +
                      $"Speed: {model.speed} | " +
                      $"Range: {model.maxRange} | " +
                      $"Dmg: {model.damage}");
        }

        #endregion

        #region Hit & Destroy

        private void HitEnemy(Collider2D enemy)
        {
            EnemyPresenter enemyPresenter = enemy.GetComponent<EnemyPresenter>();
            if (enemyPresenter != null)
            {
                Vector2 knockbackDir = (enemy.transform.position
                                       - model.playerTransform.position).normalized;

                enemyPresenter.TakeDamage(
                    model.damage,
                    knockbackDir,
                    model.knockbackForce
                );

                Debug.Log($"[AirSlashProjectile] Hit: {enemy.name} | Damage: {model.damage}");
                return;
            }

            Debug.LogWarning($"[AirSlashProjectile] Hit {enemy.name} " +
                             $"but no EnemyPresenter found!");
        }

        private void DestroyProjectile()
        {
            if (model.isDestroyed) return;
            model.isDestroyed = true;
            Destroy(gameObject);
            Debug.Log("[AirSlashProjectile] Destroyed");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // ✅ Gizmo now shows PolygonCollider2D shape automatically
            // No manual gizmo needed - Unity shows polygon in editor
        }

        #endregion
    }
}