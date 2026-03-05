using UnityEngine;
using TMPro;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for individual dice roll display.
    /// Handles animation (wobble, scale, fade).
    /// </summary>
    public class DiceRollView : MonoBehaviour
    {
        [Header("Presenter")]
        [SerializeField] private DiceRollPresenter presenter;

        private TMP_Text damageText;
        private Vector3 originalScale;

        #region Unity Lifecycle

        private void Awake()
        {
            if (presenter == null)
            {
                presenter = GetComponent<DiceRollPresenter>();
            }

            damageText = GetComponentInChildren<TMP_Text>();
            originalScale = transform.localScale;
        }

        private void Update()
        {
            if (presenter == null)
                return;

            AnimateDice();
        }

        #endregion

        #region Display Update

        public void UpdateDisplay()
        {
            if (presenter == null)
                return;

            // Set roll result text
            if (damageText != null)
            {
                damageText.text = presenter.GetRollResult().ToString();
            }
        }

        #endregion

        #region Animation

        private void AnimateDice()
        {
            float t = presenter.GetNormalizedTime();

            // Wobble effect
            float wobble = Mathf.Sin(Time.time * presenter.GetWobbleSpeed());
            float scale = 1f + (wobble * 0.1f * (1f - t)); // Reduce wobble over time
            transform.localScale = originalScale * scale * presenter.GetWobbleScale();

            // Fade out near end
            if (damageText != null && t >= 0.7f)
            {
                float fadeT = (t - 0.7f) / 0.3f;
                Color c = damageText.color;
                c.a = Mathf.Lerp(1f, 0f, fadeT);
                damageText.color = c;
            }
        }

        #endregion
    }
}