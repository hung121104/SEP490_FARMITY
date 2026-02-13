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
    event Action<int, Vector2> OnSlotHoverEnter;
    event Action<int> OnSlotHoverExit;
    event Action<int> OnItemDeleteRequested;

    // Slot operations
    void UpdateSlot(int slotIndex, ItemModel item);
    void ClearSlot(int slotIndex);

    // Drag operations
    void ShowDragPreview(ItemModel item);
    void UpdateDragPreview(Vector2 position);
    void HideDragPreview();

    // Notifications
    void ShowNotification(string message);

    //Close operations
    void CancelAllActions();
}
