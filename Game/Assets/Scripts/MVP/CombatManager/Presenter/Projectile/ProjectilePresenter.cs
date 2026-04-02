using UnityEngine;
using System.Collections.Generic;
using CombatManager.Model;
using CombatManager.Service;
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

            if (IsIgnoredEnemyCollider(other))
                return;

            if (!TryResolveEnemyPresenter(other, out EnemyPresenter enemyPresenter))
                return;

            if (alreadyHit.Contains(other)) return;
            alreadyHit.Add(other);

            if (HitEnemy(other, enemyPresenter))
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
        }

        #endregion

        #region Hit & Destroy

        private bool HitEnemy(Collider2D enemy, EnemyPresenter enemyPresenter)
        {
            if (enemyPresenter != null)
            {
                Vector3 ownerPosition = model.playerTransform != null
                    ? model.playerTransform.position
                    : transform.position - model.direction;

                Vector2 knockbackDir = (enemy.transform.position - ownerPosition).normalized;

                EnemySyncManager.Instance.RequestEnemyHit(
                    enemyPresenter,
                    model.damage,
                    knockbackDir,
                    model.knockbackForce);
                return true;
            }
            return false;
        }

        private static bool TryResolveEnemyPresenter(Collider2D col, out EnemyPresenter enemyPresenter)
        {
            enemyPresenter = null;
            if (col == null)
                return false;

            enemyPresenter = col.GetComponent<EnemyPresenter>()
                             ?? col.GetComponentInParent<EnemyPresenter>()
                             ?? col.GetComponentInChildren<EnemyPresenter>();
            return enemyPresenter != null;
        }

        private static bool IsIgnoredEnemyCollider(Collider2D col)
        {
            if (col == null)
                return true;

            if (col.GetComponent<EnemyAttackHitbox>() != null)
                return true;

            return false;
        }

        private void DestroyProjectile()
        {
            if (model.isDestroyed) return;
            model.isDestroyed = true;
            Destroy(gameObject);
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