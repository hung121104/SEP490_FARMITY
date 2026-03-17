using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays catalog loading progress with a progress bar and percentage text.
/// Subscribes to catalog service progress events and updates UI in real-time.
/// </summary>
public class ResourceDownloadProgress : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private CanvasGroup canvasGroup; // For fade in/out
    [SerializeField] private bool loadSceneOnFinished;
    [SerializeField] private string finishedSceneName;

    private float _currentProgress = 0f;
    private float _targetProgress = 0f;
    private const float PROGRESS_LERP_SPEED = 5f;
    private bool _isFinishing;

    private void OnEnable()
    {
        // Subscribe to all catalog progress events
        CatalogProgressManager.OnProgressChanged += UpdateProgress;
        CatalogProgressManager.OnCatalogStarted += ShowProgress;
        CatalogProgressManager.OnCatalogCompleted += HideProgress;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        CatalogProgressManager.OnProgressChanged -= UpdateProgress;
        CatalogProgressManager.OnCatalogStarted -= ShowProgress;
        CatalogProgressManager.OnCatalogCompleted -= HideProgress;
    }

    private void Start()
    {
        // Initialize UI
        if (progressBar != null)
            progressBar.value = 0f;
        if (progressText != null)
            progressText.text = "0%";
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        // Smooth progress bar animation
        if (Mathf.Abs(_currentProgress - _targetProgress) > 0.01f)
        {
            _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * PROGRESS_LERP_SPEED);
            UpdateUI();
        }
    }

    private void UpdateProgress(float progress)
    {
        _targetProgress = Mathf.Clamp01(progress);
    }

    private void ShowProgress()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        _isFinishing = false;
        _currentProgress = 0f;
        _targetProgress = 0f;
        UpdateUI();
    }

    private void HideProgress()
    {
        if (_isFinishing)
            return;

        _isFinishing = true;
        _targetProgress = 1f;
        _currentProgress = 1f;
        UpdateUI();
        // Fade out after a short delay
        StartCoroutine(FadeOutAfterDelay(1f));
    }

    private void UpdateUI()
    {
        if (progressBar != null)
            progressBar.value = _currentProgress;

        if (progressText != null)
            progressText.text = $"{(_currentProgress * 100):F0}%";
    }

    private System.Collections.IEnumerator FadeOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float fadeDuration = 0.5f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
        }

        TryLoadFinishedScene();
    }

    private void TryLoadFinishedScene()
    {
        if (!loadSceneOnFinished || string.IsNullOrWhiteSpace(finishedSceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(finishedSceneName))
        {
            Debug.LogError($"[ResourceDownloadProgress] Scene '{finishedSceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(finishedSceneName);
    }
}
