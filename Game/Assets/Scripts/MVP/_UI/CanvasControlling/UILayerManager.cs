using UnityEngine;

/// <summary>
/// Coordinates HUD visibility based on how many UI panels are currently open.
/// UI panels call NotifyPanelOpened / NotifyPanelClosed when they show / hide.
/// HUD hides when any panel is open and shows again when all panels close.
///
/// Setup: attach to any persistent gameplay GameObject and assign the HUD CanvasGroup.
/// </summary>
public class UILayerManager : MonoBehaviour
{
    public static UILayerManager Instance { get; private set; }

    [SerializeField] private CanvasGroup hudCanvasGroup;

    private int _openPanelCount;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Call when a UI panel opens that should hide the HUD.</summary>
    public void NotifyPanelOpened()
    {
        _openPanelCount++;
        if (_openPanelCount == 1 && hudCanvasGroup != null)
            hudCanvasGroup.Hide();
    }

    /// <summary>Call when a UI panel closes.</summary>
    public void NotifyPanelClosed()
    {
        _openPanelCount = Mathf.Max(0, _openPanelCount - 1);
        if (_openPanelCount == 0 && hudCanvasGroup != null)
            hudCanvasGroup.Show();
    }

    /// <summary>True when at least one UI panel is open.</summary>
    public bool IsAnyPanelOpen => _openPanelCount > 0;
}
