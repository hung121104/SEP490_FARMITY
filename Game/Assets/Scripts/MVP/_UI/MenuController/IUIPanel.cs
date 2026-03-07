public interface IUIPanel
{
    /// <summary>
    /// Unique identifier for this panel (e.g. "Inventory", "Crafting", "Cooking")
    /// </summary>
    string PanelId { get; }

    /// <summary>
    /// Whether this panel is currently visible
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Determines how this panel interacts with other panels
    /// </summary>
    UILayerType Layer { get; }

    /// <summary>
    /// Show this panel
    /// </summary>
    void Show();

    /// <summary>
    /// Hide this panel
    /// </summary>
    void Hide();
}

/// <summary>
/// Optional extension for panels that need to respond to focus changes
/// caused by popups opening/closing on top of them.
/// </summary>
public interface IFocusablePanel : IUIPanel
{
    /// <summary>
    /// Called when this panel is brought back to foreground (e.g. popup above it closed)
    /// </summary>
    void OnPanelFocused();

    /// <summary>
    /// Called when a popup opens on top of this panel
    /// </summary>
    void OnPanelLostFocus();
}
