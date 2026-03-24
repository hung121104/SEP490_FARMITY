using System.Collections;

/// <summary>
/// Defines the Unity-specific callbacks that NPCInteractor (MonoBehaviour)
/// provides back to the pure-C# NPCInteractionPresenter.
/// The Presenter never inherits MonoBehaviour — it calls through this interface instead.
/// </summary>
public interface INPCInteractorView
{
    /// <summary>Disable the player's movement component.</summary>
    void LockPlayer();

    /// <summary>Re-enable the player's movement component.</summary>
    void UnlockPlayer();

    /// <summary>Enable or disable the hotbar MonoBehaviour script.</summary>
    void EnableHotbar(bool enable);

    /// <summary>Show/hide the inventory menu root GameObject.</summary>
    void SetInventoryMenuRoot(bool active);

    /// <summary>Open the inventory panel through InventoryGameView.</summary>
    void OpenInventory();

    /// <summary>Close the inventory panel through InventoryGameView.</summary>
    void CloseInventory();

    /// <summary>Call InventoryGameView.NotifyExternalAction to reset sync cooldown.</summary>
    void NotifyExternalAction();

    /// <summary>Return the live IInventoryService from InventoryGameView.</summary>
    IInventoryService GetInventoryService();

    /// <summary>
    /// Host an IEnumerator coroutine on the MonoBehaviour.
    /// The Presenter owns the coroutine logic; the View runs it.
    /// </summary>
    void StartPresenterCoroutine(IEnumerator coroutine);
}
