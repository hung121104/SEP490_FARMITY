using UnityEngine;
using TMPro;
using CombatManager.Model;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for enemy combat - dealing damage and knockback to player.
    /// Finds PlayerHealthPresenter in CombatSystem hierarchy (NOT on player prefab).
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

            Debug.Log($"[EnemyCombatService] ========== COLLISION DETECTED ==========");
            Debug.Log($"  - Hit Object: {collision.gameObject.name}");
            Debug.Log($"  - Hit Tag: {collision.gameObject.tag}");
            Debug.Log($"  - Hit Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

            // Find presenters in scene (they're on CombatSystem hierarchy)
            PlayerHealthPresenter healthPresenter = Object.FindObjectOfType<PlayerHealthPresenter>();
            
            if (healthPresenter == null)
            {
                Debug.LogError("[EnemyCombatService] ❌ PlayerHealthPresenter NOT FOUND!");
                Debug.LogError("  Make sure CombatSystem/PlayerHealthManager exists and is active");
                return;
            }

            Debug.Log($"[EnemyCombatService] ✅ Found PlayerHealthPresenter on: {healthPresenter.gameObject.name}");

            PlayerKnockbackPresenter knockbackPresenter = Object.FindObjectOfType<PlayerKnockbackPresenter>();
            
            if (knockbackPresenter == null)
            {
                Debug.LogWarning("[EnemyCombatService] ⚠️ PlayerKnockbackPresenter not found");
            }
            else
            {
                Debug.Log($"[EnemyCombatService] ✅ Found PlayerKnockbackPresenter on: {knockbackPresenter.gameObject.name}");
            }

            // Call presenter's public methods
            Debug.Log($"[EnemyCombatService] Applying {model.damageAmount} damage...");
            healthPresenter.ChangeHealth(-model.damageAmount);
            Debug.Log($"[EnemyCombatService] ✅ Damage applied!");

            // Apply knockback
            if (knockbackPresenter != null)
            {
                Transform attackerTransform = collision.otherCollider.transform;
                Debug.Log($"[EnemyCombatService] Applying knockback from {attackerTransform.name}...");
                knockbackPresenter.Knockback(attackerTransform, model.knockbackForce);
                Debug.Log($"[EnemyCombatService] ✅ Knockback applied!");
            }

            // Show damage popup
            ShowDamagePopup(collision.transform.position);

            Debug.Log($"[EnemyCombatService] ========== DAMAGE COMPLETE ==========");
        }

        public void ShowDamagePopup(Vector3 position)
        {
            // Use centralized manager
            DamagePopupPresenter.Spawn(position, model.damageAmount);
        }
    }
}