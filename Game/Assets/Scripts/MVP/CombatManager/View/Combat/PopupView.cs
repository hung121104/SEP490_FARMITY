using UnityEngine;
using TMPro;
using CombatManager.Presenter;

namespace CombatManager.View
{
    /// <summary>
    /// View for individual damage popup.
    /// Handles animation (movement, scale, fade).
    /// </summary>
    public class PopupView : MonoBehaviour
    {
        [Header("Presenter")]
        [SerializeField] private PopupPresenter presenter;

        [Header("Motion")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0f, 0.8f, 0f);
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Scale")]
        [SerializeField] private Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
        [SerializeField] private Vector3 peakScale = new Vector3(1.1f, 1.1f, 1f);
        [SerializeField] private Vector3 endScale = new Vector3(1f, 1f, 1f);

        [Header("Fade")]
        [SerializeField] private float fadeOutStart = 0.5f;

        private TMP_Text tmpText;
        private Vector3 startPos;

        #region Unity Lifecycle

        private void Awake()
        {
            tmpText = GetComponentInChildren<TMP_Text>();
            
            if (presenter == null)
            {
                presenter = GetComponent<PopupPresenter>();
            }
        }

        private void Start()
        {
            startPos = transform.position;
            transform.localScale = startScale;
        }

        private void Update()
        {
            if (presenter == null)
                return;

            AnimatePopup();
        }

        #endregion

        #region Animation

        private void AnimatePopup()
        {
            float t = presenter.GetNormalizedTime();

            // Motion
            float moveT = moveCurve.Evaluate(t);
            transform.position = startPos + moveOffset * moveT;

            // Scale (pop then settle)
            if (t < 0.25f)
            {
                float popT = t / 0.25f;
                transform.localScale = Vector3.Lerp(startScale, peakScale, popT);
            }
            else
            {
                float settleT = (t - 0.25f) / 0.75f;
                transform.localScale = Vector3.Lerp(peakScale, endScale, settleT);
            }

            // Fade out
            if (tmpText != null && t >= fadeOutStart)
            {
                float fadeT = (t - fadeOutStart) / (1f - fadeOutStart);
                Color c = tmpText.color;
                c.a = Mathf.Lerp(1f, 0f, fadeT);
                tmpText.color = c;
            }
        }

        #endregion
    }
}