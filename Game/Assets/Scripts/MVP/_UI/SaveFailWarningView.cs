using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MVP View — shows a dismissible warning popup when the world auto-save fails.
///
/// Placement: attach to a persistent HUD canvas object in the in-game scene.
///
/// Prefab / Inspector requirements:
///   - CanvasGroup on the root (or assign in Inspector)
///   - (Optional) TextMeshProUGUI for the message label
///   - (Optional) Button for manual dismiss
///
/// The popup auto-dismisses after <see cref="displayDuration"/> seconds.
/// If multiple failures occur while the popup is already visible the timer resets.
/// </summary>
public class SaveFailWarningView : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static SaveFailWarningView Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button dismissButton;

    [Header("Settings")]
    [Tooltip("How long the popup stays visible before auto-dismissing (seconds).")]
    [SerializeField] private float displayDuration = 5f;
    [Tooltip("Fade-out duration in seconds.")]
    [SerializeField] private float fadeDuration    = 0.4f;
    [Tooltip("Message shown inside the popup.")]
    [SerializeField] private string warningMessage = "World save failed. Your progress will be retried on the next auto-save.";

    // ── Runtime ──────────────────────────────────────────────────────────────
    private Coroutine _autoDismissCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (canvasGroup != null)
            canvasGroup.Hide();

        if (dismissButton != null)
            dismissButton.onClick.AddListener(DismissPopup);

        if (messageText != null)
            messageText.text = warningMessage;
    }

    private void OnEnable()  => WorldSaveManager.OnSaveFailed += ShowWarning;
    private void OnDisable() => WorldSaveManager.OnSaveFailed -= ShowWarning;

    // ── Public API ────────────────────────────────────────────────────────────
    public void ShowWarning()
    {
        if (canvasGroup == null) return;

        // Stop any existing auto-dismiss (reset timer on repeated failures)
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

        float elapsed = 0f;
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
