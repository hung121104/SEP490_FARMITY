using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attached to the CraftingTable prefab.
/// Enables a global highlight prefab when the player is inside the trigger collider AND hovering over the table.
/// Listens for the Interact action (E key) to toggle the Crafting UI.
/// Auto-closes when the player genuinely leaves the trigger zone.
/// </summary>
public class CraftingTableStructure : MonoBehaviour, IInteractable
{

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("Highlight Settings")]
    [Tooltip("Offset position for the global highlight object relative to this structure.")]
    [SerializeField] private Vector3 highlightOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("A dedicated trigger object for mouse hover.")]
    [SerializeField] private MouseHoverTrigger mouseHoverTrigger;

    private CraftingSystemManager craftingSystemManager;
    private Collider2D _triggerCollider;

    // Polling state instead of overlap counter.
    private bool _playerInRange = false;
    public bool IsPlayerInRange => _playerInRange;

    // Track mouse hover state
    private bool _isMouseHovering = false;
    private bool _isTargeted = false;

    // ── IInteractable Properties ────────────────────────────────────────
    public bool IsTargeted => _isTargeted;
    public Vector3 HighlightOffset => highlightOffset;

    // Guard against double-subscribing.
    private bool _inputSubscribed = false;
    private bool _isBeingPooled = false;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();

        if (mouseHoverTrigger != null)
        {
            mouseHoverTrigger.OnHoverEnter += HandleHoverEnter;
            mouseHoverTrigger.OnHoverExit += HandleHoverExit;
        }
    }

    private void Start()
    {
        craftingSystemManager = FindFirstObjectByType<CraftingSystemManager>();
    }

    private void OnEnable()
    {
        _isBeingPooled = false;
        _isTargeted = false;

        // Immediately re-evaluate state to avoid any visible flicker.
        _isMouseHovering = false;
        _playerInRange = false;

        if (_triggerCollider != null && Camera.main != null)
        {
            // Mouse hover check
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _isMouseHovering = _triggerCollider.OverlapPoint(mousePos);

            // Player range check
            var results = new List<Collider2D>();
            var filter = new ContactFilter2D { useTriggers = true };
            Physics2D.OverlapCollider(_triggerCollider, filter, results);
            foreach (var col in results)
            {
                if (col.CompareTag("PlayerEntity")) { _playerInRange = true; break; }
            }
        }

        EvaluateTargetState(); // Show highlight immediately if conditions are already met.
        SubscribeInput();
    }

    private void Update()
    {
        CheckPlayerInRange();
    }

    private void CheckPlayerInRange()
    {
        if (_triggerCollider == null) return;

        if (!gameObject.activeInHierarchy) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("PlayerEntity");
        if (playerObj != null)
        {
            var rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null && rb.IsSleeping()) rb.WakeUp();
        }

        var results = new List<Collider2D>();
        var filter = new ContactFilter2D { useTriggers = true };
        Physics2D.OverlapCollider(_triggerCollider, filter, results);

        bool foundPlayer = false;
        foreach (var col in results)
        {
            if (col.CompareTag("PlayerEntity"))
            {
                foundPlayer = true;
                break;
            }
        }

        if (foundPlayer != _playerInRange)
        {
            _playerInRange = foundPlayer;
            
            if (!_playerInRange && IsUIOpen())
            {
                CloseUI();
            }

            EvaluateTargetState();
        }
    }

    private void OnDisable()
    {
        UnsubscribeInput();

        if (_isBeingPooled)
        {
            _isBeingPooled = false;
            return;
        }

        _playerInRange = false;
        _isMouseHovering = false;
        EvaluateTargetState();
    }

    private void OnDestroy()
    {
        UnsubscribeInput();
        if (mouseHoverTrigger != null)
        {
            mouseHoverTrigger.OnHoverEnter -= HandleHoverEnter;
            mouseHoverTrigger.OnHoverExit -= HandleHoverExit;
        }
    }

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

    // ── Target Evaluation ───────────────────────────────────────────────

    private void EvaluateTargetState()
    {
        bool shouldBeTargeted = IsPlayerInRange && _isMouseHovering;

        if (shouldBeTargeted && !_isTargeted)
        {
            _isTargeted = true;
            if (StructureHighlight.Instance != null)
                StructureHighlight.Instance.Show(transform, highlightOffset);

            if (showDebugLogs) Debug.Log("[CraftingTable] Targeted (Highlight ON)");
        }
        else if (!shouldBeTargeted && _isTargeted)
        {
            _isTargeted = false;
            if (StructureHighlight.Instance != null)
                StructureHighlight.Instance.Hide(transform);

            if (showDebugLogs) Debug.Log("[CraftingTable] Untargeted (Highlight OFF)");
        }
    }

    // ── Mouse / Hover callback ────────────────────────────────────

    private void HandleHoverEnter()
    {
        _isMouseHovering = true;
        EvaluateTargetState();
    }

    private void HandleHoverExit()
    {
        _isMouseHovering = false;
        EvaluateTargetState();
    }

    // ── Input callback (Interact) ───────────────────────────────────────

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (craftingSystemManager == null) return;

        // If the UI is already open, any table the player is standing near can close it.
        // This solves the issue where hover is blocked by the UI itself.
        if (IsUIOpen())
        {
            if (IsPlayerInRange)
            {
                CloseUI();
            }
            return;
        }

        // If UI is closed, only this specific table can open it if targeted (hover + range)
        if (_isTargeted)
        {
            Interact();
        }
    }

    // ── IInteractable ───────────────────────────────────────────────────

    public void Interact()
    {
        OpenUI();
    }

    public bool IsUIOpen()
    {
        return craftingSystemManager != null && craftingSystemManager.IsCraftingUIOpen();
    }

    public void OpenUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.OpenCraftingUI();
        else
        {
            if (showDebugLogs) Debug.LogWarning("[CraftingTableStructure] CraftingSystemManager not found in scene!");
        }
    }

    public void CloseUI()
    {
        if (craftingSystemManager != null)
            craftingSystemManager.CloseCraftingUI();
    }

    public void SetBeingPooled()
    {
        _isBeingPooled = true;
    }
}
