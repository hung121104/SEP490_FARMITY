using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
public class ShopTrigger : MonoBehaviour
{
    [Header("Shop Settings")]
    public List<ItemType> npcShopTypes = new List<ItemType>();

    private bool _isLocalPlayerInRange = false;
    private bool _isShopOpenedByThis = false;
    private bool _closeInputSubscribed = false;

    // ── Unity Lifecycle ──────────────────────────────────────────────────

    private void Update()
    {
        if (!_isLocalPlayerInRange) return;
        if (InputManager.Instance == null) return;
        if (!InputManager.Instance.Interact.WasPressedThisFrame()) return;

        // Block opening while any structure UI is already active.
        if (InteractableStructureBase.ActiveStructure != null) return;

        // Block opening while the full inventory/menu canvas is open.
        if (MenuController.Instance != null && MenuController.Instance.IsMenuOpen) return;

        // Shop already open from this trigger — ignore (close only via Inventory key).
        if (_isShopOpenedByThis) return;

        OpenShop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;
        var pv = other.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) return; // Bỏ qua remote player
        _isLocalPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerEntity")) return;
        var pv = other.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine) return; // Bỏ qua remote player
        _isLocalPlayerInRange = false;
        CloseShop();
    }

    private void OnDisable()
    {
        CloseShop();
    }

    // ── Open / Close with player-movement lock ──────────────────────────

    private void OpenShop()
    {
        if (ShopSystemManager.Instance == null) return;

        ShopSystemManager.Instance.OpenShopUI(npcShopTypes);
        _isShopOpenedByThis = true;

        // Block all player actions (movement, attack, interact, …) while shop is open.
        InputManager.Instance?.DisablePlayerActions();

        // Only OpenInventory key can close the shop.
        SubscribeCloseInput();
    }

    private void CloseShop()
    {
        if (!_isShopOpenedByThis) return;
        _isShopOpenedByThis = false;

        if (ShopSystemManager.Instance != null)
            ShopSystemManager.Instance.CloseShopUI();

        UnsubscribeCloseInput();

        // Re-enable player actions after shop closes.
        InputManager.Instance?.EnablePlayerActions();
    }

    // ── Close-input subscription (OpenInventory only) ───────────────────

    private void SubscribeCloseInput()
    {
        if (_closeInputSubscribed) return;
        if (InputManager.Instance == null) return;

        // Re-enable just this single action so its callback still fires
        // even though the rest of the Player map is disabled.
        InputManager.Instance.OpenInventory.Enable();
        InputManager.Instance.OpenInventory.performed += OnCloseInputPressed;

        _closeInputSubscribed = true;
    }

    private void UnsubscribeCloseInput()
    {
        if (!_closeInputSubscribed) return;

        if (InputManager.Instance != null)
            InputManager.Instance.OpenInventory.performed -= OnCloseInputPressed;

        _closeInputSubscribed = false;
    }

    private void OnCloseInputPressed(InputAction.CallbackContext ctx)
    {
        if (ShopView.Instance == null || !ShopView.Instance.IsVisible) return;
        CloseShop();
    }
}
