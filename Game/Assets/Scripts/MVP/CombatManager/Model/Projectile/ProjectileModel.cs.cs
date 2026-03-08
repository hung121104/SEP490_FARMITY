using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for all projectiles.
    /// Renamed from AirSlashProjectileModel → ProjectileModel.
    /// Used by AirSlash skill, Staff normal attack, Staff special skill.
    /// </summary>
    [System.Serializable]
    public class ProjectileModel
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