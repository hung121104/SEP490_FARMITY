using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Displays Combat Mode state with visual indicators.
/// Shows Sword icon when Combat Mode ON, Letter icon when OFF.
/// Sits on a GameObject in CombatSystem and references UI elements in Canvas.
/// </summary>
public class CombatModeIndicator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image combatModeIcon;  // Sword sprite
    [SerializeField] private Image normalModeIcon;  // Letter sprite

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine fadeRoutine;

    private void Start()
    {
        // Find icons in Canvas if not assigned
        if (combatModeIcon == null)
        {
            combatModeIcon = FindByName("CombatModeIcon")?.GetComponent<Image>();
        }
        if (normalModeIcon == null)
        {
            normalModeIcon = FindByName("NormalModeIcon")?.GetComponent<Image>();
        }

        // Subscribe to mode changes
        CombatModeManager.OnCombatModeChanged += OnCombatModeChanged;

        // Set initial state
        UpdateIndicator(CombatModeManager.Instance.IsCombatModeActive);
    }

    private void OnDestroy()
    {
        CombatModeManager.OnCombatModeChanged -= OnCombatModeChanged;
    }

    private void OnCombatModeChanged(bool isActive)
    {
        UpdateIndicator(isActive);
    }

    private void UpdateIndicator(bool isCombatMode)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeSwitch(isCombatMode));
    }

    private IEnumerator FadeSwitch(bool isCombatMode)
    {
        float elapsed = 0f;

        // Fade out current, fade in new
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = fadeCurve.Evaluate(elapsed / fadeDuration);

            if (combatModeIcon != null)
            {
                Color combatColor = combatModeIcon.color;
                combatColor.a = isCombatMode ? progress : 1f - progress;
                combatModeIcon.color = combatColor;
            }

            if (normalModeIcon != null)
            {
                Color normalColor = normalModeIcon.color;
                normalColor.a = isCombatMode ? 1f - progress : progress;
                normalModeIcon.color = normalColor;
            }

            yield return null;
        }

        // Ensure final state
        if (combatModeIcon != null)
        {
            Color combatColor = combatModeIcon.color;
            combatColor.a = isCombatMode ? 1f : 0f;
            combatModeIcon.color = combatColor;
        }

        if (normalModeIcon != null)
        {
            Color normalColor = normalModeIcon.color;
            normalColor.a = isCombatMode ? 0f : 1f;
            normalModeIcon.color = normalColor;
        }

        fadeRoutine = null;
    }

    private Transform FindByName(string name)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return null;

        return canvas.transform.Find(name);
    }
}