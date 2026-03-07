using System;
using System.Collections.Generic;
using UnityEngine;

public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager Instance { get; private set; }

    [Header("Hotbar")]
    [Tooltip("Hotbar panel to hide when any UI panel is open. Assign in Inspector.")]
    [SerializeField] private HotbarView hotbarView;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = false;

    private readonly Dictionary<string, IUIPanel> registeredPanels = new();
    private readonly Stack<IUIPanel> popupStack = new();
    private IUIPanel currentPanel; // Current active Panel-layer panel

    /// <summary>
    /// Fired when any panel is opened. Passes the PanelId.
    /// </summary>
    public event Action<string> OnPanelOpened;

    /// <summary>
    /// Fired when any panel is closed. Passes the PanelId.
    /// </summary>
    public event Action<string> OnPanelClosed;

    /// <summary>
    /// Fired when UI active state changes. true = at least one Panel/Popup is open.
    /// Use this to disable player movement, lock cursor, etc.
    /// </summary>
    public event Action<bool> OnAnyPanelActiveChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #region Registration

    public void Register(IUIPanel panel)
    {
        if (panel == null) return;

        if (registeredPanels.ContainsKey(panel.PanelId))
        {
            Log($"Panel '{panel.PanelId}' is already registered. Overwriting.", LogType.Warning);
        }

        registeredPanels[panel.PanelId] = panel;
        Log($"Registered panel: {panel.PanelId} (Layer: {panel.Layer})");
    }

    public void Unregister(string panelId)
    {
        if (string.IsNullOrEmpty(panelId)) return;

        if (currentPanel != null && currentPanel.PanelId == panelId)
            currentPanel = null;

        registeredPanels.Remove(panelId);
        Log($"Unregistered panel: {panelId}");
    }

    #endregion

    #region Open / Close / Toggle

    public void Open(string panelId)
    {
        if (!registeredPanels.TryGetValue(panelId, out var panel))
        {
            Log($"Cannot open '{panelId}': not registered.", LogType.Warning);
            return;
        }

        if (panel.IsVisible) return;

        bool wasActive = IsAnyPanelOpen();

        switch (panel.Layer)
        {
            case UILayerType.Panel:
                // Mutual exclusion: close current panel first
                if (currentPanel != null && currentPanel.PanelId != panelId)
                    CloseInternal(currentPanel);

                currentPanel = panel;
                panel.Show();
                break;

            case UILayerType.Popup:
                // Blur whatever is on top (current panel or previous popup) — only if they support focus
                if (popupStack.Count > 0)
                    (popupStack.Peek() as IFocusablePanel)?.OnPanelLostFocus();
                else
                    (currentPanel as IFocusablePanel)?.OnPanelLostFocus();

                popupStack.Push(panel);
                panel.Show();
                break;

            case UILayerType.HUD:
            case UILayerType.Overlay:
                panel.Show();
                break;
        }

        OnPanelOpened?.Invoke(panelId);

        if (!wasActive && IsAnyPanelOpen())
            FireAnyPanelActiveChanged(true);
    }

    public void Close(string panelId)
    {
        if (!registeredPanels.TryGetValue(panelId, out var panel))
        {
            Log($"Cannot close '{panelId}': not registered.", LogType.Warning);
            return;
        }

        if (!panel.IsVisible) return;

        CloseInternal(panel);
    }

    public void Toggle(string panelId)
    {
        if (!registeredPanels.TryGetValue(panelId, out var panel))
        {
            Log($"Cannot toggle '{panelId}': not registered.", LogType.Warning);
            return;
        }

        if (panel.IsVisible)
            Close(panelId);
        else
            Open(panelId);
    }

    /// <summary>
    /// Close all popups first (top to bottom), then close the current panel.
    /// </summary>
    public void CloseAll()
    {
        // Close popups in stack order
        while (popupStack.Count > 0)
        {
            var popup = popupStack.Pop();
            if (popup.IsVisible)
            {
                popup.Hide();
                OnPanelClosed?.Invoke(popup.PanelId);
            }
        }

        // Close current panel
        if (currentPanel != null && currentPanel.IsVisible)
        {
            currentPanel.Hide();
            OnPanelClosed?.Invoke(currentPanel.PanelId);
            currentPanel = null;
        }

        FireAnyPanelActiveChanged(false);
    }

    /// <summary>
    /// Close the topmost UI element (popup first, then panel). Useful for ESC key.
    /// </summary>
    public void CloseTopmost()
    {
        if (popupStack.Count > 0)
        {
            var popup = popupStack.Pop();
            popup.Hide();
            OnPanelClosed?.Invoke(popup.PanelId);

            // Restore focus to whatever is below — only if it supports focus
            if (popupStack.Count > 0)
                (popupStack.Peek() as IFocusablePanel)?.OnPanelFocused();
            else
                (currentPanel as IFocusablePanel)?.OnPanelFocused();

            if (!IsAnyPanelOpen())
                FireAnyPanelActiveChanged(false);

            return;
        }

        if (currentPanel != null && currentPanel.IsVisible)
        {
            currentPanel.Hide();
            OnPanelClosed?.Invoke(currentPanel.PanelId);
            currentPanel = null;

            FireAnyPanelActiveChanged(false);
        }
    }

    #endregion

    #region Queries

    public bool IsAnyPanelOpen()
    {
        return (currentPanel != null && currentPanel.IsVisible) || popupStack.Count > 0;
    }

    public bool IsPanelOpen(string panelId)
    {
        return registeredPanels.TryGetValue(panelId, out var panel) && panel.IsVisible;
    }

    public bool IsPanelRegistered(string panelId)
    {
        return !string.IsNullOrEmpty(panelId) && registeredPanels.ContainsKey(panelId);
    }

    public IUIPanel GetPanel(string panelId)
    {
        registeredPanels.TryGetValue(panelId, out var panel);
        return panel;
    }

    #endregion

    #region Internal

    private void CloseInternal(IUIPanel panel)
    {
        bool wasActive = IsAnyPanelOpen();

        switch (panel.Layer)
        {
            case UILayerType.Panel:
                panel.Hide();
                if (currentPanel == panel)
                    currentPanel = null;
                break;

            case UILayerType.Popup:
                panel.Hide();
                // Rebuild stack without this popup (in case it's not on top)
                RebuildPopupStack(panel);
                // Restore focus — only if the panel below supports it
                if (popupStack.Count > 0)
                    (popupStack.Peek() as IFocusablePanel)?.OnPanelFocused();
                else
                    (currentPanel as IFocusablePanel)?.OnPanelFocused();
                break;

            case UILayerType.HUD:
            case UILayerType.Overlay:
                panel.Hide();
                break;
        }

        OnPanelClosed?.Invoke(panel.PanelId);

        if (wasActive && !IsAnyPanelOpen())
            FireAnyPanelActiveChanged(false);
    }

    private void RebuildPopupStack(IUIPanel removed)
    {
        if (popupStack.Count == 0) return;

        // If the removed popup is on top, just pop
        if (popupStack.Peek() == removed)
        {
            popupStack.Pop();
            return;
        }

        // Otherwise rebuild without it
        var temp = new List<IUIPanel>(popupStack);
        temp.Remove(removed);
        temp.Reverse();
        popupStack.Clear();
        foreach (var p in temp)
            popupStack.Push(p);
    }

    private void FireAnyPanelActiveChanged(bool isActive)
    {
        hotbarView?.SetActive(!isActive);
        OnAnyPanelActiveChanged?.Invoke(isActive);
    }

    private void Log(string message, LogType type = LogType.Log)
    {
        if (!enableDebugLog) return;

        switch (type)
        {
            case LogType.Warning:
                Debug.LogWarning($"[UIPanelManager] {message}");
                break;
            case LogType.Error:
                Debug.LogError($"[UIPanelManager] {message}");
                break;
            default:
                Debug.Log($"[UIPanelManager] {message}");
                break;
        }
    }

    #endregion
}
