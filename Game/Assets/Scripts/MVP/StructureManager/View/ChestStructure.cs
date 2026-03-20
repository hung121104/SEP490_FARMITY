using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attached to the Chest prefab.
/// Enables a global highlight prefab when the player is inside the trigger collider AND hovering over the chest.
/// Listens for the Interact action to toggle the Chest UI.
/// Auto-closes when the player genuinely leaves the trigger zone.
/// Subscribes to ChestSyncManager open/close notifications for badge display.
/// </summary>
public class ChestStructure : MonoBehaviour, IInteractable, IWorldStructure
{
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("Highlight Settings")]
    [Tooltip("Offset position for the global highlight object relative to this structure.")]
    [SerializeField] private Vector3 highlightOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("A dedicated trigger object for mouse hover.")]
    [SerializeField] private MouseHoverTrigger mouseHoverTrigger;

    [Header("Chest Settings")]
    [Tooltip("Badge shown when another player is using this chest.")]
    [SerializeField] private GameObject inUseBadge;

    private ChestGameView chestGameView;
    private ChestData chestData;
    private Collider2D _triggerCollider;

    // Polling state
    private bool _playerInRange = false;
    public bool IsPlayerInRange => _playerInRange;

    // Track mouse hover state
    private bool _isMouseHovering = false;
    private bool _isTargeted = false;

    // ── IInteractable Properties ────────────────────────────────────────
    public bool IsTargeted => _isTargeted;
    public Vector3 HighlightOffset => highlightOffset;

    // Guard against double-subscribing
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
        else
        {
            Debug.LogWarning($"[ChestStructure] mouseHoverTrigger is not assigned on '{gameObject.name}'. Chest cannot be targeted (hover required to open UI).", this);
        }
    }

    private void Start()
    {
        chestGameView = FindFirstObjectByType<ChestGameView>(FindObjectsInactive.Include);
        if (chestGameView == null)
            Debug.LogError("[ChestStructure] ChestGameView not found in scene! Chest interaction will not work.", this);
    }

    private void OnEnable()
    {
        _isBeingPooled = false;
        _isTargeted = false;

        // Immediately re-evaluate state to avoid any visible flicker
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

        EvaluateTargetState();
        SubscribeInput();

        // Subscribe to open/close notifications for badge
        ChestSyncManager.OnChestOpened += HandleChestOpened;
        ChestSyncManager.OnChestClosed += HandleChestClosed;
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

            // If player out of range and UI is open, close UI
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

        ChestSyncManager.OnChestOpened -= HandleChestOpened;
        ChestSyncManager.OnChestClosed -= HandleChestClosed;

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

        ChestSyncManager.OnChestOpened -= HandleChestOpened;
        ChestSyncManager.OnChestClosed -= HandleChestClosed;
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

            if (showDebugLogs) Debug.Log("[ChestStructure] Targeted (Highlight ON)");
        }
        else if (!shouldBeTargeted && _isTargeted)
        {
            _isTargeted = false;
            if (StructureHighlight.Instance != null)
                StructureHighlight.Instance.Hide(transform);

            if (showDebugLogs) Debug.Log("[ChestStructure] Untargeted (Highlight OFF)");
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
        if (chestGameView == null || chestData == null) return;

        // If the UI is already open, any chest the player is standing near can close it.
        if (IsUIOpen())
        {
            if (IsPlayerInRange)
            {
                CloseUI();
            }
            return;
        }

        // If UI is closed, only this specific chest can open it if targeted (hover + range)
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
        return chestGameView != null && chestGameView.IsChestOpen()
            && chestGameView.ActiveChestId == chestData?.ChestId;
    }

    public void OpenUI()
    {
        if (chestGameView != null && chestData != null)
            chestGameView.OpenChest(chestData);
        else
        {
            if (showDebugLogs) Debug.LogWarning("[ChestStructure] ChestGameView not found in scene!");
        }
    }

    public void CloseUI()
    {
        if (chestGameView != null)
            chestGameView.CloseChest();
    }

    // ── IWorldStructure ─────────────────────────────────────────────────

    /// <summary>
    /// Called by ChunkLoadingManager after spawn. Builds ChestData from world info.
    /// </summary>
    public void InitializeFromWorld(int worldX, int worldY, StructureData structureData)
    {
        chestData = new ChestData(worldX, worldY, structureData.StructureLevel);

        // Self-register in ChestDataModule (Master or offline only).
        // Idempotent — safe to call even if already registered.
        if (Photon.Pun.PhotonNetwork.IsMasterClient || !Photon.Pun.PhotonNetwork.IsConnected)
        {
            WorldDataManager.Instance?.RegisterChest(
                (short)worldX,
                (short)worldY,
                (byte)structureData.StorageSlots,
                (byte)structureData.StructureLevel);
        }
    }

    /// <summary>Direct init — used by tests or code that already has a ChestData.</summary>
    public void Initialize(ChestData data)
    {
        chestData = data;
    }

    public void SetBeingPooled()
    {
        _isBeingPooled = true;
    }

    // ── Badge (Open/Close Notifications) ─────────────────────────────────

    private void HandleChestOpened(string chestId, int actorNumber)
    {
        if (chestData == null || chestData.ChestId != chestId) return;
        if (inUseBadge != null)
            inUseBadge.SetActive(true);

        if (showDebugLogs)
            Debug.Log($"[ChestStructure] Badge ON — player #{actorNumber} opened '{chestId}'");
    }

    private void HandleChestClosed(string chestId, int actorNumber)
    {
        if (chestData == null || chestData.ChestId != chestId) return;
        if (inUseBadge != null)
            inUseBadge.SetActive(false);

        if (showDebugLogs)
            Debug.Log($"[ChestStructure] Badge OFF — player #{actorNumber} closed '{chestId}'");
    }
}
