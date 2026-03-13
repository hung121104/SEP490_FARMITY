using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for all projectiles.
    /// Renamed from AirSlashProjectilePresenter → ProjectilePresenter.
    /// Handles movement + hit detection via PolygonCollider2D trigger.
    /// Used by: AirSlash skill, Staff normal attack, Staff special skill.
    /// </summary>
    public class ProjectilePresenter : MonoBehaviour
    {
        private ProjectileModel model;
        private ProjectileView view;

        private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        #region Unity Lifecycle

        private void Awake()
        {
            view = GetComponent<ProjectileView>();
            if (view == null)
                view = gameObject.AddComponent<ProjectileView>();

            PolygonCollider2D col = GetComponent<PolygonCollider2D>();
            if (col != null)
                col.isTrigger = true;
            else
                Debug.LogWarning("[ProjectilePresenter] PolygonCollider2D missing on prefab!");
        }

        private void Update()
        {
            if (model == null || !model.isInitialized || model.isDestroyed)
                return;

            transform.position += model.direction * model.speed * Time.deltaTime;

            float distanceTravelled = Vector3.Distance(model.spawnPosition, transform.position);
            if (distanceTravelled >= model.maxRange)
            {
                DestroyProjectile();
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (model == null || !model.isInitialized || model.isDestroyed)
                return;

            if ((model.enemyLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            if (alreadyHit.Contains(other)) return;
            alreadyHit.Add(other);

            HitEnemy(other);
            DestroyProjectile();
        }

        #endregion

        #region Initialization

        public void Initialize(ProjectileModel projectileModel)
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

            Debug.Log($"[ProjectilePresenter] Initialized → " +
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

                Debug.Log($"[ProjectilePresenter] Hit: {enemy.name} | Damage: {model.damage}");
                return;
            }

            Debug.LogWarning($"[ProjectilePresenter] Hit {enemy.name} " +
                             $"but no EnemyPresenter found!");
        }

        private void DestroyProjectile()
        {
            if (model.isDestroyed) return;
            model.isDestroyed = true;
            Destroy(gameObject);
            Debug.Log("[ProjectilePresenter] Destroyed");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // PolygonCollider2D shape shown automatically in editor
        }

        #endregion
    }
}