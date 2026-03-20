using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Abstract base class for all interactable structures (Chest, CraftingTable, CookingTable, …).
/// Contains shared logic: player-range polling, mouse-hover targeting, highlight,
/// input subscription, and the Interact → Open/Close UI flow.
///
/// Subclasses only override:
///   - StructureTag (for debug logs)
///   - FindUI()      — locate their specific manager/view in scene
///   - CanInteract()  — guard before OnInteract fires
///   - IsUIOpen() / OpenUI() / CloseUI() — delegate to their UI
///   - OnStructureEnabled/Disabled/Destroyed() — optional hooks
///   - InitializeFromWorld() — if the structure implements IWorldStructure
///
/// MVP layer: View
/// </summary>
public abstract class InteractableStructureBase : MonoBehaviour, IInteractable
{
    [Header("Debug")]
    [SerializeField] protected bool showDebugLogs = false;

    [Header("Highlight Settings")]
    [Tooltip("Offset position for the global highlight object relative to this structure.")]
    [SerializeField] private Vector3 highlightOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("A dedicated trigger object for mouse hover.")]
    [SerializeField] private MouseHoverTrigger mouseHoverTrigger;

    private Collider2D _triggerCollider;

    private bool _playerInRange = false;
    private bool _isMouseHovering = false;
    private bool _isTargeted = false;
    private bool _inputSubscribed = false;
    private bool _isBeingPooled = false;

    // ── IInteractable Properties ────────────────────────────────────────
    public bool IsPlayerInRange => _playerInRange;
    public bool IsTargeted => _isTargeted;
    public Vector3 HighlightOffset => highlightOffset;

    // ── Abstract / Virtual members for subclasses ────────────────────────

    /// <summary>Tag used in Debug.Log messages, e.g. "ChestStructure", "CraftingTable".</summary>
    protected abstract string StructureTag { get; }

    /// <summary>Called in Start(). Find and cache the UI manager/view needed by this structure.</summary>
    protected abstract void FindUI();

    /// <summary>Guard called before OnInteract processes input. Return false to skip.</summary>
    protected abstract bool CanInteract();

    /// <summary>Returns true if this structure's UI panel is currently open.</summary>
    public abstract bool IsUIOpen();

    /// <summary>Opens the UI panel associated with this structure.</summary>
    public abstract void OpenUI();

    /// <summary>Closes the UI panel associated with this structure.</summary>
    public abstract void CloseUI();

    /// <summary>Optional hook: called at the end of OnEnable, after base setup.</summary>
    protected virtual void OnStructureEnabled() { }

    /// <summary>Optional hook: called at the start of OnDisable, before base cleanup.</summary>
    protected virtual void OnStructureDisabled() { }

    /// <summary>Optional hook: called at the start of OnDestroy, before base cleanup.</summary>
    protected virtual void OnStructureDestroyed() { }

    // ── Unity Lifecycle ──────────────────────────────────────────────────

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider2D>();

        if (mouseHoverTrigger != null)
        {
            mouseHoverTrigger.OnHoverEnter += HandleHoverEnter;
            mouseHoverTrigger.OnHoverExit += HandleHoverExit;
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"[{StructureTag}] mouseHoverTrigger is not assigned on '{gameObject.name}'.", this);
        }
    }

    private void Start()
    {
        FindUI();
    }

    private void OnEnable()
    {
        _isBeingPooled = false;
        _isTargeted = false;
        _isMouseHovering = false;
        _playerInRange = false;

        if (_triggerCollider != null && Camera.main != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _isMouseHovering = _triggerCollider.OverlapPoint(mousePos);

            var results = new List<Collider2D>();
            var filter = new ContactFilter2D { useTriggers = true };
            Physics2D.OverlapCollider(_triggerCollider, filter, results);
            foreach (var col in results)
            {
                if (col.CompareTag("PlayerEntity")) { _playerInRange = true; break; }
            }
        }

        EvaluateTargetState();
        SubscribeInput();
        OnStructureEnabled();
    }

    private void Update()
    {
        CheckPlayerInRange();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        OnStructureDisabled();

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
        OnStructureDestroyed();

        if (mouseHoverTrigger != null)
        {
            mouseHoverTrigger.OnHoverEnter -= HandleHoverEnter;
            mouseHoverTrigger.OnHoverExit -= HandleHoverExit;
        }
    }

    // ── Player Range Polling ─────────────────────────────────────────────

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
            if (col.CompareTag("PlayerEntity")) { foundPlayer = true; break; }
        }

        if (foundPlayer != _playerInRange)
        {
            _playerInRange = foundPlayer;

            if (!_playerInRange && IsUIOpen())
                CloseUI();

            EvaluateTargetState();
        }
    }

    // ── Input ────────────────────────────────────────────────────────────

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

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!CanInteract()) return;

        if (IsUIOpen())
        {
            if (IsPlayerInRange)
                CloseUI();
            return;
        }

        if (_isTargeted)
            Interact();
    }

    // ── Target Evaluation ────────────────────────────────────────────────

    private void EvaluateTargetState()
    {
        bool shouldBeTargeted = _playerInRange && _isMouseHovering;

        if (shouldBeTargeted && !_isTargeted)
        {
            _isTargeted = true;
            if (StructureHighlight.Instance != null)
                StructureHighlight.Instance.Show(transform, highlightOffset);

            if (showDebugLogs) Debug.Log($"[{StructureTag}] Targeted (Highlight ON)");
        }
        else if (!shouldBeTargeted && _isTargeted)
        {
            _isTargeted = false;
            if (StructureHighlight.Instance != null)
                StructureHighlight.Instance.Hide(transform);

            if (showDebugLogs) Debug.Log($"[{StructureTag}] Untargeted (Highlight OFF)");
        }
    }

    // ── Hover Callbacks ──────────────────────────────────────────────────

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

    // ── IInteractable ────────────────────────────────────────────────────

    public void Interact()
    {
        OpenUI();
    }

    public void SetBeingPooled()
    {
        _isBeingPooled = true;
    }
}
