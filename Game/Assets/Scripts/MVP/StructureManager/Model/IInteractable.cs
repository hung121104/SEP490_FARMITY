/// <summary>
/// Interface for any structure that supports player interaction.
/// Attach to structure prefab GameObjects alongside a Trigger Collider.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when the player presses the interaction key (e.g., F) while inside the trigger zone.
    /// </summary>
    void Interact();
}
