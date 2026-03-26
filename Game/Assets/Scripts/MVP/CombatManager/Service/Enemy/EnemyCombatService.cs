using UnityEngine;
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
        private PlayerHealthPresenter cachedHealthPresenter;
        private PlayerKnockbackPresenter cachedKnockbackPresenter;

        public EnemyCombatService(EnemyModel model)
        {
            this.model = model;
        }

        public void Initialize(GameObject damagePopupPrefab)
        {
            // Kept for interface compatibility; popup spawning is centralized via DamagePopupPresenter.
        }

        public bool CanDealDamage()
        {
            // Check throttle
            if (Time.time - model.lastDamageTime < model.damageThrottleTime)
                return false;

            return true;
        }

        public void DealDamageToPlayer(Collider2D playerCollider, Transform enemyTransform)
        {
            if (playerCollider == null || enemyTransform == null)
                return;

            if (!CanDealDamage())
                return;

            Transform playerRoot = ResolvePlayerRoot(playerCollider);
            if (playerRoot == null)
                return;

            Photon.Pun.PhotonView targetView = playerRoot.GetComponent<Photon.Pun.PhotonView>();
            if (targetView != null && Photon.Pun.PhotonNetwork.IsConnected && !targetView.IsMine)
                return; // Phase 1: host-local-only damage application.

            PlayerHealthPresenter healthPresenter = ResolveHealthPresenter();
            if (healthPresenter == null)
            {
                Debug.LogError("[EnemyCombatService] ❌ PlayerHealthPresenter NOT FOUND!");
                Debug.LogError("  Make sure CombatSystem/PlayerHealthManager exists and is active");
                return;
            }

            Transform localPlayerRoot = healthPresenter.GetService()?.GetPlayerEntity();
            if (localPlayerRoot == null || localPlayerRoot != playerRoot)
                return;

            PlayerKnockbackPresenter knockbackPresenter = ResolveKnockbackPresenter();

            // Call presenter's public methods
            healthPresenter.ChangeHealth(-model.damageAmount);
            model.lastDamageTime = Time.time;

            // Apply knockback
            if (knockbackPresenter != null)
            {
                knockbackPresenter.Knockback(enemyTransform, model.knockbackForce);
            }

            // Show damage popup
            ShowDamagePopup(playerRoot.position);
        }

        private static Transform ResolvePlayerRoot(Collider2D playerCollider)
        {
            if (playerCollider == null)
                return null;

            if (playerCollider.GetComponent<Photon.Pun.PhotonView>() != null)
                return playerCollider.transform;

            Photon.Pun.PhotonView parentView = playerCollider.GetComponentInParent<Photon.Pun.PhotonView>();
            if (parentView != null)
                return parentView.transform;

            if (playerCollider.CompareTag("Player") || playerCollider.CompareTag("PlayerEntity"))
                return playerCollider.transform;

            Transform taggedParent = playerCollider.transform;
            while (taggedParent != null)
            {
                if (taggedParent.CompareTag("Player") || taggedParent.CompareTag("PlayerEntity"))
                    return taggedParent;
                taggedParent = taggedParent.parent;
            }

            return null;
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