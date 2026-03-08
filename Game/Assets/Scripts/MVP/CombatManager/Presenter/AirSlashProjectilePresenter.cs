using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.View;
using CombatManager.Presenter;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for AirSlash projectile.
    /// Mirrors AirSlashProjectile from CombatSystem (kept for legacy).
    /// Handles: movement, hit detection, enemy damage + knockback.
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

            CheckHits();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Called by AirSlashPresenter after spawning.
        /// </summary>
        public void Initialize(AirSlashProjectileModel projectileModel)
        {
            model = projectileModel;
            model.spawnPosition = transform.position;
            model.isInitialized = true;
            model.isDestroyed = false;

            // Set visual direction
            view?.SetDirection(model.direction);

            // Lock rigidbody - kinematic only
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            Debug.Log($"[AirSlashProjectile] Initialized → Dir: {model.direction} | Speed: {model.speed} | Range: {model.maxRange} | Dmg: {model.damage}");
        }

        #endregion

        #region Hit Detection

        private void CheckHits()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                model.hitRadius,
                model.enemyLayers
            );

            foreach (Collider2D hit in hits)
            {
                if (alreadyHit.Contains(hit)) continue;

                alreadyHit.Add(hit);
                HitEnemy(hit);
                DestroyProjectile();
                return;
            }
        }

        #endregion

        #region Hit & Destroy

        private void HitEnemy(Collider2D enemy)
        {
            EnemyPresenter enemyPresenter = enemy.GetComponent<EnemyPresenter>();
            if (enemyPresenter != null)
            {
                // ✅ Use model.playerTransform instead of playerTransform
                Vector2 knockbackDir = (enemy.transform.position - model.playerTransform.position).normalized;

                enemyPresenter.TakeDamage(
                    model.damage,
                    knockbackDir,
                    model.knockbackForce
                );

                Debug.Log($"[AirSlashProjectile] Hit: {enemy.name} | Damage: {model.damage}");
                return;
            }

            Debug.LogWarning($"[AirSlashProjectile] Hit {enemy.name} but no EnemyPresenter found!");
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
            if (model == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, model.hitRadius);
        }

        #endregion
    }
}