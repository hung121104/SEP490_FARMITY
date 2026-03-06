using UnityEngine;
using System.Collections.Generic;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for dice display manager.
    /// Stores dice prefabs, spawn settings, and animation settings. aaa
    /// </summary>
    [System.Serializable]
    public class DiceDisplayModel
    {
        #region Dice Prefabs

        [Header("Dice Prefabs")]
        public GameObject d6Prefab = null;
        public GameObject d8Prefab = null;
        public GameObject d10Prefab = null;
        public GameObject d12Prefab = null;
        public GameObject d20Prefab = null;

        #endregion

        #region Spawn Settings

        [Header("Spawn Settings")]
        public Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, 0f);

        #endregion

        #region Animation Settings

        [Header("Animation Settings")]
        public float rollAnimationDuration = 0.4f;
        public float wobbleScale = 1.15f;
        public float wobbleSpeed = 10f;

        #endregion

        #region Initialization

        [Header("Initialization")]
        public bool isInitialized = false;

        #endregion

        #region Constructor

        public DiceDisplayModel()
        {
            rollDisplayOffset = new Vector3(0f, 1.8f, 0f);
            rollAnimationDuration = 0.4f;
            wobbleScale = 1.15f;
            wobbleSpeed = 10f;
            isInitialized = false;
        }

        #endregion

        #region Helpers

        public GameObject GetPrefabForTier(DiceTier tier)
        {
            return tier switch
            {
                DiceTier.D6 => d6Prefab,
                DiceTier.D8 => d8Prefab,
                DiceTier.D10 => d10Prefab,
                DiceTier.D12 => d12Prefab,
                DiceTier.D20 => d20Prefab,
                _ => null
            };
        }

        #endregion
    }
}