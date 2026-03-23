using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MonoBehaviour view that displays cleanup notification to the player.
/// Shows a temporary panel listing which orphaned data was removed.
/// Follows the AchievementUnlockPopupView pattern (CanvasGroup + auto-dismiss).
/// </summary>
public class CleanupNotificationView : MonoBehaviour, ICleanupNotificationView
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button dismissButton;

    [Header("Settings")]
    [Tooltip("How long the notification stays visible (seconds). Editable in Inspector.")]
    [SerializeField] private float displayDuration = 8f;

    private Coroutine autoHideCoroutine;

    private void Start()
    {
        if (dismissButton != null)
            dismissButton.onClick.AddListener(Hide);

        if (canvasGroup != null)
            canvasGroup.Hide();
    }

    public void ShowNotification(List<CleanupNotificationData> entries, float duration)
    {
        if (entries == null || entries.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine("<b>Data has been cleaned up:</b>");
        foreach (var e in entries)
            sb.AppendLine($"  {e.Message}");

        if (contentText != null)
            contentText.text = sb.ToString();

        if (canvasGroup != null)
            canvasGroup.Show();

        float effectiveDuration = duration > 0 ? duration : displayDuration;

        if (autoHideCoroutine != null)
            StopCoroutine(autoHideCoroutine);
        autoHideCoroutine = StartCoroutine(AutoHide(effectiveDuration));
    }

    public void Hide()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (canvasGroup != null)
            canvasGroup.Hide();
    }

    private IEnumerator AutoHide(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (canvasGroup != null)
            canvasGroup.Hide();
        autoHideCoroutine = null;
    }
}
