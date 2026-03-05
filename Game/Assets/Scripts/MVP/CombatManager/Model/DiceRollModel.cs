using UnityEngine;

namespace CombatManager.Model
{
    /// <summary>
    /// Data model for individual dice roll instance.
    /// Tracks animation state and roll result.
    /// </summary>
    [System.Serializable]
    public class DiceRollModel
    {
        #region Roll Data

        [Header("Roll Data")]
        public DiceTier diceTier = DiceTier.D6;
        public int rollResult = 0;

        #endregion

        #region Animation State

        [Header("Animation")]
        public float elapsedTime = 0f;
        public float duration = 0.4f;
        public Vector3 startPosition = Vector3.zero;

        #endregion

        #region Constructor

        public DiceRollModel()
        {
            diceTier = DiceTier.D6;
            rollResult = 0;
            elapsedTime = 0f;
            duration = 0.4f;
            startPosition = Vector3.zero;
        }

        #endregion

        #region Helpers

        public float GetNormalizedTime() => Mathf.Clamp01(elapsedTime / duration);
        public bool IsComplete() => elapsedTime >= duration;

        #endregion
    }
}