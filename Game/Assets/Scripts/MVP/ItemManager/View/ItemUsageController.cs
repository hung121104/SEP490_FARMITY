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

        itemUsagePresenter = new ItemUsagePresenter(new ItemUsageService(new UseToolService()));

        presenter.OnItemUsed += HandleItemUsed;
        isSubscribed = true;
        Debug.Log("ItemUsageController: Subscribed to Hotbar");
    }

    private void HandleItemUsed(ItemData item, Vector3 targetPosition, int inventorySlotIndex)
    {
        Debug.Log("ItemUsageController: Using " + item.itemName + " at " + targetPosition);

        switch (item.itemType)
        {
            case ItemType.Seed:
                itemUsagePresenter.UseSeed(item, targetPosition);
                break;

            case ItemType.Tool:
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

            case ItemType.Pollen:
                Debug.Log($"[ItemUsageController] Pollen use requested: '{item.itemName}' (id={item.itemID}, type={item.GetType().Name}) at {targetPosition}");
                // Fire the pollen event. CropBreedingView will raise OnBreedingResult
                // synchronously with true/false — consume only on success.
                void OnResult(bool success)
                {
                    CropBreedingView.OnBreedingResult -= OnResult;
                    Debug.Log($"[ItemUsageController] Pollen breeding result: {(success ? "SUCCESS" : "FAILED")}");
                    if (success)
                        presenter.ConsumeCurrentItem(1);
                }
                CropBreedingView.OnBreedingResult += OnResult;
                bool eventFired = itemUsagePresenter.UsePollen(item, targetPosition);
                // Guard: if the event was never fired (no CropBreedingView in scene), clean up
                if (!eventFired)
                {
                    Debug.LogWarning($"[ItemUsageController] Pollen event was NOT fired. " +
                        $"Item runtime type: {item.GetType().Name} (expected PollenData). " +
                        $"Check: 1) Item has itemType=Pollen in catalog, " +
                        $"2) CropBreedingView exists in scene and is enabled.");
                    CropBreedingView.OnBreedingResult -= OnResult;
                }
                break;

            case ItemType.Structure:
                // Toggle and placement handled entirely by StructureView via UseStructureService.OnStructureRequested
                // (mirrors the CropPlantingView / UseSeedService pattern)
                itemUsagePresenter.UseStructure(item, targetPosition);
                break;

            default:
                Debug.LogWarning("No handler for item type: " + item.itemType);
                break;
        }
    }

    private void OnDestroy()
    {
        if (presenter != null && isSubscribed)
            presenter.OnItemUsed -= HandleItemUsed;
    }
}
