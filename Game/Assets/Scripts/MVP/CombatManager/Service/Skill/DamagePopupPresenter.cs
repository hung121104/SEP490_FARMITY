using UnityEngine;
using CombatManager.Model;
using CombatManager.Service;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Damage Popup Manager.
    /// Centralized system for spawning all damage popups in the game.
    /// Other systems call this to show damage numbers.
    /// </summary>
    public class DamagePopupPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private DamagePopupModel model = new DamagePopupModel();

        [Header("Prefab")]
        [SerializeField] private GameObject popupPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.8f, 0f);
        [SerializeField] private float randomOffsetX = 0.2f;
        [SerializeField] private float randomOffsetY = 0.1f;

        private IDamagePopupService service;

        #region Singleton (Optional - for easy access)

        private static DamagePopupPresenter instance;
        public static DamagePopupPresenter Instance => instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup (optional)
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            InitializeService();
        }

        #endregion

        #region Initialization

        private void InitializeService()
        {
            // Sync inspector values to model
            model.popupPrefab = popupPrefab;
            model.spawnOffset = spawnOffset;
            model.randomOffsetX = randomOffsetX;
            model.randomOffsetY = randomOffsetY;

            // Initialize service
            service = new DamagePopupService(model);
            service.Initialize(popupPrefab);

            Debug.Log("[DamagePopupPresenter] Initialized successfully");
        }

        #endregion

        #region Public API for Other Systems

        /// <summary>
        /// Spawn a damage popup with default styling.
        /// </summary>
        public void SpawnDamagePopup(Vector3 position, int damage)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[DamagePopupPresenter] Service not initialized");
                return;
            }

            service.SpawnPopup(position, damage);
        }

        /// <summary>
        /// Spawn a damage popup with specific type (crit, heal, miss).
        /// </summary>
        public void SpawnDamagePopup(Vector3 position, int damage, PopupType type)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[DamagePopupPresenter] Service not initialized");
                return;
            }

            service.SpawnPopup(position, damage, type);
        }

        /// <summary>
        /// Spawn a custom text popup (e.g., "BLOCKED", "EVADED").
        /// </summary>
        public void SpawnCustomPopup(Vector3 position, string text, PopupType type)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[DamagePopupPresenter] Service not initialized");
                return;
            }

            service.SpawnPopup(position, text, type);
        }

        #endregion

        #region Static Helpers (For Easy Access)

        /// <summary>
        /// Static helper for easy access from anywhere.
        /// Usage: DamagePopupPresenter.Spawn(pos, 10);
        /// </summary>
        public static void Spawn(Vector3 position, int damage)
        {
            if (Instance != null)
            {
                Instance.SpawnDamagePopup(position, damage);
            }
        }

        public static void Spawn(Vector3 position, int damage, PopupType type)
        {
            if (Instance != null)
            {
                Instance.SpawnDamagePopup(position, damage, type);
            }
        }

        public static void SpawnText(Vector3 position, string text, PopupType type)
        {
            if (Instance != null)
            {
                Instance.SpawnCustomPopup(position, text, type);
            }
        }

        #endregion

        #region Getters for View

        public bool IsInitialized() => service?.IsInitialized() ?? false;

        #endregion

        #region Public API for Other Systems

        public IDamagePopupService GetService() => service;

        #endregion
    }
}