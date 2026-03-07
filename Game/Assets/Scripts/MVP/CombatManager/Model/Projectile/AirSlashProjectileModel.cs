using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for AirSlash projectile.
    /// Pure data - no logic.
    /// hitRadius removed - now uses PolygonCollider2D trigger.
    /// </summary>
    [System.Serializable]
    public class AirSlashProjectileModel
    {
        [Header("Movement")]
        public Vector3 direction;
        public float speed;
        public float maxRange;
        public Vector3 spawnPosition;

        [Header("Combat")]
        public int damage;
        public float knockbackForce;
        public LayerMask enemyLayers;

        [Header("Runtime State")]
        public bool isInitialized = false;
        public bool isDestroyed = false;

        [Header("References")]
        public Transform playerTransform;
    }
}