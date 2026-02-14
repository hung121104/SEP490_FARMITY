using UnityEngine;

public class ItemUsageController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotbarView hotbarView;

    [Header("Settings")]
    [SerializeField] private LayerMask farmableGroundLayer;
    [SerializeField] private LayerMask targetLayer;

    private HotbarPresenter presenter;
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
        Debug.Log("Using tool: " + item.itemName);

        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero, 0.1f, targetLayer);
        if (hit.collider != null)
        {
            Debug.Log("Hit: " + hit.collider.name);
        }

        return false;
    }

    private (bool, int) UseSeed(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("Planting seed: " + item.itemName);

        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero, 0.1f, farmableGroundLayer);
        if (hit.collider != null)
        {
            Debug.Log("Planted on: " + hit.collider.name);
            return (true, 1);
        }

        Debug.LogWarning("Cannot plant here");
        return (false, 0);
    }

    private (bool, int) UseConsumable(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("Consuming: " + item.itemName);
        return (true, 1);
    }

    private bool UseWeapon(ItemDataSO item, Vector3 pos)
    {
        Debug.Log("Using weapon: " + item.itemName);
        return false;
    }

    private void OnDestroy()
    {
        if (presenter != null && isSubscribed)
        {
            presenter.OnItemUsed -= HandleItemUsed;
        }
    }
}
