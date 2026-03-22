using UnityEngine;

/// <summary>
/// Interface for any structure that supports player interaction.
/// Attach to structure prefab GameObjects alongside a Trigger Collider.
/// Defines the common contract shared by all interactable structures
/// (e.g., CraftingTable, CookingTable).
/// </summary>
public interface IInteractable
{
    // ── Properties ──────────────────────────────────────────────────────

    /// <summary>
    /// Whether the player is currently within the interaction trigger zone.
    /// </summary>
    bool IsPlayerInRange { get; }

    /// <summary>
    /// Whether this structure is currently targeted (player in range AND mouse hovering).
    /// </summary>
    bool IsTargeted { get; }

    /// <summary>
    /// The offset applied to the highlight object when this structure is targeted.
    /// </summary>
    Vector3 HighlightOffset { get; }

    // ── Interaction ─────────────────────────────────────────────────────

    /// <summary>
    /// Called when the player presses the interaction key (e.g., E) while
    /// the structure is targeted (in range + hover). Opens the corresponding UI.
    /// </summary>
    void Interact();

    // ── UI Lifecycle ────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if this structure's associated UI panel is currently open.
    /// </summary>
    bool IsUIOpen();

    /// <summary>
    /// Opens the UI panel associated with this structure.
    /// </summary>
    void OpenUI();

    /// <summary>
    /// Closes the UI panel associated with this structure.
    /// </summary>
    void CloseUI();

    // ── Object-Pool Support ─────────────────────────────────────────────

    /// <summary>
    /// Marks this structure as being returned to the object pool,
    /// so that OnDisable skips the normal cleanup logic.
    /// </summary>
    void SetBeingPooled();
}
