using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to the CraftingTable prefab.
/// Uses mouse click to toggle the Crafting UI when the player
/// is inside the trigger collider and hovering over the table.
/// Auto-closes when the player genuinely leaves the trigger zone.
/// </summary>
public class CraftingTableStructure : MonoBehaviour, IInteractable
{

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private CraftingSystemManager craftingSystemManager;
    private Collider2D _triggerCollider;

    // Overlap counter instead of a plain bool.
    // Handles Unity physics wakeup events that may fire Enter/Exit repeatedly.
    private int _overlapCount = 0;
    private bool PlayerInRange => _overlapCount > 0;

    // Track mouse hover state
    private bool _isMouseHovering = false;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        craftingSystemManager = FindFirstObjectByType<CraftingSystemManager>();
    }

    private void OnEnable()
    {
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
        _overlapCount = 0;
        _isMouseHovering = false;
    }

    // ── Trigger zone ────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerEntity"))
        {
            _overlapCount++;

            if (showDebugLogs) Debug.Log($"[CraftingTableStructure] Player entered trigger (overlap={_overlapCount})");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("PlayerEntity")) return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        if (showDebugLogs) Debug.Log($"[CraftingTableStructure] OnTriggerExit2D received (overlap={_overlapCount})");

        if (_overlapCount == 0)
        {
            // Guard: if the GameObject is being deactivated (e.g. returned to pool),
            if (!gameObject.activeInHierarchy)
            {
                if (showDebugLogs) Debug.Log("[CraftingTableStructure] Object inactive — ignoring false exit.");
                return;
            }
            StartCoroutine(ConfirmPlayerExited());
        }
    }

    /// <summary>
    /// Waits one frame then performs a physics query to verify the player
    /// has actually left the trigger zone before closing the UI.
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
                if (showDebugLogs) Debug.Log("[CraftingTableStructure] False exit ignored — player still inside trigger.");

                yield break;
            }
        }

        // Player has genuinely left the trigger zone — close the UI.
        if (showDebugLogs) Debug.Log("[CraftingTableStructure] Player confirmed outside — closing Crafting UI.");

        if (craftingSystemManager != null)
            craftingSystemManager.CloseCraftingUI();
        else
        {
            if (showDebugLogs) Debug.LogWarning("[CraftingTableStructure] CraftingSystemManager not found in scene!");
        }
    }

    // ── Mouse Click / Hover callback ────────────────────────────────────

    private void OnMouseEnter()
    {
        _isMouseHovering = true;
    }

    private void OnMouseExit()
    {
        _isMouseHovering = false;
    }

    private void OnMouseDown()
    {
        if (!PlayerInRange || !_isMouseHovering) return;
        if (craftingSystemManager == null) return;

        // Toggle: close if already open, open if closed.
        if (craftingSystemManager.IsCraftingUIOpen())
            craftingSystemManager.CloseCraftingUI();
        else
            Interact();
    }

    // ── IInteractable ───────────────────────────────────────────────────

    public void Interact()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.OpenCraftingUI();
        else
        {
            if (showDebugLogs) Debug.LogWarning("[CraftingTableStructure] CraftingSystemManager not found in scene!");
        }
    }
}
