using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AchievementManager.Model;

namespace AchievementManager.View
{
    /// <summary>
    /// Professional popup panel for achievement unlocks.
    /// Queue system: shows one popup at a time.
    /// Auto-dismisses after displayDuration seconds.
    /// Player can also manually dismiss.
    /// </summary>
    public class AchievementUnlockPopupView : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Popup Panel")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Animator popupAnimator;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI achievementNameText;
        [SerializeField] private TextMeshProUGUI achievementDescText;

        [Header("Buttons")]
        [SerializeField] private Button dismissButton;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private string showTrigger    = "Show";
        [SerializeField] private string hideTrigger    = "Hide";

        #endregion

        #region Runtime State

        private Queue<AchievementData> popupQueue = new Queue<AchievementData>();
        private bool isShowing = false;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (dismissButton != null)
                dismissButton.onClick.AddListener(DismissCurrent);

            if (popupRoot != null)
                popupRoot.SetActive(false);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Add achievement to popup queue.
        /// If nothing is showing, starts immediately.
        /// Multiple unlocks at once = queued one by one.
        /// </summary>
        public void EnqueueUnlock(AchievementData achievement)
        {
            if (achievement == null) return;

            popupQueue.Enqueue(achievement);
            Debug.Log($"[AchievementUnlockPopupView] " +
                      $"Queued: {achievement.name} | Queue size: {popupQueue.Count}");

            // Start showing if not already
            if (!isShowing)
                StartCoroutine(ShowNextInQueue());
        }

        /// <summary>
        /// Manually dismiss current popup.
        /// Shows next in queue if any.
        /// </summary>
        public void DismissCurrent()
        {
            StopAllCoroutines();
            StartCoroutine(HideAndShowNext());
        }

        #endregion

        #region Queue Logic

        private IEnumerator ShowNextInQueue()
        {
            while (popupQueue.Count > 0)
            {
                AchievementData next = popupQueue.Dequeue();
                yield return StartCoroutine(ShowPopup(next));
            }

            isShowing = false;
        }

        private IEnumerator ShowPopup(AchievementData achievement)
        {
            isShowing = true;

            // Populate content
            if (titleText != null)
                titleText.text = "Achievement Unlocked!";

            if (achievementNameText != null)
                achievementNameText.text = achievement.name;

            if (achievementDescText != null)
                achievementDescText.text = achievement.description;

            // Show panel
            if (popupRoot != null)
                popupRoot.SetActive(true);

            // Play show animation if available
            if (popupAnimator != null)
                popupAnimator.SetTrigger(showTrigger);

            Debug.Log($"[AchievementUnlockPopupView] " +
                      $"Showing: {achievement.name}");

            // Wait for display duration
            yield return new WaitForSeconds(displayDuration);

            // Hide
            yield return StartCoroutine(HidePopup());
        }

        private IEnumerator HidePopup()
        {
            // Play hide animation if available
            if (popupAnimator != null)
            {
                popupAnimator.SetTrigger(hideTrigger);
                // Wait for animation to finish
                yield return new WaitForSeconds(0.5f);
            }

            if (popupRoot != null)
                popupRoot.SetActive(false);

            Debug.Log("[AchievementUnlockPopupView] Popup hidden");
        }

        private IEnumerator HideAndShowNext()
        {
            yield return StartCoroutine(HidePopup());

            if (popupQueue.Count > 0)
                yield return StartCoroutine(ShowNextInQueue());
            else
                isShowing = false;
        }

        #endregion
    }
}