using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Photon.Pun;

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
    private bool _isPointerOverUI = false;

    // ── Structure UI active tracking ─────────────────────────────────────
    private static InteractableStructureBase _activeStructure;
    public static InteractableStructureBase ActiveStructure => _activeStructure;

    private InputAction _escCloseAction;
    private bool _closeInputSubscribed = false;

    // ── Structure Interaction Badge ───────────────────────────────────────
    private GameObject _structureInteractionBadge;
    private SpriteRenderer _structureInteractionRenderer;

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
        _isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        CheckPlayerInRange();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        if (_activeStructure == this) OnStructureClosed();
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
        if (_activeStructure == this) OnStructureClosed();
        OnStructureDestroyed();

        if (mouseHoverTrigger != null)
        {
            mouseHoverTrigger.OnHoverEnter -= HandleHoverEnter;
            mouseHoverTrigger.OnHoverExit -= HandleHoverExit;
        }

        if (_escCloseAction != null)
        {
            _escCloseAction.performed -= OnCloseInputPressed;
            _escCloseAction.Dispose();
            _escCloseAction = null;
        }
    }

    // ── Player Range Polling ─────────────────────────────────────────────

    private void CheckPlayerInRange()
    {
        if (_triggerCollider == null) return;
        if (!gameObject.activeInHierarchy) return;

        GameObject playerObj = FindLocalPlayer();
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
            if (col is CapsuleCollider2D && col.CompareTag("PlayerEntity")) 
            { foundPlayer = true; break; }
        }

        if (foundPlayer != _playerInRange)
        {
            _playerInRange = foundPlayer;

            if (!_playerInRange && IsUIOpen())
            {
                CloseUI();
                OnStructureClosed();
            }

            EvaluateTargetState();
        }
    }

    private GameObject FindLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
        if (players == null || players.Length == 0)
            return null;

        // Online: always prefer the locally owned Photon entity.
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (PhotonNetwork.IsConnected && (pv == null || !pv.IsMine))
                continue;
            return player;
        }

        // Offline fallback: use the first tagged player entity.
        if (!PhotonNetwork.IsConnected)
            return players[0];

        return null;
    }

    // ── Input ────────────────────────────────────────────────────────────
    private static int _lastInteractFrame = -1;

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

        if (Time.frameCount == _lastInteractFrame) return;

        // Block opening another structure while one is already open
        if (_activeStructure != null) return;

        // Block interaction when the pointer is over a UI element (cached from Update to avoid InputSystem callback timing issue)
        if (_isPointerOverUI)
            return;

        if (_isTargeted)
        {
            _lastInteractFrame = Time.frameCount;
            Interact();
        }
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

    // ── Structure Interaction Badge ──────────────────────────────────────

    protected void SetupStructureInteractionBadge(string structureId)
    {
        // Destroy previous badge to prevent duplicates when reused from pool
        if (_structureInteractionBadge != null)
        {
            Destroy(_structureInteractionBadge);
            _structureInteractionBadge = null;
            _structureInteractionRenderer = null;
        }

        var sprite = ItemCatalogService.Instance?.GetCachedStructureInteractionSprite(structureId);
        if (sprite == null) return;

        _structureInteractionBadge = new GameObject("StructureInteractionBadge");
        _structureInteractionBadge.transform.SetParent(transform);
        _structureInteractionBadge.transform.localPosition = Vector3.zero;
        _structureInteractionRenderer = _structureInteractionBadge.AddComponent<SpriteRenderer>();
        _structureInteractionRenderer.sprite = Sprite.Create(
            sprite.texture,
            sprite.rect,
            new Vector2(0.5f, 0f),
            sprite.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect);
        _structureInteractionRenderer.sortingLayerName = "WalkInfront";
        _structureInteractionRenderer.sortingOrder = 1;
        _structureInteractionBadge.SetActive(false);
    }

    protected void ShowStructureInteractionBadge()
    {
        if (_structureInteractionBadge != null)
            _structureInteractionBadge.SetActive(true);
    }

    protected void HideStructureInteractionBadge()
    {
        if (_structureInteractionBadge != null)
            _structureInteractionBadge.SetActive(false);
    }

    // ── IInteractable ────────────────────────────────────────────────────

    public void Interact()
    {
        OpenUI();
        OnStructureOpened();
    }

    /// <summary>
    /// Called after a structure UI opens.
    /// Disables most player input (except OpenInventory) and subscribes ESC/Inventory to close.
    /// </summary>
    private void OnStructureOpened()
    {
        _activeStructure = this;

        // Disable all player input
        if (InputManager.Instance != null)
            InputManager.Instance.DisablePlayerActions();

        // Subscribe close triggers: OpenInventory and ESC
        SubscribeCloseInput();
    }

    /// <summary>
    /// Called when a structure UI is closed.
    /// Re-enables player input and unsubscribes close triggers.
    /// </summary>
    private void OnStructureClosed()
    {
        if (_activeStructure != this) return;
        _activeStructure = null;

        UnsubscribeCloseInput();

        // Re-enable all player input
        if (InputManager.Instance != null)
            InputManager.Instance.EnablePlayerActions();
    }

    /// <summary>
    /// Public method for external systems (MenuController, etc.) to close the active structure UI.
    /// </summary>
    public static void CloseActiveStructure()
    {
        if (_activeStructure != null && _activeStructure.IsUIOpen())
        {
            _activeStructure.CloseUI();
            _activeStructure.OnStructureClosed();
        }
    }

    private void SubscribeCloseInput()
    {
        if (_closeInputSubscribed) return;

        // ESC key
        if (_escCloseAction == null)
        {
            _escCloseAction = new InputAction("StructureCloseESC", InputActionType.Button, "<Keyboard>/escape");
            _escCloseAction.performed += OnCloseInputPressed;
        }
        _escCloseAction.Enable();

        // OpenInventory action (re-enable just this one action so the callback fires)
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OpenInventory.Enable();
            InputManager.Instance.OpenInventory.performed += OnCloseInputPressed;
        }

        _closeInputSubscribed = true;
    }

    private void UnsubscribeCloseInput()
    {
        if (!_closeInputSubscribed) return;

        _escCloseAction?.Disable();

        if (InputManager.Instance != null)
            InputManager.Instance.OpenInventory.performed -= OnCloseInputPressed;

        _closeInputSubscribed = false;
    }

    private void OnCloseInputPressed(InputAction.CallbackContext ctx)
    {
        if (!IsUIOpen()) return;
        CloseUI();
        OnStructureClosed();
    }

    public void SetBeingPooled()
    {
        _isBeingPooled = true;
    }
}
