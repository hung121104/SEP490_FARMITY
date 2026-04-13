using System;

/// <summary>
/// Shared static state for Minecraft-style click-to-carry inventory interaction.
/// Tracks whether an item is currently being carried and coordinates between multiple inventory views.
/// </summary>
public static class InventoryCarryState
{
    public static bool IsCarrying { get; private set; }
    public static int SourceSlot { get; private set; } = -1;

    /// <summary>
    /// Set to true by any InventorySlotView.OnPointerDown to indicate
    /// a slot was interacted with this frame. Reset each frame by the carrying view.
    /// </summary>
    public static bool SlotInteractedThisFrame;

    private static Action endCarryCallback;

    public static void StartCarry(int slot, Action onEndCarry)
    {
        IsCarrying = true;
        SourceSlot = slot;
        endCarryCallback = onEndCarry;
    }

    public static void EndCarry()
    {
        var cb = endCarryCallback;
        IsCarrying = false;
        SourceSlot = -1;
        endCarryCallback = null;
        cb?.Invoke();
    }

    public static void Reset()
    {
        IsCarrying = false;
        SourceSlot = -1;
        endCarryCallback = null;
        SlotInteractedThisFrame = false;
    }
}
