using UnityEngine;
using CombatManager.Model;
using CombatManager.Service;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Dice Display Manager.
    /// Centralized system for showing dice rolls.
    /// Skills call this to display roll results.
    /// </summary>
    public class DiceDisplayPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private DiceDisplayModel model = new DiceDisplayModel();

        [Header("Dice Prefabs")]
        [SerializeField] private GameObject d6Prefab;
        [SerializeField] private GameObject d8Prefab;
        [SerializeField] private GameObject d10Prefab;
        [SerializeField] private GameObject d12Prefab;
        [SerializeField] private GameObject d20Prefab;

        [Header("Display Settings")]
        [SerializeField] private Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, 0f);
        [SerializeField] private float rollAnimationDuration = 0.4f;

        [Header("Animation")]
        [SerializeField] private float wobbleScale = 1.15f;
        [SerializeField] private float wobbleSpeed = 10f;

        private IDiceDisplayService service;

        #region Singleton

        private static DiceDisplayPresenter instance;
        public static DiceDisplayPresenter Instance => instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
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
            // Initialize service
            service = new DiceDisplayService(model);
            service.Initialize(
                d6Prefab, d8Prefab, d10Prefab, d12Prefab, d20Prefab,
                rollDisplayOffset, rollAnimationDuration, wobbleScale, wobbleSpeed
            );

            Debug.Log("[DiceDisplayPresenter] Initialized successfully");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Show a dice roll at specified position.
        /// </summary>
        public GameObject ShowRoll(CombatManager.Model.DiceTier tier, int rollResult, Vector3 position)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[DiceDisplayPresenter] Service not initialized!");
                return null;
            }

            return service.ShowRoll(tier, rollResult, position);
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Static helper for easy access.
        /// Usage: DiceDisplayPresenter.Show(DiceTier.D6, 4, playerPos);
        /// </summary>
        public static GameObject Show(CombatManager.Model.DiceTier tier, int rollResult, Vector3 position)
        {
            if (Instance != null)
            {
                return Instance.ShowRoll(tier, rollResult, position);
            }
            return null;
        }

        #endregion

        #region Getters

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public IDiceDisplayService GetService() => service;

        #endregion
    }
}