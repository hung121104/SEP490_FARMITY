using UnityEngine;
using TMPro;
using CombatManager.Model;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for enemy combat - dealing damage and knockback to player.
    /// Uses BOTH tags and layers for proper multiplayer detection.
    /// </summary>
    public class EnemyCombatService : IEnemyCombatService
    {
        private EnemyModel model;
        private GameObject damagePopupPrefab;

        public EnemyCombatService(EnemyModel model)
        {
            this.model = model;
        }

        public void Initialize(GameObject damagePopupPrefab)
        {
            this.damagePopupPrefab = damagePopupPrefab;
        }

        public bool CanDealDamage()
        {
            // Check throttle
            if (Time.time - model.lastDamageTime < model.damageThrottleTime)
                return false;

            return true;
        }

        public void DealDamageToPlayer(Collision2D collision)
        {
            model.lastDamageTime = Time.time;

            // Find PlayerHealthPresenter using tags (for multiplayer support)
            PlayerHealthPresenter healthPresenter = FindPlayerPresenter<PlayerHealthPresenter>(collision.gameObject);
            PlayerKnockbackPresenter knockbackPresenter = FindPlayerPresenter<PlayerKnockbackPresenter>(collision.gameObject);

            if (healthPresenter == null)
            {
                Debug.LogWarning("[EnemyCombatService] PlayerHealthPresenter not found");
                return;
            }

            // Apply damage
            healthPresenter.GetService().ChangeHealth(-model.damageAmount);

            // Apply knockback
            if (knockbackPresenter != null)
            {
                // Get the enemy transform (attacker)
                Transform attackerTransform = collision.otherCollider.transform;
                
                // Apply knockback using the correct method signature
                knockbackPresenter.GetService().ApplyKnockback(attackerTransform, model.knockbackForce);
            }

            // Show damage popup at player position
            ShowDamagePopup(collision.transform.position);

            Debug.Log($"[EnemyCombatService] Dealt {model.damageAmount} damage to player");
        }

        /// <summary>
        /// Find presenter on player object hierarchy using tags.
        /// Checks: Player tag → PlayerEntity tag → direct component → parent → children
        /// </summary>
        private T FindPlayerPresenter<T>(GameObject hitObject) where T : Component
        {
            // Check if hit object is Player or PlayerEntity
            if (hitObject.CompareTag("Player") || hitObject.CompareTag("PlayerEntity"))
            {
                // Try direct component
                T presenter = hitObject.GetComponent<T>();
                if (presenter != null) return presenter;

                // Try parent
                presenter = hitObject.GetComponentInParent<T>();
                if (presenter != null) return presenter;

                // Try children
                presenter = hitObject.GetComponentInChildren<T>();
                if (presenter != null) return presenter;
            }

            // Last resort: search in parent hierarchy
            Transform current = hitObject.transform;
            while (current != null)
            {
                if (current.CompareTag("Player") || current.CompareTag("PlayerEntity"))
                {
                    T presenter = current.GetComponent<T>();
                    if (presenter != null) return presenter;

                    presenter = current.GetComponentInChildren<T>();
                    if (presenter != null) return presenter;
                }
                current = current.parent;
            }

            return null;
        }

        public void ShowDamagePopup(Vector3 position)
        {
            if (damagePopupPrefab == null)
                return;

            Vector3 spawnPos = position + Vector3.up;
            GameObject popup = Object.Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);

            TMP_Text damageText = popup.GetComponentInChildren<TMP_Text>();
            if (damageText != null)
            {
                damageText.text = model.damageAmount.ToString();
            }
        }
    }
}