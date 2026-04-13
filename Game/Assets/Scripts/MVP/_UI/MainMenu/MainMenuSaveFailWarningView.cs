using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MVP View — shows a dismissible warning popup in the MainMenu scene when
/// the leave-room forced save failed.
///
/// Placement: attach to a canvas object in MainMenuScene.
///
/// Inspector requirements:
///   - CanvasGroup on the root (or assign in Inspector)
///   - (Optional) TextMeshProUGUI for the message label
///   - (Optional) Button for manual dismiss
///
/// On Start() it consumes WorldSaveManager.PendingLeaveRoomSaveFailWarning; if
/// the flag is set it shows the popup and auto-dismisses after displayDuration.
/// </summary>
public class MainMenuSaveFailWarningView : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button dismissButton;

    [Header("Settings")]
    [Tooltip("How long the popup stays visible before auto-dismissing (seconds).")]
    [SerializeField] private float displayDuration = 6f;
    [Tooltip("Fade-out duration in seconds.")]
    [SerializeField] private float fadeDuration    = 0.4f;
    [Tooltip("Message shown inside the popup.")]
    [SerializeField] private string warningMessage = "World failed to save before you left.\nYour latest progress may not have been saved.";

    // ── Runtime ──────────────────────────────────────────────────────────────
    private Coroutine _autoDismissCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        if (canvasGroup != null)
            canvasGroup.Hide();

        if (dismissButton != null)
            dismissButton.onClick.AddListener(DismissPopup);

        if (messageText != null)
            messageText.text = warningMessage;

        // Consume the cross-scene flag and show the popup if it was set.
        if (WorldSaveManager.PendingLeaveRoomSaveFailWarning)
        {
            WorldSaveManager.PendingLeaveRoomSaveFailWarning = false;
            ShowWarning();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void ShowWarning()
    {
        if (canvasGroup == null) return;

        if (_autoDismissCoroutine != null)
            StopCoroutine(_autoDismissCoroutine);

        canvasGroup.Show();
        _autoDismissCoroutine = StartCoroutine(AutoDismissCoroutine());
    }

    // ── Private ───────────────────────────────────────────────────────────────
    private void DismissPopup()
    {
        if (_autoDismissCoroutine != null)
        {
            StopCoroutine(_autoDismissCoroutine);
            _autoDismissCoroutine = null;
        }

        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator AutoDismissCoroutine()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return FadeOutCoroutine();
        _autoDismissCoroutine = null;
    }

    private IEnumerator FadeOutCoroutine()
    {
        if (canvasGroup == null) yield break;

        float elapsed   = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.Hide();
    }
}
