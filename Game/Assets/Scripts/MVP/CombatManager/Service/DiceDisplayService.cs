using UnityEngine;
using CombatManager.Model;
using CombatManager.Presenter;

namespace CombatManager.Service
{
    /// <summary>
    /// Service for dice display management.
    /// Handles spawning roll displays with correct prefabs and settings.
    /// </summary>
    public class DiceDisplayService : IDiceDisplayService
    {
        private DiceDisplayModel model;

        #region Constructor

        public DiceDisplayService(DiceDisplayModel model)
        {
            this.model = model;
        }

        #endregion

        #region Initialization

        public void Initialize(
            GameObject d6, GameObject d8, GameObject d10, GameObject d12, GameObject d20,
            Vector3 offset, float duration, float wobbleScale, float wobbleSpeed)
        {
            model.d6Prefab = d6;
            model.d8Prefab = d8;
            model.d10Prefab = d10;
            model.d12Prefab = d12;
            model.d20Prefab = d20;
            model.rollDisplayOffset = offset;
            model.rollAnimationDuration = duration;
            model.wobbleScale = wobbleScale;
            model.wobbleSpeed = wobbleSpeed;
            model.isInitialized = true;

            Debug.Log("[DiceDisplayService] Initialized");
        }

        public bool IsInitialized()
        {
            return model.isInitialized;
        }

        #endregion

        #region Display Management

        // ✅ FIX: Explicitly use CombatManager.Model.DiceTier
        public GameObject ShowRoll(CombatManager.Model.DiceTier tier, int rollResult, Vector3 position)
        {
            GameObject prefab = GetDicePrefab(tier);
            if (prefab == null)
            {
                Debug.LogError($"[DiceDisplayService] No prefab found for {tier}!");
                return null;
            }

            // Calculate spawn position
            Vector3 spawnPos = position + model.rollDisplayOffset;

            // Instantiate dice display
            GameObject instance = Object.Instantiate(prefab, spawnPos, Quaternion.identity);

            // Setup presenter
            DiceRollPresenter presenter = instance.GetComponent<DiceRollPresenter>();
            if (presenter == null)
            {
                presenter = instance.AddComponent<DiceRollPresenter>();
            }

            // Initialize with roll data
            presenter.Initialize(tier, rollResult, model.rollAnimationDuration, model.wobbleScale, model.wobbleSpeed);

            Debug.Log($"[DiceDisplayService] Spawned {tier} roll: {rollResult} at {spawnPos}");

            return instance;
        }

        // ✅ FIX: Explicitly use CombatManager.Model.DiceTier
        public GameObject GetDicePrefab(CombatManager.Model.DiceTier tier)
        {
            return tier switch
            {
                CombatManager.Model.DiceTier.D6 => model.d6Prefab,
                CombatManager.Model.DiceTier.D8 => model.d8Prefab,
                CombatManager.Model.DiceTier.D10 => model.d10Prefab,
                CombatManager.Model.DiceTier.D12 => model.d12Prefab,
                CombatManager.Model.DiceTier.D20 => model.d20Prefab,
                _ => null
            };
        }

        #endregion

        #region Settings

        public Vector3 GetRollDisplayOffset() => model.rollDisplayOffset;
        public float GetRollAnimationDuration() => model.rollAnimationDuration;
        public float GetWobbleScale() => model.wobbleScale;
        public float GetWobbleSpeed() => model.wobbleSpeed;

        #endregion
    }
}