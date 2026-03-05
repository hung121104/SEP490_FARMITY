using UnityEngine;
using TMPro;
using System.Collections;
using CombatManager.Model;

namespace CombatManager.View
{
    /// <summary>
    /// View for individual dice roll display.
    /// Handles text update, show/hide, popup and disappear animations.
    /// Self-contained - no dependency on old PopupDamage script.
    /// </summary>
    public class RollDisplayView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI numberText;

        [Header("Popup Animation")]
        [SerializeField] private Vector3 startScale = new Vector3(0f, 0f, 1f);
        [SerializeField] private Vector3 peakScale = new Vector3(1.2f, 1.2f, 1f);
        [SerializeField] private Vector3 normalScale = new Vector3(1f, 1f, 1f);
        [SerializeField] private float popupDuration = 0.25f;
        [SerializeField] private AnimationCurve popupCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Idle Wobble")]
        [SerializeField] private float wobbleAmount = 0.05f;
        [SerializeField] private float wobbleSpeed = 3f;

        [Header("Disappear Animation")]
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private Vector3 disappearMoveOffset = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine popupCoroutine;
        private Coroutine disappearCoroutine;
        private Coroutine wobbleCoroutine;

        private Vector3 baseLocalPosition;
        private bool isVisible = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // Auto-find text if not assigned
            if (numberText == null)
                numberText = GetComponentInChildren<TextMeshProUGUI>();

            // Start hidden
            transform.localScale = Vector3.zero;
            baseLocalPosition = transform.localPosition;
        }

        #endregion

        #region Display Update

        public void UpdateDisplay(int value)
        {
            if (numberText == null) return;
            numberText.text = value.ToString();
        }

        public void UpdateDisplay(int value, Color color)
        {
            if (numberText == null) return;
            numberText.text = value.ToString();
            numberText.color = color;
        }

        #endregion

        #region Visibility

        public void Show()
        {
            gameObject.SetActive(true);
            isVisible = true;

            // Reset alpha
            SetAlpha(1f);

            // Stop any running animations
            StopAllCoroutines();

            // Play popup animation
            popupCoroutine = StartCoroutine(PopupRoutine());
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;

            StopAllCoroutines();
            disappearCoroutine = StartCoroutine(DisappearRoutine());
        }

        #endregion

        #region Popup Animation

        private IEnumerator PopupRoutine()
        {
            float elapsed = 0f;
            transform.localScale = startScale;

            // Phase 1: Scale up to peak (overshoot)
            while (elapsed < popupDuration * 0.6f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / (popupDuration * 0.6f));
                float curveT = popupCurve.Evaluate(t);
                transform.localScale = Vector3.Lerp(startScale, peakScale, curveT);
                yield return null;
            }

            // Phase 2: Settle to normal scale
            elapsed = 0f;
            while (elapsed < popupDuration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / (popupDuration * 0.4f));
                transform.localScale = Vector3.Lerp(peakScale, normalScale, t);
                yield return null;
            }

            transform.localScale = normalScale;

            // Start idle wobble after popup
            wobbleCoroutine = StartCoroutine(WobbleRoutine());
        }

        #endregion

        #region Idle Wobble

        private IEnumerator WobbleRoutine()
        {
            float timeOffset = Random.Range(0f, Mathf.PI * 2f);

            while (true)
            {
                float wobble = Mathf.Sin((Time.time + timeOffset) * wobbleSpeed) * wobbleAmount;

                transform.localScale = new Vector3(
                    normalScale.x + wobble,
                    normalScale.y - wobble,
                    normalScale.z
                );

                yield return null;
            }
        }

        #endregion

        #region Disappear Animation

        private IEnumerator DisappearRoutine()
        {
            isVisible = false;

            Vector3 startLocalPos = transform.localPosition;
            Vector3 targetLocalPos = startLocalPos + disappearMoveOffset;
            Vector3 startScl = transform.localScale;

            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                float curveT = fadeOutCurve.Evaluate(t);

                // Move up
                transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, curveT);

                // Scale down
                transform.localScale = Vector3.Lerp(startScl, Vector3.zero, curveT);

                // Fade out
                SetAlpha(Mathf.Lerp(1f, 0f, curveT));

                yield return null;
            }

            // Reset local position for next use
            transform.localPosition = baseLocalPosition;

            gameObject.SetActive(false);
        }

        #endregion

        #region Helpers

        private void SetAlpha(float alpha)
        {
            if (numberText == null) return;
            Color c = numberText.color;
            c.a = alpha;
            numberText.color = c;
        }

        public bool IsVisible() => isVisible;

        #endregion
    }
}