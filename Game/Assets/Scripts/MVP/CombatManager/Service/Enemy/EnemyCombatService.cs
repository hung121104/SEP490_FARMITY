using UnityEngine;
using System.Collections.Generic;
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
        private readonly HashSet<int> processedActors = new HashSet<int>();

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

            Transform playerRoot = ResolvePlayerRoot(playerCollider);
            if (playerRoot == null)
                return;

            if (!IsPreferredDamageCollider(playerCollider, playerRoot))
                return;

            Photon.Pun.PhotonView targetView = ResolvePlayerPhotonView(playerCollider, playerRoot);
            ApplyDamageToResolvedTarget(playerRoot, targetView, enemyTransform, ignoreLocalThrottle: false);
        }

        public void DealDamageToPlayers(IReadOnlyList<Collider2D> playerColliders, Transform enemyTransform)
        {
            if (playerColliders == null || enemyTransform == null)
                return;

            processedActors.Clear();

            for (int i = 0; i < playerColliders.Count; i++)
            {
                Collider2D playerCollider = playerColliders[i];
                if (playerCollider == null)
                    continue;

                Transform playerRoot = ResolvePlayerRoot(playerCollider);
                if (playerRoot == null)
                    continue;

                if (!IsPreferredDamageCollider(playerCollider, playerRoot))
                    continue;

                Photon.Pun.PhotonView targetView = ResolvePlayerPhotonView(playerCollider, playerRoot);
                int actorNumber = targetView != null ? targetView.OwnerActorNr : -1;

                if (actorNumber > 0 && !processedActors.Add(actorNumber))
                    continue;

                ApplyDamageToResolvedTarget(playerRoot, targetView, enemyTransform, ignoreLocalThrottle: true);
            }
        }

        private void ApplyDamageToResolvedTarget(
            Transform playerRoot,
            Photon.Pun.PhotonView targetView,
            Transform enemyTransform,
            bool ignoreLocalThrottle)
        {
            if (playerRoot == null || enemyTransform == null)
                return;

            int targetActorNumber = targetView != null ? targetView.OwnerActorNr : -1;

            if (IsDefeatedTarget(targetView))
                return;

            EnemyPresenter enemyPresenter = enemyTransform.GetComponent<EnemyPresenter>();
            if (enemyPresenter == null)
                enemyPresenter = enemyTransform.GetComponentInParent<EnemyPresenter>();

            if (Photon.Pun.PhotonNetwork.IsConnected)
            {
                if (enemyPresenter == null)
                    return;

                if (targetActorNumber <= 0)
                    return;

                EnemySyncManager.Instance.RequestEnemyPlayerTouchDamage(
                    enemyPresenter,
                    targetActorNumber,
                    model.damageAmount,
                    model.knockbackForce,
                    enemyTransform.position);

                return;
            }

            if (!ignoreLocalThrottle && !CanDealDamage())
                return;

            if (targetView != null && !targetView.IsMine)
                return;

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

        private static Photon.Pun.PhotonView ResolvePlayerPhotonView(Collider2D playerCollider, Transform playerRoot)
        {
            if (playerCollider != null)
            {
                Photon.Pun.PhotonView colliderView = playerCollider.GetComponent<Photon.Pun.PhotonView>();
                if (colliderView != null)
                    return colliderView;

                Photon.Pun.PhotonView parentView = playerCollider.GetComponentInParent<Photon.Pun.PhotonView>();
                if (parentView != null)
                    return parentView;
            }

            if (playerRoot != null)
            {
                Photon.Pun.PhotonView rootView = playerRoot.GetComponent<Photon.Pun.PhotonView>();
                if (rootView != null)
                    return rootView;

                Photon.Pun.PhotonView childView = playerRoot.GetComponentInChildren<Photon.Pun.PhotonView>(true);
                if (childView != null)
                    return childView;
            }

            return null;
        }

        private static bool IsPreferredDamageCollider(Collider2D candidate, Transform playerRoot)
        {
            if (candidate == null)
                return false;

            // Prefer explicit body hitbox when present.
            if (candidate is PolygonCollider2D)
                return true;

            if (playerRoot == null)
                return true;

            PolygonCollider2D[] bodyPolygons = playerRoot.GetComponentsInChildren<PolygonCollider2D>(true);
            for (int i = 0; i < bodyPolygons.Length; i++)
            {
                PolygonCollider2D poly = bodyPolygons[i];
                if (poly != null && poly.enabled)
                    return false;
            }

            // Backward compatibility: if no polygon body hitbox exists, keep old collider behavior.
            return true;
        }

        private static bool IsDefeatedTarget(Photon.Pun.PhotonView targetView)
        {
            if (!Photon.Pun.PhotonNetwork.IsConnected || targetView?.Owner == null)
                return false;

            if (!targetView.Owner.CustomProperties.TryGetValue("isDefeated", out object raw))
                return false;

            return raw is bool isDefeated && isDefeated;
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