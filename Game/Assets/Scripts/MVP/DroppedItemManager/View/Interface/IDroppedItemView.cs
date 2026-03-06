/// <summary>
/// Interface for the visual representation of a dropped item in the world.
/// DroppedItemView implements this interface for the visual representation of a dropped item.
/// </summary>
public interface IDroppedItemView
{
    /// <summary>Show the item visual (sprite, label, etc.).</summary>
    void ShowItem(DroppedItemData data);

    /// <summary>Hide the item visual entirely.</summary>
    void HideItem();

    /// <summary>Start the blink animation (alpha oscillation) for the last 30 seconds.</summary>
    void StartBlinking();

    /// <summary>Stop the blink animation and restore full alpha.</summary>
    void StopBlinking();

    /// <summary>Show or hide the "[F] Pick up" prompt.</summary>
    void SetPickupPromptVisible(bool visible);
}
