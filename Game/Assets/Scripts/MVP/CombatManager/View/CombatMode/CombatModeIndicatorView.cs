using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for Combat Mode Indicator system.
    /// Handles animated fade transitions between combat/normal mode icons.
    /// </summary>
    public class CombatModeIndicatorView : MonoBehaviour
    {
        [Header("Presenter Reference")]
        [SerializeField] private CombatModeIndicatorPresenter presenter;

        private Coroutine fadeRoutine;

        #region Public API

        public void StartFadeAnimation(bool isCombatMode)
        {
            if (presenter == null || !presenter.IsInitialized())
            {
                Debug.LogWarning("[CombatModeIndicatorView] Cannot animate - presenter not initialized");
                return;
            }

            // Stop any existing fade
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            // Start new fade animation
            fadeRoutine = StartCoroutine(FadeSwitchCoroutine(isCombatMode));
        }

        #endregion

        #region Fade Animation

        private IEnumerator FadeSwitchCoroutine(bool isCombatMode)
        {
            Image combatModeIcon = presenter.GetCombatModeIcon();
            Image normalModeIcon = presenter.GetNormalModeIcon();
            float fadeDuration = presenter.GetFadeDuration();
            AnimationCurve fadeCurve = presenter.GetFadeCurve();

            float elapsed = 0f;

            // Fade animation
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = fadeCurve.Evaluate(elapsed / fadeDuration);

                if (combatModeIcon != null)
                {
                    Color combatColor = combatModeIcon.color;
                    combatColor.a = isCombatMode ? progress : 1f - progress;
                    combatModeIcon.color = combatColor;
                }

                if (normalModeIcon != null)
                {
                    Color normalColor = normalModeIcon.color;
                    normalColor.a = isCombatMode ? 1f - progress : progress;
                    normalModeIcon.color = normalColor;
                }

                yield return null;
            }

            // Ensure final state
            if (combatModeIcon != null)
            {
                Color combatColor = combatModeIcon.color;
                combatColor.a = isCombatMode ? 1f : 0f;
                combatModeIcon.color = combatColor;
            }

            if (normalModeIcon != null)
            {
                Color normalColor = normalModeIcon.color;
                normalColor.a = isCombatMode ? 0f : 1f;
                normalModeIcon.color = normalColor;
            }

            fadeRoutine = null;
        }

        #endregion
    }
}