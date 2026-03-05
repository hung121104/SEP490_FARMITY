using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for dice display manager.
    /// Stores dice prefabs and display settings.
    /// </summary>
    [System.Serializable]
    public class DiceDisplayModel
    {
        #region Prefabs

        [Header("Dice Prefabs")]
        public GameObject d6Prefab = null;
        public GameObject d8Prefab = null;
        public GameObject d10Prefab = null;
        public GameObject d12Prefab = null;
        public GameObject d20Prefab = null;

        #endregion

        #region Display Settings

        [Header("Display Settings")]
        public Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, 0f);
        public float rollAnimationDuration = 0.4f;

        #endregion

        #region Animation Settings

        [Header("Animation")]
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
    }

    /// <summary>
    /// Enum for dice tiers (moved from static class).
    /// </summary>
    public enum DiceTier
    {
        D6 = 6,
        D8 = 8,
        D10 = 10,
        D12 = 12,
        D20 = 20
    }
}