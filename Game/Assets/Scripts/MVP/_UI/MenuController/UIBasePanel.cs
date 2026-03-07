using UnityEngine;

/// <summary>
/// Base class for all UI panels. Handles registration with UIPanelManager,
/// Show/Hide with animation hooks, and consistent IsVisible tracking.
/// 
/// Usage: Attach directly to any GameObject and configure in Inspector,
/// or extend and override OnShow()/OnHide() for custom behavior.
/// </summary>
public class UIBasePanel : MonoBehaviour, IUIPanel
{
    [Header("UIBasePanel Settings")]
    [Tooltip("The root GameObject of this panel (activated/deactivated on Show/Hide).")]
    [SerializeField] private GameObject mainPanel;

    [Tooltip("Unique identifier used by UIPanelManager (e.g. 'Inventory', 'Crafting').")]
    [SerializeField] private string panelId;

    [Tooltip("Determines how this panel interacts with other panels.")]
    [SerializeField] private UILayerType layer = UILayerType.Panel;

    #region IUIPanel

    public string PanelId => panelId;
    public bool IsVisible => mainPanel != null && mainPanel.activeSelf;
    public UILayerType Layer => layer;

    public void Show()
    {
        if (mainPanel != null)
            mainPanel.SetActive(true);

        OnShow();
    }

    public void Hide()
    {
        OnHide();

        if (mainPanel != null)
            mainPanel.SetActive(false);
    }

    #endregion

    #region Registration

    protected virtual void OnEnable()
    {
        if (UIPanelManager.Instance != null)
            UIPanelManager.Instance.Register(this);
    }


    //Fallback in case panel is enabled after Start (e.g. dynamically spawned)
    protected virtual void Start()
    {
        if (UIPanelManager.Instance != null &&
            !UIPanelManager.Instance.IsPanelRegistered(PanelId))
        {
            UIPanelManager.Instance.Register(this);
        }
    }

    protected virtual void OnDisable()
    {
        if (UIPanelManager.Instance != null)
            UIPanelManager.Instance.Unregister(PanelId);
    }

    #endregion

    #region Virtual Hooks

    /// <summary>
    /// Called after the panel is shown. Override for subclass-specific logic
    /// (e.g. refresh data, play animation, request focus).
    /// </summary>
    protected virtual void OnShow() { }

    /// <summary>
    /// Called before the panel is hidden. Override to clean up
    /// (e.g. cancel drag, hide tooltips, clear selection).
    /// </summary>
    protected virtual void OnHide() { }

    #endregion

    #region Helpers

    /// <summary>
    /// Access the root panel GameObject (for subclasses that need direct reference).
    /// </summary>
    protected GameObject MainPanel => mainPanel;

    #endregion
}
