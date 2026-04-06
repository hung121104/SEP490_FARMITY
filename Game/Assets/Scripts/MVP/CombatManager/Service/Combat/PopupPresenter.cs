using UnityEngine;

namespace CombatManager.Presenter
{
    /// <summary>
    /// Presenter for individual damage popup.
    /// Manages lifecycle of a single popup instance.
    /// </summary>
    public class PopupPresenter : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float duration = 0.8f;

        private float elapsed = 0f;

        #region Unity Lifecycle

        private void Start()
        {
            // Start self-destruct timer
            Destroy(gameObject, duration);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
        }

        #endregion

        #region Public API

        public float GetElapsedTime() => elapsed;
        public float GetDuration() => duration;
        public float GetNormalizedTime() => Mathf.Clamp01(elapsed / duration);

        #endregion
    }
}