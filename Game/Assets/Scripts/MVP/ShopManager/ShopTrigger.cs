using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShopTrigger : MonoBehaviour
{
    [Header("Shop Settings")]
    public ItemType npcShopType;

    // Các biến private, tự động tìm
    private InventoryGameView playerInventoryView;
    private TimeManagerView timeManager;

    private bool _isPlayerInRange = false;
    private IShopService _shopService;
    private ShopPresenter _presenter;

    private void Start()
    {
        _shopService = new ShopService(npcShopType);

    
        playerInventoryView = UnityEngine.Object.FindFirstObjectByType<InventoryGameView>(FindObjectsInactive.Include);
        timeManager = UnityEngine.Object.FindFirstObjectByType<TimeManagerView>(FindObjectsInactive.Include);

        if (timeManager != null) timeManager.OnDayChanged += HandleDayChanged;
    }

    private void HandleDayChanged()
    {
        _shopService.GenerateDailyItems();

        // Gọi thẳng ShopView.Instance một cách an toàn
        if (ShopView.Instance != null && ShopView.Instance.IsVisible && _presenter != null)
        {
            ShopView.Instance.UpdateShopSlots(_shopService.GetShopModel().DailyItems);
        }
    }

    private void Update()
    {
        if (_isPlayerInRange && InputManager.Instance.Interact.WasPressedThisFrame())
        {
            // Kiểm tra trạng thái của ShopPanel
            if (ShopView.Instance != null && ShopView.Instance.IsVisible)
                CloseShop();
            else
                OpenShop();
        }
    }

    private void OpenShop()
    {
        if (ShopView.Instance == null || playerInventoryView == null) return;

        ShopView.Instance.SetVisible(true);

        _presenter = new ShopPresenter(ShopView.Instance, _shopService, playerInventoryView);
    }

    private void CloseShop()
    {
        if (ShopView.Instance != null) ShopView.Instance.SetVisible(false);

        if (_presenter != null)
        {
            _presenter.Dispose(); 
            _presenter = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerEntity")) _isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerEntity"))
        {
            _isPlayerInRange = false;

            
            if (ShopView.Instance != null && ShopView.Instance.IsVisible)
            {
                CloseShop();
            }
        }
    }
}