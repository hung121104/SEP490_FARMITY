using UnityEngine;
using CombatManager.Model;
using CombatManager.View;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for individual dice roll instance.
    /// Manages lifecycle and animation of a single dice display.
    /// </summary>
    public class DiceRollPresenter : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private DiceRollModel model = new DiceRollModel();

        private float wobbleScale = 1.15f;
        private float wobbleSpeed = 10f;

        #region Initialization

        // ✅ FIX: Explicitly specify CombatManager.Model.DiceTier
        public void Initialize(CombatManager.Model.DiceTier tier, int rollResult, float duration, float wobbleScale, float wobbleSpeed)
        {
            model.diceTier = tier; // ← Now the types match!
            model.rollResult = rollResult;
            model.duration = duration;
            model.startPosition = transform.position;
            model.elapsedTime = 0f;

            this.wobbleScale = wobbleScale;
            this.wobbleSpeed = wobbleSpeed;

            // Notify view
            NotifyViewUpdate();

            // Start self-destruct timer
            Destroy(gameObject, duration);

            Debug.Log($"[DiceRollPresenter] Initialized {tier} roll: {rollResult}");
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            model.elapsedTime += Time.deltaTime;
        }

        #endregion

        #region View Notification

        private void NotifyViewUpdate()
        {
            DiceRollView view = GetComponent<DiceRollView>();
            if (view != null)
            {
                view.UpdateDisplay();
            }
        }

        #endregion

        #region Getters for View

        public int GetRollResult() => model.rollResult;
        public float GetNormalizedTime() => model.GetNormalizedTime();
        public float GetWobbleScale() => wobbleScale;
        public float GetWobbleSpeed() => wobbleSpeed;
        public bool IsComplete() => model.IsComplete();

        #endregion
    }
}