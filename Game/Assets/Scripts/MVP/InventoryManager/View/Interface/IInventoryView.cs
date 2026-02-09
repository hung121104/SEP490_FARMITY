using System;
using UnityEngine;

public interface IInventoryView
{
    // State
    bool IsVisible { get; }

    // Events to Presenter
    event Action<int> OnSlotClicked;
    event Action<int> OnSlotBeginDrag;
    event Action<Vector2> OnSlotDrag;
    event Action OnSlotEndDrag;
    event Action<int> OnSlotDrop;
    event Action<int> OnUseItemRequested;
    event Action<int> OnDropItemRequested;
    event Action OnSortRequested;

    // Display Methods
    void UpdateSlot(int slotIndex, InventoryItem item);
    void ClearSlot(int slotIndex);
    void ShowItemDetails(InventoryItem item);
    void HideItemDetails();
    void ShowDragPreview(InventoryItem item);
    void UpdateDragPreview(Vector2 position);
    void HideDragPreview();
    void ShowNotification(string message);
}
