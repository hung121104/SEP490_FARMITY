using UnityEngine;

public class HotbarPresenter
{
    private readonly HotbarModel model;
    private readonly HotbarView view;
    private readonly IHotbarService service;

    public HotbarPresenter(HotbarModel model, HotbarView view, IHotbarService service)
    {
        this.model = model;
        this.view = view;
        this.service = service;

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        // Subscribe to View input events
        view.OnSlotKeyPressed += HandleSlotSelection;
        view.OnScrollInput += HandleScrollInput;
        view.OnUseItemInput += HandleUseItem;
        view.OnUseItemAlternateInput += HandleUseItemAlternate;

        // Subscribe to Model events for UI updates
        model.OnSlotIndexChanged += view.UpdateSelection;
        model.OnSlotContentChanged += view.UpdateSlotDisplay;
        model.OnItemUsed += HandleItemUsage;
        model.OnItemUsedAlternate += HandleItemUsageAlternate;
    }

    public void UnsubscribeEvents()
    {
        // Unsubscribe from View
        view.OnSlotKeyPressed -= HandleSlotSelection;
        view.OnScrollInput -= HandleScrollInput;
        view.OnUseItemInput -= HandleUseItem;
        view.OnUseItemAlternateInput -= HandleUseItemAlternate;

        // Unsubscribe from Model
        model.OnSlotIndexChanged -= view.UpdateSelection;
        model.OnSlotContentChanged -= view.UpdateSlotDisplay;
        model.OnItemUsed -= HandleItemUsage;
        model.OnItemUsedAlternate -= HandleItemUsageAlternate;
    }

    #region Input Handlers

    private void HandleSlotSelection(int slotIndex)
    {
        model.SelectSlot(slotIndex);
    }

    private void HandleScrollInput(float direction)
    {
        int currentIndex = model.CurrentSlotIndex;
        int newIndex;

        if (direction > 0f) // Previous slot
            newIndex = (currentIndex - 1 + model.HotbarSize) % model.HotbarSize;
        else // Next slot
            newIndex = (currentIndex + 1) % model.HotbarSize;

        model.SelectSlot(newIndex);
    }

    private void HandleUseItem()
    {
        model.UseCurrentItem();
    }

    private void HandleUseItemAlternate()
    {
        model.UseCurrentItemAlternate();
    }

    #endregion

    #region Business Logic

    private async void HandleItemUsage(ItemDataSO item, int slotIndex)
    {
        Vector3 targetPosition = view.GetMouseWorldPosition();
        Debug.Log($"✨ Using: {item.itemName} at position: {targetPosition}");

        if (service != null)
        {
            // Call service layer
            var result = await service.UseItemAsync(item, slotIndex, targetPosition);

            // Handle result - consume item if needed
            if (result.WasConsumed)
            {
                model.RemoveItemFromSlot(slotIndex, result.ConsumedAmount);
                Debug.Log($"➖ Consumed {result.ConsumedAmount}x {item.itemName}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ HotbarService not available!");
        }
    }

    private async void HandleItemUsageAlternate(ItemDataSO item, int slotIndex)
    {
        Vector3 targetPosition = view.GetMouseWorldPosition();
        Debug.Log($"🔄 Alternate use: {item.itemName} at position: {targetPosition}");

        // For now, same as primary usage - you can implement different logic
        if (service != null)
        {
            var result = await service.UseItemAsync(item, slotIndex, targetPosition);
            if (result.WasConsumed)
            {
                model.RemoveItemFromSlot(slotIndex, result.ConsumedAmount);
            }
        }
    }

    #endregion

    #region Public API

    public void Initialize()
    {
        view.Initialize(model.HotbarSize);
        view.RefreshAll(model.Slots);
    }

    public bool AddItem(int slotIndex, ItemDataSO item, int quantity = 1)
    {
        return model.AddItemToSlot(slotIndex, item, quantity);
    }

    public bool RemoveItem(int slotIndex, int quantity = 1)
    {
        return model.RemoveItemFromSlot(slotIndex, quantity);
    }

    public void ClearHotbar()
    {
        model.ClearHotbar();
        view.RefreshAll(model.Slots);
    }

    public void SwapSlots(int indexA, int indexB)
    {
        model.SwapSlots(indexA, indexB);
    }

    #endregion
}
