using UnityEngine;
using System.Collections.Generic;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for enemy combat (damage dealing to player).
    /// </summary>
    public interface IEnemyCombatService
    {
        void Initialize(GameObject damagePopupPrefab);
        bool CanDealDamage();
        void DealDamageToPlayer(Collider2D playerCollider, Transform enemyTransform);
        void DealDamageToPlayers(IReadOnlyList<Collider2D> playerColliders, Transform enemyTransform);
        void ShowDamagePopup(Vector3 position);
    }
}