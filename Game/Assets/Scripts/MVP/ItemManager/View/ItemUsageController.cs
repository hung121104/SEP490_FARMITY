using UnityEngine;
using CombatManager.Presenter;

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
    private ItemData _currentItem;

    private void Start() => TrySubscribe();

    private void OnEnable()
    {
        MaterialCatalogService.OnReady += OnMaterialCatalogReady;
    }

    private void OnDisable()
    {
        MaterialCatalogService.OnReady -= OnMaterialCatalogReady;
    }

    /// <summary>
    /// Called once when MaterialCatalogService finishes loading all sheets.
    /// Re-applies tool appearance in case it was skipped while the catalog was loading.
    /// </summary>
    private void OnMaterialCatalogReady()
    {
        if (_currentItem != null)
            HandleSelectedItemChanged(_currentItem);
    }

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
        presenter.OnSelectedItemChanged += HandleSelectedItemChanged;
        isSubscribed = true;
        Debug.Log("ItemUsageController: Subscribed to Hotbar");
    }

    private void HandleSelectedItemChanged(ItemData item)
    {
        _currentItem = item;

        if (item is WeaponData weapon)
        {
            WeaponEquipPresenter.Instance?.EquipWeapon(weapon);
            return;
        }

        if (WeaponEquipPresenter.Instance != null && WeaponEquipPresenter.Instance.IsWeaponEquipped())
        {
            WeaponEquipPresenter.Instance.UnequipWeapon();
        }

        if (item is ToolData tool)
        {
            string configId = MaterialCatalogService.Instance?.GetMaterial(tool.toolMaterialId)?.materialId;
            if (!string.IsNullOrEmpty(configId))
            {
                var localPlayer = FindLocalPlayer();
                if (localPlayer == null) return;
                var sync = localPlayer.GetComponent<PlayerAppearanceSync>();
                if (sync != null) sync.SetTool(configId);
                return;
            }

            // Tool has a materialId but catalog isn't ready yet — defer without
            // clearing the tool layer. OnMaterialCatalogReady() will re-trigger.
            if (!string.IsNullOrEmpty(tool.toolMaterialId)) return;
        }

        // No tool selected (or tool has no materialId) — clear the tool layer
        var lp = FindLocalPlayer();
        if (lp == null) return;
        var s = lp.GetComponent<PlayerAppearanceSync>();
        if (s != null) s.SetTool("");
    }

    private void HandleItemUsed(ItemData item, Vector3 targetPosition, int inventorySlotIndex)
    {
        Debug.Log("ItemUsageController: Using " + item.itemName + " at " + targetPosition);

        // Update tool layer sprite before the action plays
        SyncToolAppearance(item);

        switch (item.itemType)
        {
            case ItemType.Seed:
                itemUsagePresenter.UseSeed(item, targetPosition);
                break;

            case ItemType.Tool:
                itemUsagePresenter.UseTool(item, targetPosition);
                break;

            case ItemType.Fertilizer:
                void OnFertilizeResult(bool success)
                {
                    CropFertilizingView.OnFertilizeResult -= OnFertilizeResult;
                    Debug.Log($"[ItemUsageController] Fertilizer result: {(success ? "SUCCESS" : "FAILED")}");
                    if (success)
                        presenter.ConsumeCurrentItem(1);
                }
                CropFertilizingView.OnFertilizeResult += OnFertilizeResult;
                bool fertilizerEventFired = itemUsagePresenter.UseFertilizer(item, targetPosition);
                if (!fertilizerEventFired)
                {
                    Debug.LogWarning($"[ItemUsageController] Fertilizer event was NOT fired. " +
                        $"Item runtime type: {item.GetType().Name} (expected FertilizerData). " +
                        $"Check: 1) Item has itemType=Fertilizer in catalog, " +
                        $"2) CropFertilizingView exists in scene and is enabled.");
                    CropFertilizingView.OnFertilizeResult -= OnFertilizeResult;
                }
                break;

            case ItemType.Consumable:
                var (consumed, amount) = itemUsagePresenter.UseConsumable(item, targetPosition);
                if (consumed && amount > 0)
                    presenter.ConsumeCurrentItem(amount);
                break;

            case ItemType.Weapon:
                itemUsagePresenter.UseWeapon(item, targetPosition);
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
        MaterialCatalogService.OnReady -= OnMaterialCatalogReady;
        if (presenter != null && isSubscribed)
        {
            presenter.OnItemUsed -= HandleItemUsed;
            presenter.OnSelectedItemChanged -= HandleSelectedItemChanged;
        }
    }

    /// <summary>
    /// Updates the tool paper-doll layer to match the item being used.
    /// Looks up the tool's materialId in MaterialCatalogService; materialId
    /// is also the SkinCatalogManager configId registered on startup.
    /// </summary>
    private void SyncToolAppearance(ItemData item)
    {
        if (item is not ToolData tool) return;

        string configId = MaterialCatalogService.Instance?.GetMaterial(tool.toolMaterialId)?.materialId;
        if (string.IsNullOrEmpty(configId)) return;

        var localPlayer = FindLocalPlayer();
        if (localPlayer == null) return;

        var sync = localPlayer.GetComponent<PlayerAppearanceSync>();
        if (sync != null)
            sync.SetTool(configId);
    }

    private static GameObject _cachedLocalPlayer;

    private static GameObject FindLocalPlayer()
    {
        // Validate cache — destroyed objects pass != null in some editor contexts
        if (_cachedLocalPlayer != null && _cachedLocalPlayer) return _cachedLocalPlayer;
        _cachedLocalPlayer = null;

        foreach (var go in GameObject.FindGameObjectsWithTag("PlayerEntity"))
        {
            var pv = go.GetComponent<Photon.Pun.PhotonView>();
            if (pv != null && pv.IsMine)
            {
                _cachedLocalPlayer = go;
                return go;
            }
        }
        return null;
    }
}
