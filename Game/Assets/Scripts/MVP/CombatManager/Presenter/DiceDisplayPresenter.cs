using UnityEngine;
using System.Collections;
using CombatManager.Model;
using CombatManager.Service;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for Dice Display Manager.
    /// Centralized system for spawning and managing dice roll displays.
    /// SkillPresenter calls this to show dice rolls above player.
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

        [Header("Spawn Settings")]
        [SerializeField] private Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, 0f);

        [Header("Animation Settings")]
        [SerializeField] private float rollAnimationDuration = 0.4f;
        [SerializeField] private float wobbleScale = 1.15f;
        [SerializeField] private float wobbleSpeed = 10f;

        private IDiceDisplayService service;

        // Active dice instance
        private GameObject currentDiceInstance;
        private RollDisplayPresenter currentRollPresenter;

        #region Singleton

        private static DiceDisplayPresenter instance;
        public static DiceDisplayPresenter Instance => instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
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
            model.d6Prefab = d6Prefab;
            model.d8Prefab = d8Prefab;
            model.d10Prefab = d10Prefab;
            model.d12Prefab = d12Prefab;
            model.d20Prefab = d20Prefab;
            model.rollDisplayOffset = rollDisplayOffset;
            model.rollAnimationDuration = rollAnimationDuration;
            model.wobbleScale = wobbleScale;
            model.wobbleSpeed = wobbleSpeed;

            service = new DiceDisplayService(model);
            service.Initialize(model);

            Debug.Log("[DiceDisplayPresenter] Initialized successfully");
        }

        #endregion

        #region Public API - Roll Display

        /// <summary>
        /// Spawn dice above player and play roll animation.
        /// Called by SkillPresenter during charge state.
        /// </summary>
        public void ShowRoll(int finalValue, CombatManager.Model.DiceTier tier, Transform playerTransform)
        {
            if (service == null || !service.IsInitialized())
            {
                Debug.LogWarning("[DiceDisplayPresenter] Service not initialized!");
                return;
            }

            // Destroy previous dice if exists
            HideRoll();

            // Spawn new dice at player position
            currentDiceInstance = service.SpawnDice(tier, playerTransform);
            if (currentDiceInstance == null)
            {
                Debug.LogError("[DiceDisplayPresenter] Failed to spawn dice!");
                return;
            }

            // Get or add RollDisplayPresenter on the dice prefab
            currentRollPresenter = currentDiceInstance.GetComponent<RollDisplayPresenter>();
            if (currentRollPresenter == null)
            {
                currentRollPresenter = currentDiceInstance.AddComponent<RollDisplayPresenter>();
            }

            // Initialize follow behavior
            currentRollPresenter.Initialize(
                playerTransform,
                service.GetRollDisplayOffset()
            );

            // Play roll animation
            currentRollPresenter.PlayRoll(
                finalValue,
                tier,
                service.GetRollAnimationDuration()
            );

            Debug.Log($"[DiceDisplayPresenter] Showing roll: {finalValue} ({tier}) above {playerTransform.name}");
        }

        /// <summary>
        /// Hide and destroy current dice display.
        /// Called after player confirms/cancels skill.
        /// </summary>
        public void HideRoll()
        {
            if (currentDiceInstance != null)
            {
                service?.DespawnDice(currentDiceInstance);
                currentDiceInstance = null;
                currentRollPresenter = null;
            }
        }

        #endregion

        #region Public API - Settings

        public Vector3 GetRollDisplayOffset()
        {
            return service?.GetRollDisplayOffset() ?? new Vector3(0f, 1.8f, 0f);
        }

        public float GetRollAnimationDuration()
        {
            return service?.GetRollAnimationDuration() ?? 0.4f;
        }

        public GameObject GetDicePrefab(CombatManager.Model.DiceTier tier)
        {
            return service?.GetDicePrefab(tier);
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Static helper for easy access from SkillPresenter.
        /// Usage: DiceDisplayPresenter.Show(roll, DiceTier.D6, playerTransform);
        /// </summary>
        public static void Show(int finalValue, CombatManager.Model.DiceTier tier, Transform playerTransform)
        {
            if (Instance != null)
            {
                Instance.ShowRoll(finalValue, tier, playerTransform);
            }
            else
            {
                Debug.LogWarning("[DiceDisplayPresenter] Instance not found!");
            }
        }

        public static void Hide()
        {
            Instance?.HideRoll();
        }

        #endregion

        #region Getters

        public bool IsInitialized() => service?.IsInitialized() ?? false;
        public IDiceDisplayService GetService() => service;
        public bool IsRolling() => currentRollPresenter?.IsRolling() ?? false;
        public int GetLastRollValue() => currentRollPresenter?.GetFinalValue() ?? 0;

        #endregion
    }
}