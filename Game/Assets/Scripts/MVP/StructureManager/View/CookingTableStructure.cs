using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Attached to the CookingTable prefab.
/// Listens for the Interact action (E key via InputManager) while the player
/// is inside the trigger collider, then toggles the Cooking UI.
/// Auto-closes when the player genuinely leaves the trigger zone.
/// </summary>
public class CookingTableStructure : MonoBehaviour, IInteractable
{

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private CraftingSystemManager craftingSystemManager;
    private Collider2D _triggerCollider;



    // Overlap counter instead of a plain bool.
    // Handles Unity physics wakeup events that may fire Enter/Exit repeatedly.
    private int _overlapCount = 0;
    private bool PlayerInRange => _overlapCount > 0;

    // Guard against double-subscribing.
    private bool _inputSubscribed = false;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        craftingSystemManager = FindFirstObjectByType<CraftingSystemManager>();
        SubscribeInput();


    }

    private void OnEnable()
    {
        SubscribeInput();
        StartCoroutine(VisibilityCheckRoutine());
    }

    private System.Collections.IEnumerator VisibilityCheckRoutine()
    {
        // Wait 2 frames to let physics engine trigger OnTriggerEnter2D 
        // if the player is already inside the newly spawned position.
        yield return null;
        yield return null;
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        _overlapCount = 0;
    }

    private void OnDestroy() => UnsubscribeInput();

    private void SubscribeInput()
    {
        if (_inputSubscribed) return;
        if (InputManager.Instance == null) return;
        InputManager.Instance.Interact.performed += OnInteract;
        _inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!_inputSubscribed) return;
        if (InputManager.Instance != null)
            InputManager.Instance.Interact.performed -= OnInteract;
        _inputSubscribed = false;
    }

    // ── Trigger zone ────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerEntity"))
        {
            _overlapCount++;

            if (showDebugLogs) Debug.Log($"[CookingTableStructure] Player entered trigger (overlap={_overlapCount})");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("PlayerEntity")) return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        if (showDebugLogs) Debug.Log($"[CookingTableStructure] OnTriggerExit2D received (overlap={_overlapCount})");

        if (_overlapCount == 0)
        {
            // Guard: if the GameObject is being deactivated (e.g. returned to pool),
            if (!gameObject.activeInHierarchy)
            {
                if (showDebugLogs) Debug.Log("[CookingTableStructure] Object inactive — ignoring false exit.");
                return;
            }
            StartCoroutine(ConfirmPlayerExited());
        }
    }

    /// <summary>
    /// This prevents false exits caused by Rigidbody2D going to sleep,
    /// which can fire OnTriggerExit2D even when the player is stationary.
    /// </summary>
    private System.Collections.IEnumerator ConfirmPlayerExited()
    {
        // Wait one frame for physics to settle before querying.
        yield return null;

        if (_triggerCollider == null) yield break;

        // Re-check via physics overlap query.
        var results = new List<Collider2D>();
        var filter = new ContactFilter2D { useTriggers = true };
        Physics2D.OverlapCollider(_triggerCollider, filter, results);

        foreach (var col in results)
        {
            if (col.CompareTag("PlayerEntity"))
            {
                // False exit — player is still inside (Rigidbody2D sleep artifact).
                _overlapCount++;
                if (showDebugLogs) Debug.Log("[CookingTableStructure] False exit ignored — player still inside trigger.");

                yield break;
            }
        }

        // Player has genuinely left the trigger zone — close the UI.
        if (showDebugLogs) Debug.Log("[CookingTableStructure] Player confirmed outside — closing Cooking UI.");

        if (craftingSystemManager != null)
            craftingSystemManager.CloseCookingUI();
        else
        {
            if (showDebugLogs) Debug.LogWarning("[CookingTableStructure] CraftingSystemManager not found in scene!");
        }
    }

    // ── Input callback ──────────────────────────────────────────────────

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!PlayerInRange) return;
        if (craftingSystemManager == null) return;

        // Toggle: close if already open, open if closed.
        if (craftingSystemManager.IsCookingUIOpen())
            craftingSystemManager.CloseCookingUI();
        else
            Interact();
    }

    // ── IInteractable ───────────────────────────────────────────────────

    public void Interact()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.OpenCookingUI();
        else
        {
            if (showDebugLogs) Debug.LogWarning("[CookingTableStructure] CraftingSystemManager not found in scene!");
        }
    }
}
