using System;
using UnityEngine;

/// <summary>
/// View interface for the chest inventory panel.
/// ChestPresenter communicates with the chest UI through this interface.
/// </summary>
public interface IChestView
{
    bool IsVisible { get; }

    // Events to Presenter
    event Action<int> OnSlotClicked;
    event Action<int> OnSlotBeginDrag;
    event Action<Vector2> OnSlotDrag;
    event Action OnSlotEndDrag;
    event Action<int> OnSlotDrop;
    event Action<int, Vector2> OnSlotHoverEnter;
    event Action<int> OnSlotHoverExit;

    // Slot operations
    void InitializeSlots(int count);
    void UpdateSlot(int slotIndex, ItemModel item);
    void ClearSlot(int slotIndex);
    void SetSlotLocked(int slotIndex, bool locked);

    // Drag operations
    void ShowDragPreview(ItemModel item);
    void UpdateDragPreview(Vector2 position);
    void HideDragPreview();

    // Visibility
    void Show();
    void Hide();

    // Cleanup
    void CancelAllActions();
}
