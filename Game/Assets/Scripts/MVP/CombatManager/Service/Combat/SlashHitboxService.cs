using UnityEngine;
using System.Collections.Generic;
using TMPro;
using CombatManager.Model;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    /// <summary>
    /// Service layer for slash hitbox management.
    /// Handles hit detection, damage dealing, and popup spawning.
    /// </summary>
    public class SlashHitboxService : ISlashHitboxService
    {
        private SlashHitboxModel model;

        #region Constructor

        public SlashHitboxService(SlashHitboxModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(
            int damage,
            float knockbackForce,
            LayerMask enemyLayers,
            Transform ownerTransform,
            GameObject damagePopupPrefab,
            float animationDuration,
            PolygonCollider2D hitCollider,
            Animator animator)
        {
            model.damage = damage;
            model.knockbackForce = knockbackForce;
            model.enemyLayers = enemyLayers;
            model.ownerTransform = ownerTransform;
            model.damagePopupPrefab = damagePopupPrefab;
            model.animationDuration = animationDuration;
            model.hitCollider = hitCollider;
            model.animator = animator;
            model.isActive = true;
            model.alreadyHit.Clear();

            Debug.Log($"[SlashHitboxService] Initialized: Damage={damage}, Knockback={knockbackForce}");
        }

        public bool IsInitialized()
        {
            return model.hitCollider != null;
        }

        #endregion

        #region Hit Detection

        public void CheckHits()
        {
            if (!model.isActive || model.hitCollider == null)
                return;

            List<Collider2D> hits = GetOverlappingEnemies();

            foreach (Collider2D hit in hits)
            {
                if (model.alreadyHit.Contains(hit))
                    continue;

                model.alreadyHit.Add(hit);
                DamageEnemy(hit);
            }
        }

        public List<Collider2D> GetOverlappingEnemies()
        {
            List<Collider2D> hits = new List<Collider2D>();

            if (model.hitCollider == null)
                return hits;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(model.enemyLayers);
            filter.useTriggers = true;

            model.hitCollider.Overlap(filter, hits);

            return hits;
        }

        #endregion

        #region Damage Dealing

        private void DamageEnemy(Collider2D enemy)
        {
            // Get EnemyPresenter using tag first, then layer
            EnemyPresenter enemyPresenter = null;
            
            // Try direct component
            enemyPresenter = enemy.GetComponent<EnemyPresenter>();
            
            // Try parent if not found
            if (enemyPresenter == null)
            {
                enemyPresenter = enemy.GetComponentInParent<EnemyPresenter>();
            }
            
            // Try children if still not found
            if (enemyPresenter == null)
            {
                enemyPresenter = enemy.GetComponentInChildren<EnemyPresenter>();
            }

            if (enemyPresenter == null)
            {
                Debug.LogWarning($"[SlashHitboxService] EnemyPresenter not found on {enemy.name}");
                return;
            }

            // Calculate knockback direction (from player to enemy)
            Vector2 knockbackDir = (enemy.transform.position - model.ownerTransform.position).normalized;

            // Deal damage using new MVP system
            enemyPresenter.TakeDamage(model.damage, knockbackDir, model.knockbackForce);

            // Show popup
            ShowDamagePopup(enemy.transform.position);
            
            Debug.Log($"[SlashHitboxService] Dealt {model.damage} damage to {enemy.name}");
        }

        private void ShowDamagePopup(Vector3 position)
        {
            if (model.damagePopupPrefab == null)
                return;

            Vector3 spawnPos = position + Vector3.up * 0.8f;
            GameObject popup = Object.Instantiate(model.damagePopupPrefab, spawnPos, Quaternion.identity);

            TMP_Text text = popup.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = model.damage.ToString();
            }
        }

        #endregion

        #region State

        public bool IsActive() => model.isActive;

        public void SetActive(bool active)
        {
            model.isActive = active;
        }

        public float GetAnimationDuration() => model.animationDuration;

        #endregion
    }
}