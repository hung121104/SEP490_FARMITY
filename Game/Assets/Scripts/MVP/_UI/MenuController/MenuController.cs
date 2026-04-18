using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toggles the inventory/menu canvas via the New Input System.
/// Replaces the legacy Input.GetKeyDown(useMenu) approach.
/// </summary>
public class MenuController : MonoBehaviour
{
    public static MenuController Instance { get; private set; }

    public GameObject menuCanvas;
    public GameObject hotbarPanel;
    public InventoryGameView inventoryGameView;

    public bool IsMenuOpen => menuCanvas != null && menuCanvas.activeSelf;

    /// <summary>Frame number when ESC closed the menu. Used to prevent settings menu from opening on the same frame.</summary>
    public int LastEscCloseFrame { get; private set; } = -1;

    private InputAction _escCloseAction;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OpenInventory.performed += OnToggleMenu;

        if (_escCloseAction == null)
        {
            _escCloseAction = new InputAction("MenuCloseESC", InputActionType.Button, "<Keyboard>/escape");
            _escCloseAction.performed += OnEscPressed;
        }
        _escCloseAction.Enable();
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OpenInventory.performed -= OnToggleMenu;

        _escCloseAction?.Disable();
    }

    private void OnDestroy()
    {
        if (_escCloseAction != null)
        {
            _escCloseAction.performed -= OnEscPressed;
            _escCloseAction.Dispose();
            _escCloseAction = null;
        }
    }

    void Start()
    {
        menuCanvas.SetActive(false);
    }

    private void OnToggleMenu(InputAction.CallbackContext ctx)
    {
        if (menuCanvas == null || hotbarPanel == null || inventoryGameView == null)
            return;

        // If a structure UI is open, close it instead of toggling inventory
        if (InteractableStructureBase.ActiveStructure != null)
        {
            InteractableStructureBase.CloseActiveStructure();
            return;
        }

        // Block open main menu when open orther UI Inv
        if (!menuCanvas.activeSelf && inventoryGameView.IsInventoryInUseByOtherUI())
        {
            Debug.Log("[MenuController] Blocked Main Menu toggle because inventory is currently in use by another UI.");
            return;
        }

        hotbarPanel.SetActive(menuCanvas.activeSelf);

        //Force cancel all ongoing actions in inventory when menu is toggled
        menuCanvas.SetActive(!menuCanvas.activeSelf);

        if (menuCanvas.activeSelf == false)
        {
            inventoryGameView.CloseInventory();
            UILayerManager.Instance?.NotifyPanelClosed();
        }
        else
        {
            inventoryGameView.OpenInventory();
            UILayerManager.Instance?.NotifyPanelOpened();
        }
    }

    private void OnEscPressed(InputAction.CallbackContext ctx)
    {
        if (menuCanvas == null || !menuCanvas.activeSelf) return;

        // Don't intercept ESC if a structure UI is open (structure handles its own ESC)
        if (InteractableStructureBase.ActiveStructure != null) return;

        hotbarPanel.SetActive(true);
        menuCanvas.SetActive(false);
        inventoryGameView.CloseInventory();
        UILayerManager.Instance?.NotifyPanelClosed();
        LastEscCloseFrame = Time.frameCount;
    }
}
