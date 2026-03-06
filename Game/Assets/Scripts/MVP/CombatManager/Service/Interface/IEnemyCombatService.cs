using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for enemy combat (damage dealing to player).
    /// </summary>
    public interface IEnemyCombatService
    {
        void Initialize(GameObject damagePopupPrefab);
        bool CanDealDamage();
        void DealDamageToPlayer(UnityEngine.Collision2D collision);
        void ShowDamagePopup(Vector3 position);
    }
}