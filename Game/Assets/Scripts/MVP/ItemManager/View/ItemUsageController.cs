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

    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (isSubscribed) return;

        if (hotbarView == null)
        {
            hotbarView = FindFirstObjectByType<HotbarView>();
        }

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

        itemUsagePresenter = new ItemUsagePresenter(new ItemUsageService(new UseToolService()));
        presenter.OnItemUsed += HandleItemUsed;
        isSubscribed = true;
        Debug.Log("ItemUsageController: Subscribed to Hotbar");
    }

    // Handler for item usage events
    private void HandleItemUsed(ItemDataSO item, Vector3 targetPosition, int inventorySlotIndex)
    {
        Debug.Log("ItemUsageController: Using " + item.itemName + " at " + targetPosition);

        bool consumed = false;
        int amount = 0;

        switch (item.GetItemType())
        {
            case ItemType.Tool:
                consumed = UseTool(item, targetPosition);
                break;

            case ItemType.Seed:
                (consumed, amount) = UseSeed(item, targetPosition);
                break;

            case ItemType.Consumable:
                (consumed, amount) = UseConsumable(item, targetPosition);
                break;

            case ItemType.Weapon:
                consumed = UseWeapon(item, targetPosition);
                break;

            default:
                Debug.LogWarning("No handler for item type: " + item.GetItemType());
                break;
        }

        if (consumed && amount > 0)
        {
            presenter.ConsumeCurrentItem(amount);
        }
    }

    //Add specific item usage implementations below
    private bool UseTool(ItemDataSO item, Vector3 pos)
    {
        return itemUsagePresenter.UseTool(item, pos);
    }

    private (bool, int) UseSeed(ItemDataSO item, Vector3 pos)
    {
        return itemUsagePresenter.UseSeed(item,pos);
    }

    private (bool, int) UseConsumable(ItemDataSO item, Vector3 pos)
    {
         return itemUsagePresenter.UseConsumable(item,pos);
    }

    private bool UseWeapon(ItemDataSO item, Vector3 pos)
    {
        return itemUsagePresenter.UseWeapon(item,pos);
    }

    private void OnDestroy()
    {
        if (presenter != null && isSubscribed)
        {
            presenter.OnItemUsed -= HandleItemUsed;
        }
    }
}
