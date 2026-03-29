using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

[RequireComponent(typeof(Collider2D))]
public class ShopTrigger : MonoBehaviour
{
    [Header("Shop Settings")]
    public List<ItemType> npcShopTypes = new List<ItemType>();

    private bool _isLocalPlayerInRange = false;

    private void Update()
    {
        if (_isLocalPlayerInRange && InputManager.Instance.Interact.WasPressedThisFrame())
        {
            if (ShopView.Instance != null && ShopView.Instance.IsVisible)
                ShopSystemManager.Instance.CloseShopUI();
            else
                ShopSystemManager.Instance.OpenShopUI(npcShopTypes);
        }
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
        ShopSystemManager.Instance.CloseShopUI();
    }
}