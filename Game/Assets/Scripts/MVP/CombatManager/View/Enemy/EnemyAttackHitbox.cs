using System.Collections.Generic;
using UnityEngine;

namespace CombatManager.View
{
    /// <summary>
    /// Tracks player colliders currently inside the enemy attack hitbox.
    /// This is sampled only at animation impact time by EnemyPresenter.
    /// </summary>
    public class EnemyAttackHitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private bool allowTagFallback = true;

        private readonly HashSet<Collider2D> overlappingPlayers = new HashSet<Collider2D>();

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;

            if (playerLayer.value == 0)
                playerLayer = LayerMask.GetMask("Player");
        }

        private void OnEnable()
        {
            overlappingPlayers.Clear();
        }

        private void OnDisable()
        {
            overlappingPlayers.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
                overlappingPlayers.Add(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other != null)
                overlappingPlayers.Remove(other);
        }

        public void CollectOverlappingPlayers(List<Collider2D> result)
        {
            if (result == null)
                return;

            result.Clear();

            foreach (Collider2D col in overlappingPlayers)
            {
                if (col == null)
                    continue;

                result.Add(col);
            }
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            if (other == null)
                return false;

            if (((1 << other.gameObject.layer) & playerLayer) != 0)
                return true;

            if (!allowTagFallback)
                return false;

            if (other.CompareTag("Player") || other.CompareTag("PlayerEntity"))
                return true;

            Transform parent = other.transform;
            while (parent != null)
            {
                if (parent.CompareTag("Player") || parent.CompareTag("PlayerEntity"))
                    return true;
                parent = parent.parent;
            }

            return false;
        }
    }
}