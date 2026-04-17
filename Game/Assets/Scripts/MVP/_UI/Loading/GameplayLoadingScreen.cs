using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows a loading panel until all critical gameplay systems are initialized.
/// Attach to a Canvas in the gameplay scene with a full-screen panel.
///
/// Setup:
///   1. Create a full-screen UI panel (loading screen) under a Canvas.
///   2. Assign the panel's CanvasGroup to <see cref="loadingCanvasGroup"/>.
///   3. Optionally assign <see cref="hudCanvasGroup"/> and <see cref="uiCanvasGroup"/>
///      to keep them hidden until loading finishes.
///   4. Attach this script to that Canvas or panel GameObject.
/// </summary>
public class GameplayLoadingScreen : MonoBehaviour
{
    public static GameplayLoadingScreen Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private CanvasGroup uiCanvasGroup;

    [Header("Optional — Progress")]
    [SerializeField] private UnityEngine.UI.Slider progressBar;
    [SerializeField] private TMPro.TMP_Text statusText;

    [Header("Fade")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    /// <summary>True while the loading screen is visible.</summary>
    public bool IsLoading { get; private set; } = true;

    private readonly List<SystemEntry> _systems = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Show loading, hide gameplay layers
        loadingCanvasGroup.Show();
        if (hudCanvasGroup != null) hudCanvasGroup.Hide();
        if (uiCanvasGroup != null) uiCanvasGroup.Hide();

        // Block player input while loading
        if (InputManager.Instance != null)
            InputManager.Instance.DisablePlayerActions();
    }

    void Start()
    {
        BuildSystemList();
        StartCoroutine(WaitForSystems());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Register all systems that must be ready before gameplay starts.
    /// Add or remove entries here when new critical systems are introduced.
    /// </summary>
    private void BuildSystemList()
    {
        // ── Catalogs ──
        _systems.Add(new SystemEntry("Item Catalog",
            () => ItemCatalogService.Instance != null && ItemCatalogService.Instance.IsReady));

        _systems.Add(new SystemEntry("Plant Catalog",
            () => PlantCatalogService.Instance != null && PlantCatalogService.Instance.IsReady));

        _systems.Add(new SystemEntry("Recipe Catalog",
            () => RecipeCatalogService.Instance != null && RecipeCatalogService.Instance.IsReady));

        _systems.Add(new SystemEntry("Material Catalog",
            () => MaterialCatalogService.Instance != null && MaterialCatalogService.Instance.IsReady));

        _systems.Add(new SystemEntry("Skin Catalog",
            () => SkinCatalogManager.Instance != null && SkinCatalogManager.Instance.IsReady));

        _systems.Add(new SystemEntry("Achievement Catalog",
            () => AchievementCatalogService.Instance != null && AchievementCatalogService.Instance.IsReady));

        // ── World data ──
        _systems.Add(new SystemEntry("World Data",
            () => WorldDataBootstrapper.Instance != null && WorldDataBootstrapper.Instance.IsReady));

        _systems.Add(new SystemEntry("World Manager",
            () => WorldDataManager.Instance != null && WorldDataManager.Instance.IsInitialized));

        // ── Core singletons ──
        _systems.Add(new SystemEntry("Input Manager",
            () => InputManager.Instance != null));
    }

    private IEnumerator WaitForSystems()
    {
        while (true)
        {
            int ready = 0;

            string pending = null;
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].IsReady())
                    ready++;
                else
                    pending ??= _systems[i].Name;
            }

            float progress = (float)ready / _systems.Count;

            if (progressBar != null)
                progressBar.value = progress;
            if (statusText != null)
                statusText.text = ready < _systems.Count
                    ? $"Loading {pending}... ({ready}/{_systems.Count})"
                    : "Ready!";

            if (ready >= _systems.Count)
                break;

            yield return null;
        }

        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        IsLoading = false;

        // Show gameplay layers
        if (hudCanvasGroup != null) hudCanvasGroup.Show();
        if (uiCanvasGroup != null) uiCanvasGroup.Show();

        // Re-enable player input
        if (InputManager.Instance != null)
            InputManager.Instance.EnablePlayerActions();

        // Fade the loading panel out
        if (fadeOutDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                loadingCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
        }

        loadingCanvasGroup.Hide();
        gameObject.SetActive(false);
    }

    /// <summary>Lightweight descriptor for a system to wait on.</summary>
    private class SystemEntry
    {
        public readonly string Name;
        private readonly global::System.Func<bool> _check;

        public SystemEntry(string name, global::System.Func<bool> check)
        {
            Name = name;
            _check = check;
        }

        public bool IsReady() => _check();
    }
}
