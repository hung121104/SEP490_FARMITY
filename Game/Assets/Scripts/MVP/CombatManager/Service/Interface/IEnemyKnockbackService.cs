using UnityEngine;
using System.Collections;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for enemy knockback visual effects.
    /// </summary>
    public interface IEnemyKnockbackService
    {
        void Initialize(UnityEngine.MonoBehaviour coroutineRunner);
        IEnumerator PlayKnockbackEffect();
        IEnumerator PlayFlashEffect();
        bool IsKnockedBack();
        void UpdateKnockbackTimer(float deltaTime);
    }
}