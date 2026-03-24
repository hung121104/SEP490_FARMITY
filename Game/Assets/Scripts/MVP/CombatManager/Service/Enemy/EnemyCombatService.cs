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
        private PlayerHealthPresenter cachedHealthPresenter;
        private PlayerKnockbackPresenter cachedKnockbackPresenter;

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

            PlayerHealthPresenter healthPresenter = ResolveHealthPresenter();
            if (healthPresenter == null)
            {
                Debug.LogError("[EnemyCombatService] ❌ PlayerHealthPresenter NOT FOUND!");
                Debug.LogError("  Make sure CombatSystem/PlayerHealthManager exists and is active");
                return;
            }

            PlayerKnockbackPresenter knockbackPresenter = ResolveKnockbackPresenter();

            // Call presenter's public methods
            healthPresenter.ChangeHealth(-model.damageAmount);

            // Apply knockback
            if (knockbackPresenter != null)
            {
                Transform attackerTransform = collision.otherCollider.transform;
                knockbackPresenter.Knockback(attackerTransform, model.knockbackForce);
            }

            // Show damage popup
            ShowDamagePopup(collision.transform.position);
        }

        private PlayerHealthPresenter ResolveHealthPresenter()
        {
            if (cachedHealthPresenter != null)
                return cachedHealthPresenter;

            cachedHealthPresenter = Object.FindObjectOfType<PlayerHealthPresenter>();
            return cachedHealthPresenter;
        }

        private PlayerKnockbackPresenter ResolveKnockbackPresenter()
        {
            if (cachedKnockbackPresenter != null)
                return cachedKnockbackPresenter;

            cachedKnockbackPresenter = Object.FindObjectOfType<PlayerKnockbackPresenter>();
            return cachedKnockbackPresenter;
        }

        public void ShowDamagePopup(Vector3 position)
        {
            // Use centralized manager
            DamagePopupPresenter.Spawn(position, model.damageAmount);
        }
    }
}