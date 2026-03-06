using UnityEngine;

namespace CombatManager.Service
{
    /// <summary>
    /// Interface for enemy AI state machine and behavior.
    /// </summary>
    public interface IEnemyAIService
    {
        void Initialize(Transform enemyTransform);
        void UpdateBehavior(float deltaTime, float distanceToPlayer);
        void UpdatePhysics(float fixedDeltaTime);
        void OnHit();
        
        // State queries
        Model.EnemyState GetCurrentState();
        bool IsAlerted();
        bool IsKnockedBack();
        
        // Movement
        void ApplyFriction();
        void ClampVelocity();
        void TakeKnockback(Vector2 direction, float force);
        void Stop();
        
        // Detection
        bool CanSeePlayer();
    }
}