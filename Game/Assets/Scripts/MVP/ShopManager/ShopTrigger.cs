using UnityEngine;
using System.Collections.Generic; // Thêm thư viện này

[RequireComponent(typeof(Collider2D))]
public class ShopTrigger : MonoBehaviour
{
    [Header("Shop Settings")]
    public List<ItemType> npcShopTypes = new List<ItemType>();

    private bool _isPlayerInRange = false;

    private void Update()
    {
        if (_isPlayerInRange && InputManager.Instance.Interact.WasPressedThisFrame())
        {
            if (ShopView.Instance != null && ShopView.Instance.IsVisible)
                ShopSystemManager.Instance.CloseShopUI();
            else
                ShopSystemManager.Instance.OpenShopUI(npcShopTypes); 
        }
    }

    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("PlayerEntity")) _isPlayerInRange = true; }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerEntity"))
        {
            _isPlayerInRange = false;
            ShopSystemManager.Instance.CloseShopUI();
        }
    }
}