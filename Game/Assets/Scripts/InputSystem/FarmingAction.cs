using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Example gameplay script showing how to consume the NEW Input System
/// instead of using Input.GetKeyDown(KeyCode.F).
///
/// ──────────────────────────────────────────────────
/// BEFORE (legacy – scattered hardcoded key):
///     void Update() {
///         if (Input.GetKeyDown(KeyCode.F))  Harvest();
///         if (Input.GetKeyDown(KeyCode.E))  Interact();
///     }
///
/// AFTER (New Input System – event-driven):
///     Subscribe in OnEnable / Unsubscribe in OnDisable.
///     Zero key references in this file.
/// ──────────────────────────────────────────────────
/// </summary>
public class FarmingAction : MonoBehaviour
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Subscribe / Unsubscribe
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OnEnable()
    {
        // Guard: InputManager might not exist yet in some scene-load orders
        if (InputManager.Instance == null) return;

        InputManager.Instance.Harvest.performed  += OnHarvest;
        InputManager.Instance.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        if (InputManager.Instance == null) return;

        InputManager.Instance.Harvest.performed  -= OnHarvest;
        InputManager.Instance.Interact.performed -= OnInteract;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Callbacks
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OnHarvest(InputAction.CallbackContext ctx)
    {
        // ⮕ Replace this with your actual harvest logic
        Debug.Log("[FarmingAction] Harvest performed!");
        // e.g. TryHarvestCrop();
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        // ⮕ Replace this with your actual interact logic
        Debug.Log("[FarmingAction] Interact performed!");
        // e.g. InteractWithNearbyObject();
    }
}
