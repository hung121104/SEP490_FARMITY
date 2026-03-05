using UnityEngine;
using TMPro;
using CombatManager.Model;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for enemy combat - dealing damage and knockback to player.
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

            // Check if player health system exists
            PlayerHealthPresenter healthPresenter = Object.FindObjectOfType<PlayerHealthPresenter>();
            if (healthPresenter == null)
                return false;

            return true;
        }

        public void DealDamageToPlayer(Collision2D collision)
        {
            model.lastDamageTime = Time.time;

            // Find player presenters
            PlayerHealthPresenter healthPresenter = Object.FindObjectOfType<PlayerHealthPresenter>();
            PlayerKnockbackPresenter knockbackPresenter = Object.FindObjectOfType<PlayerKnockbackPresenter>();

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
                // Calculate knockback direction (from enemy to player)
                Vector2 knockbackDir = (collision.transform.position - collision.otherCollider.transform.position).normalized;
                
                // Get the enemy transform source (this is the attacker)
                Transform attackerTransform = collision.otherCollider.transform;
                
                // Apply knockback using the correct method signature
                knockbackPresenter.GetService().ApplyKnockback(attackerTransform, model.knockbackForce);
            }

            // Show damage popup at player position
            ShowDamagePopup(collision.transform.position);

            Debug.Log($"[EnemyCombatService] Dealt {model.damageAmount} damage to player");
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