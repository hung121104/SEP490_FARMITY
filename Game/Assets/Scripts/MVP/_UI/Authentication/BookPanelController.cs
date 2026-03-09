using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookPanelController : MonoBehaviour
{
    [Header("Title Canvas Group (never hidden by panel logic)")]
    [SerializeField] private CanvasGroup titleCanvasGroup;

    [Header("Title Button")]
    [SerializeField] private Button openTitleBtn;

    [Header("Panels")]
    [SerializeField] private List<PanelEntry> panels = new List<PanelEntry>();

    /// <summary>Fired when any panel open button is clicked.</summary>
    public event Action OnPanelOpened;
    /// <summary>Fired when any close button or the title button is clicked.</summary>
    public event Action OnShowTitle;

    private void Awake()
    {
        BindButtons();
    }

    private void BindButtons()
    {
        if (openTitleBtn != null) openTitleBtn.onClick.AddListener(ShowTitle);

        foreach (var entry in panels)
        {
            var captured = entry;
            if (captured.openButton  != null) captured.openButton.onClick.AddListener(() => ShowPanel(captured));
            if (captured.closeButton != null) captured.closeButton.onClick.AddListener(ShowTitle);
        }
    }

    private void ShowPanel(PanelEntry entry)
    {
        // Hide every panel canvas group — titleCanvasGroup is never touched.
        foreach (var p in panels)
            if (p.canvasGroup != null) p.canvasGroup.Hide();

        entry.canvasGroup?.Show();
        OnPanelOpened?.Invoke();
    }

    /// <summary>Show a panel by index programmatically (e.g. from a presenter).</summary>
    public void ShowPanel(int index)
    {
        if (index < 0 || index >= panels.Count) return;
        ShowPanel(panels[index]);
    }

    public void ShowTitle()
    {
        OnShowTitle?.Invoke();
    }

    [Serializable]
    public class PanelEntry
    {
        public CanvasGroup canvasGroup;
        public Button openButton;
        public Button closeButton;
    }
}
