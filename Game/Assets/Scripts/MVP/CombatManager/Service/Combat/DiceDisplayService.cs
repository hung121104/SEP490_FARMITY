using UnityEngine;
using CombatManager.Model;

namespace CombatManager.Service
{
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

        public void Initialize(DiceDisplayModel model)
        {
            this.model = model;
            ValidatePrefabs();
            model.isInitialized = true;

            Debug.Log("[DiceDisplayService] Initialized");
        }

        public bool IsInitialized()
        {
            return model != null && model.isInitialized;
        }

        #endregion

        #region Dice Display

        public GameObject SpawnDice(CombatManager.Model.DiceTier tier, Transform followTarget)
        {
            GameObject prefab = GetDicePrefab(tier);
            if (prefab == null)
            {
                Debug.LogError($"[DiceDisplayService] No prefab for {tier}!");
                return null;
            }

            Vector3 spawnPos = followTarget != null
                ? followTarget.position + model.rollDisplayOffset
                : Vector3.zero;

            GameObject instance = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            Debug.Log($"[DiceDisplayService] Spawned {tier} dice at {spawnPos}");

            return instance;
        }

        public void DespawnDice(GameObject diceInstance)
        {
            if (diceInstance != null)
            {
                Object.Destroy(diceInstance);
            }
        }

        public GameObject GetDicePrefab(CombatManager.Model.DiceTier tier)
        {
            return model.GetPrefabForTier(tier);
        }

        #endregion

        #region Settings

        public Vector3 GetRollDisplayOffset() => model.rollDisplayOffset;
        public float GetRollAnimationDuration() => model.rollAnimationDuration;
        public float GetWobbleScale() => model.wobbleScale;
        public float GetWobbleSpeed() => model.wobbleSpeed;

        #endregion

        #region Validation

        private void ValidatePrefabs()
        {
            if (model.d6Prefab == null) Debug.LogWarning("[DiceDisplayService] D6 prefab missing!");
            if (model.d8Prefab == null) Debug.LogWarning("[DiceDisplayService] D8 prefab missing!");
            if (model.d10Prefab == null) Debug.LogWarning("[DiceDisplayService] D10 prefab missing!");
            if (model.d12Prefab == null) Debug.LogWarning("[DiceDisplayService] D12 prefab missing!");
            if (model.d20Prefab == null) Debug.LogWarning("[DiceDisplayService] D20 prefab missing!");
        }

        #endregion
    }
}