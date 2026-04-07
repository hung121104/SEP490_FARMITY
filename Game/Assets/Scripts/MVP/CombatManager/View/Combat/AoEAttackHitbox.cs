using System.Collections.Generic;
using UnityEngine;

namespace CombatManager.View
{
    /// <summary>
    /// AoE hitbox sampler used at animation impact timing.
    /// Uses this GameObject's collider overlap query to collect enemies currently inside the AoE shape.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class AoEAttackHitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private bool useTriggers = true;

        private Collider2D hitboxCollider;

        private void Awake()
        {
            hitboxCollider = GetComponent<Collider2D>();
        }

        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        public void CollectOverlappingEnemies(List<Collider2D> result)
        {
            if (result == null)
                return;

            result.Clear();

            if (hitboxCollider == null)
                return;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayer);
            filter.useTriggers = useTriggers;

            hitboxCollider.Overlap(filter, result);
        }
    }
}
