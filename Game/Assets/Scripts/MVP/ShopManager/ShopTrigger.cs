using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShopTrigger : MonoBehaviour
{
    public ItemType npcShopType;
    private bool _isPlayerInRange = false;

    private void Update()
    {
        if (_isPlayerInRange && InputManager.Instance.Interact.WasPressedThisFrame())
        {
            if (ShopView.Instance != null && ShopView.Instance.IsVisible)
                ShopSystemManager.Instance.CloseShopUI();
            else
                ShopSystemManager.Instance.OpenShopUI(npcShopType);
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