using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example: toggling the inventory panel via the New Input System.
///
/// BEFORE (legacy):
///     if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventory();
///
/// AFTER (New Input System):
///     Subscribe to OpenInventory.performed.
/// </summary>
public class InventoryOpenExample : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    private void OnEnable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OpenInventory.performed += OnToggleInventory;
    }

    private void OnDisable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.OpenInventory.performed -= OnToggleInventory;
    }

    private void OnToggleInventory(InputAction.CallbackContext ctx)
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            Debug.Log($"[InventoryOpen] Panel is now {(inventoryPanel.activeSelf ? "open" : "closed")}.");
        }
    }
}
