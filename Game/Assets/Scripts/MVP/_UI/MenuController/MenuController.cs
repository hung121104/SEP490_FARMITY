using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toggles the inventory/menu canvas via the New Input System.
/// Replaces the legacy Input.GetKeyDown(useMenu) approach.
/// </summary>
public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject hotbarPanel;
    public InventoryGameView inventoryGameView;

    private void OnEnable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OpenInventory.performed += OnToggleMenu;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OpenInventory.performed -= OnToggleMenu;
    }

    void Start()
    {
        menuCanvas.SetActive(false);
    }

    private void OnToggleMenu(InputAction.CallbackContext ctx)
    {
        if (menuCanvas == null || hotbarPanel == null || inventoryGameView == null)
            return;

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
        }
        else
        {
            inventoryGameView.OpenInventory();
        }
    }
}
