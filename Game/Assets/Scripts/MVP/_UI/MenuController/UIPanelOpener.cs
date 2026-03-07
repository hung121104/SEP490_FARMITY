using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to any world GameObject (crafting table, cooking station, chest...)
/// to open a specific UI panel when the player interacts with it.
///
/// SETUP:
///   1. Add a Collider with IsTrigger = true to the GameObject.
///   2. Set Panel Id to match the target IUIPanel.PanelId (e.g. "Crafting").
///   3. (Optional) Assign an Interact Action (e.g. Player/Interact from InputSystem_Actions).
///   4. (Optional) Assign an Interaction Prompt to show/hide when player is in range.
///
/// ALTERNATIVE — call Interact() from another script (e.g. your existing NPCInteractor pattern):
///   GetComponent<UIPanelOpener>().Interact();
/// </summary>
public class UIPanelOpener : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Must match the PanelId of the target IUIPanel (e.g. 'Crafting', 'Cooking')")]
    [SerializeField] private string panelId;

    [Tooltip("If true, calling Interact() a second time will close the panel instead of ignoring.")]
    [SerializeField] private bool toggleOnInteract = true;

    [Header("Interaction Input (optional)")]
    [Tooltip("Input action to trigger interaction when player is in range. Leave empty if you call Interact() manually.")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Interaction Prompt (optional)")]
    [Tooltip("GameObject to show when player enters the trigger zone (e.g. 'Press E to craft').")]
    [SerializeField] private GameObject interactionPrompt;

    private bool playerInRange = false;

    private void Awake()
    {
        interactionPrompt?.SetActive(false);
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteractPerformed;
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.performed -= OnInteractPerformed;

        SetPromptVisible(false);
        playerInRange = false;
    }

    // ── Trigger detection ──────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = true;
        SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = false;
        SetPromptVisible(false);
    }

    // 2D physics variant
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = true;
        SetPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = false;
        SetPromptVisible(false);
    }

    // ── Input ──────────────────────────────────────────────────────────────────

    private void OnInteractPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (playerInRange)
            Interact();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Open (or toggle) the target panel. Call this from any external script.
    /// </summary>
    public void Interact()
    {
        if (UIPanelManager.Instance == null) return;
        if (string.IsNullOrEmpty(panelId)) return;

        if (toggleOnInteract)
            UIPanelManager.Instance.Toggle(panelId);
        else
            UIPanelManager.Instance.Open(panelId);
    }

    /// <summary>
    /// Explicitly close the panel this opener controls.
    /// </summary>
    public void ClosePanel()
    {
        UIPanelManager.Instance?.Close(panelId);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool IsPlayer(Component other)
    {
        return other.CompareTag("Player");
    }

    private void SetPromptVisible(bool visible)
    {
        interactionPrompt?.SetActive(visible);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(panelId))
            Debug.LogWarning($"[UIPanelOpener] '{name}': Panel Id is not set.", this);
    }
#endif
}
