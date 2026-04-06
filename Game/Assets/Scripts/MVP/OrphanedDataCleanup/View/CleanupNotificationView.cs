using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MonoBehaviour view that displays cleanup notification to the player.
/// Shows messages one at a time in a queue, auto-advancing after each duration.
/// Follows the AchievementUnlockPopupView pattern (CanvasGroup + auto-dismiss).
/// </summary>
public class CleanupNotificationView : MonoBehaviour, ICleanupNotificationView
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button dismissButton;

    [Header("Settings")]
    [Tooltip("How long each message stays visible (seconds).")]
    [SerializeField] private float displayDuration = 4f;

    private readonly Queue<string> messageQueue = new Queue<string>();
    private Coroutine showCoroutine;

    private void Start()
    {
        if (dismissButton != null)
            dismissButton.onClick.AddListener(DismissCurrent);

        if (canvasGroup != null)
            canvasGroup.Hide();
    }

    public void ShowNotification(List<CleanupNotificationData> entries, float duration)
    {
        if (entries == null || entries.Count == 0) return;

        foreach (var e in entries)
        {
            if (!messageQueue.Contains(e.Message))
                messageQueue.Enqueue(e.Message);
        }

        float effectiveDuration = duration > 0 ? duration : displayDuration;

        if (showCoroutine == null)
            showCoroutine = StartCoroutine(ProcessQueue(effectiveDuration));
    }

    public void Hide()
    {
        messageQueue.Clear();

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        if (canvasGroup != null)
            canvasGroup.Hide();
    }

    /// <summary>
    /// Dismiss button: skip current message and show next (or hide if queue empty).
    /// </summary>
    private void DismissCurrent()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        if (messageQueue.Count > 0)
            showCoroutine = StartCoroutine(ProcessQueue(displayDuration));
        else if (canvasGroup != null)
            canvasGroup.Hide();
    }

    private IEnumerator ProcessQueue(float secondsPerMessage)
    {
        while (messageQueue.Count > 0)
        {
            string msg = messageQueue.Dequeue();
            int remaining = messageQueue.Count;

            string display = remaining > 0
                ? $"{msg}\n<size=80%><color=#888>({remaining} more)</color></size>"
                : msg;

            if (contentText != null)
                contentText.text = display;

            if (canvasGroup != null)
                canvasGroup.Show();

            yield return new WaitForSeconds(secondsPerMessage);
        }

        if (canvasGroup != null)
            canvasGroup.Hide();

        showCoroutine = null;
    }
}
