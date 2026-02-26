using UnityEngine;

public class ItemUsageController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotbarView hotbarView;

    [Header("Settings")]
    [SerializeField] private LayerMask farmableGroundLayer;
    [SerializeField] private LayerMask targetLayer;

    private HotbarPresenter presenter;
    private ItemUsagePresenter itemUsagePresenter;
    private bool isSubscribed = false;

    private void Start() => TrySubscribe();

    private void TrySubscribe()
    {
        if (isSubscribed) return;

        if (hotbarView == null)
            hotbarView = FindFirstObjectByType<HotbarView>();

        if (hotbarView == null || !hotbarView.IsInitialized())
        {
            Invoke(nameof(TrySubscribe), 0.2f);
            return;
        }

        presenter = hotbarView.GetPresenter();
        if (presenter == null)
        {
            Invoke(nameof(TrySubscribe), 0.2f);
            return;
        }

        // Build the service chain for tools (and consumables/weapons when ready)
        itemUsagePresenter = new ItemUsagePresenter(new ItemUsageService(new UseToolService()));

        presenter.OnItemUsed += HandleItemUsed;
        isSubscribed = true;
        Debug.Log("ItemUsageController: Subscribed to Hotbar");
    }

    private void HandleItemUsed(ItemDataSO item, Vector3 targetPosition, int inventorySlotIndex)
    {
        Debug.Log("ItemUsageController: Using " + item.itemName + " at " + targetPosition);

        switch (item.GetItemType())
        {
            case ItemType.Seed:
                // Routed through ItemUsageService → UseSeedService → fires OnSeedRequested
                itemUsagePresenter.UseSeed(item, targetPosition);
                break;

            case ItemType.Tool:
                // UseToolService fires per-tool events (OnHoeRequested, etc.)
                // that the relevant Views subscribe to
                itemUsagePresenter.UseTool(item, targetPosition);
                break;

            case ItemType.Consumable:
                var (consumed, amount) = itemUsagePresenter.UseConsumable(item, targetPosition);
                if (consumed && amount > 0)
                    presenter.ConsumeCurrentItem(amount);
                break;

            case ItemType.Weapon:
                if (itemUsagePresenter.UseWeapon(item, targetPosition))
                    presenter.ConsumeCurrentItem(1);
                break;

            default:
                Debug.LogWarning("No handler for item type: " + item.GetItemType());
                break;
        }
    }

    private void OnDestroy()
    {
        if (presenter != null && isSubscribed)
            presenter.OnItemUsed -= HandleItemUsed;
    }
}
