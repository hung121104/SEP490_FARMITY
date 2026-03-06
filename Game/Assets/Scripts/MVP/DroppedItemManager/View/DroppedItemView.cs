using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;


/// <summary>
/// Visual representation of a single dropped item in the world.
/// Attach this to the DroppedItem prefab.
/// Manages both the visual display and the per-item lifecycle (despawn timer, blink, pickup).
/// 
/// Requires: SpriteRenderer, child TextMeshPro for prompt.
/// Player detection uses Physics2D.OverlapCircle in Update() instead of trigger events
/// to ensure correct reset state when reused via Object Pooling.
/// </summary>

public class DroppedItemView : MonoBehaviour, IDroppedItemView
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Seconds remaining when blink animation should start.</summary>
    private const float BLINK_THRESHOLD_SECONDS = 30f;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Root GameObject shown when quantity is 67–99 (multiple stack). Contains 3 SpriteRenderer children.")]
    [SerializeField] private GameObject stackMultiple;

    [Tooltip("Root GameObject shown when quantity is 34–66 (medium stack). Contains 2 SpriteRenderer children.")]
    [SerializeField] private GameObject stackMedium;

    [Tooltip("Root GameObject shown when quantity is 1–33 (single stack). Contains 1 SpriteRenderer child.")]
    [SerializeField] private GameObject stackSingle;

    [Tooltip("TextMeshPro child for the pickup prompt.")]
    [SerializeField] private TextMeshProUGUI pickupPromptText;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "WalkInfront";
    [SerializeField] private int sortingOrder = 10;

    [Header("Blink Settings")]
    [Tooltip("How fast the item blinks (cycles per second).")]
    [SerializeField] private float blinkSpeed = 4f;

    [Tooltip("Minimum alpha during blink.")]
    [SerializeField] private float blinkMinAlpha = 0.25f;

    [Header("Interaction")]
    [Tooltip("Tag used to identify the local player.")]
    [SerializeField] private string playerTag = "PlayerEntity";

    [Tooltip("Key to pick up the item.")]
    [SerializeField] private KeyCode pickupKey = KeyCode.F;

    [Tooltip("Radius (world units) to detect local player for pickup prompt.\nReplaces CircleCollider2D trigger — safe for Object Pooling.")]
    [SerializeField] private float pickupRadius = 0.8f;

    [Tooltip("Layer mask for player detection. Default: Everything.")]
    [SerializeField] private LayerMask playerLayerMask = ~0;

    [Header("Debug")]
    [Tooltip("Show pickup radius gizmo in Scene view.")]
    [SerializeField] private bool showGizmos = true;

    [Tooltip("Color of the pickup radius gizmo.")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);

    [Tooltip("Enable verbose logging to debug pickupPromptText and player detection.")]
    [SerializeField] private bool debugLogs = false;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private DroppedItemData _data;
    private Coroutine _blinkCoroutine;
    private bool _isBlinking;
    private bool _playerInRange;
    private bool _blinkStarted;
    private bool _despawnBroadcast;

    /// <summary>Pre-allocated buffer for OverlapCircleNonAlloc — avoids per-frame heap allocation.</summary>
    private readonly Collider2D[] _overlapBuffer = new Collider2D[16];

    /// <summary>Drop ID this view represents.</summary>
    public string DropId => _data?.dropId;

    // ── Initialization ────────────────────────────────────────────────────────

    /// <summary>
    /// Initialize the view with a data model.
    /// Called once by DroppedItemManagerView right after instantiation.
    /// </summary>
    /// <param name="data">The dropped item data from service/sync.</param>
    public void Initialize(DroppedItemData data)
    {
        _data = data;
        _blinkStarted = false;
        _despawnBroadcast = false;

        // Show the visual immediately
        ShowItem(data);
    }

    // ── IDroppedItemView ──────────────────────────────────────────────────────

    /// <summary>
    /// Initialize the visual with model data. 
    /// Loads the sprite from ItemCatalogService cache.
    /// </summary>
    public void ShowItem(DroppedItemData data)
    {
        _data = data;

        // Load sprite from catalog cache
        Sprite icon = ItemCatalogService.Instance != null
            ? ItemCatalogService.Instance.GetCachedSprite(data.itemId)
            : null;

        if (icon == null)
            Debug.LogWarning($"[DroppedItemView] No cached sprite for item '{data.itemId}'. Using fallback.");

        // Activate the correct stack visual group and set sprite on all its renderers
        ApplyStackVisual(data.quantity, icon);

        // Position in world
        transform.position = new Vector3(data.worldX, data.worldY, 0f);

        // Prompt hidden by default
        SetPickupPromptVisible(false);

        // Debug: warn if pickupPromptText is missing
        if (pickupPromptText == null)
            Debug.LogWarning($"[DroppedItemView] pickupPromptText is NULL on '{gameObject.name}'. " +
                             "Assign a TextMeshProUGUI child in the Inspector or check prefab hierarchy.");

        gameObject.SetActive(true);
    }

    /// <summary>Hide the visual and clean up blink state.</summary>
    public void HideItem()
    {
        StopBlinking();
        SetPickupPromptVisible(false);
        gameObject.SetActive(false);
    }

    /// <summary>Start alpha oscillation (last 30 seconds before expiry).</summary>
    public void StartBlinking()
    {
        if (_isBlinking) return;
        _isBlinking = true;
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    /// <summary>Stop alpha oscillation and restore full opacity on all active renderers.</summary>
    public void StopBlinking()
    {
        if (!_isBlinking) return;
        _isBlinking = false;

        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        // Restore full alpha on all active renderers
        foreach (var sr in GetActiveRenderers())
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    /// <summary>Show or hide the "[F] Pick up" text prompt.</summary>
    public void SetPickupPromptVisible(bool visible)
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.gameObject.SetActive(visible);

            if (debugLogs)
                Debug.Log($"[DroppedItemView] pickupPromptText.SetActive({visible}) on '{gameObject.name}'");
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"[DroppedItemView] SetPickupPromptVisible({visible}) skipped — pickupPromptText is NULL.");
        }
    }

    // ── Blink Coroutine ───────────────────────────────────────────────────────

    /// <summary>
    /// Oscillates the sprite alpha between 1.0 and blinkMinAlpha
    /// using a sine wave for smooth pulsing.
    /// </summary>
    private IEnumerator BlinkRoutine()
    {
        float t = 0f;
        while (_isBlinking)
        {
            t += Time.deltaTime * blinkSpeed;
            float alpha = Mathf.Lerp(blinkMinAlpha, 1f, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);

            // Apply alpha to every renderer in the active stack group
            foreach (var sr in GetActiveRenderers())
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
            yield return null;
        }
    }

    // ── Player Range Detection ─────────────────────────────────────────────────────

    /// <summary>
    /// Uses Physics2D.OverlapCircleNonAlloc to detect the local player nearby.
    /// NonAlloc writes into a pre-allocated buffer — zero heap allocation per frame.
    /// </summary>
    private bool IsLocalPlayerInRange()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, pickupRadius, _overlapBuffer, playerLayerMask);

        if (debugLogs && count > 0)
            Debug.Log($"[DroppedItemView] OverlapCircleNonAlloc hit {count} collider(s) within radius {pickupRadius} on '{gameObject.name}'");

        for (int i = 0; i < count; i++)
        {
            if (IsLocalPlayer(_overlapBuffer[i]))
            {
                if (debugLogs)
                    Debug.Log($"[DroppedItemView] Local player detected: '{_overlapBuffer[i].gameObject.name}'");
                return true;
            }
        }
        return false;
    }

    // ── Frame Update ──────────────────────────────────────────────────────────

    private void Update()
    {
        if (_data == null) return;

        double remaining = _data.RemainingSeconds;

        // Start blinking when <= threshold
        if (!_blinkStarted && remaining <= BLINK_THRESHOLD_SECONDS && remaining > 0f)
        {
            _blinkStarted = true;
            StartBlinking();
        }

        // Despawn when expired — only MasterClient broadcasts to avoid duplicate events
        if (!_despawnBroadcast && _data.IsExpired)
        {
            _despawnBroadcast = true;

            if (PhotonNetwork.IsMasterClient)
            {
                // Notify all clients to despawn this item
                var syncManager = FindAnyObjectByType<DroppedItemSyncManager>();
                if (syncManager != null)
                {
                    syncManager.BroadcastItemDespawn(_data.dropId);
                }
            }
        }

        // OverlapCircle-based player detection (pool-safe, replaces OnTriggerEnter/Exit)
        bool playerNearby = IsLocalPlayerInRange();
        if (playerNearby != _playerInRange)
        {
            _playerInRange = playerNearby;
            SetPickupPromptVisible(_playerInRange);
        }

        // Listen for pickup key press when player is in range
        if (_playerInRange && Input.GetKeyDown(pickupKey))
        {
            OnPickupRequested(_data.dropId);
        }
    }

    // ── Pickup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when the local player presses the pickup key.
    /// Delegates to DroppedItemManagerView which handles the Photon request flow.
    /// </summary>
    /// <param name="dropId">The unique drop ID to pick up.</param>
    private void OnPickupRequested(string dropId)
    {
        if (_data == null) return;
        if (_data.IsExpired)
        {
            Debug.Log($"[DroppedItemView] Item '{dropId}' already expired, ignoring pickup.");
            return;
        }

        var manager = DroppedItemManagerView.Instance;
        if (manager != null)
        {
            manager.RequestPickupItem(dropId);
        }
        else
        {
            Debug.LogError("[DroppedItemView] DroppedItemManagerView.Instance is null!");
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws pickup radius and debug info in the Scene view.
    /// Visible only when the GameObject is selected (OnDrawGizmosSelected)
    /// or always when showGizmos = true (OnDrawGizmos).
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        DrawPickupGizmo();
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawPickupGizmo();
    }

    private void DrawPickupGizmo()
    {
        // Filled disc — shows the detection area
        UnityEditor.Handles.color = gizmoColor;
#if UNITY_EDITOR
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, pickupRadius);
#endif

        // Outline wire circle
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Label: item name + prompt state
        if (_data != null)
        {
#if UNITY_EDITOR
            string promptState = pickupPromptText == null ? "[prompt: NULL!]" :
                                 pickupPromptText.gameObject.activeSelf ? "[prompt: visible]" : "[prompt: hidden]";
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (pickupRadius + 0.15f),
                $"{_data.itemName} | r={pickupRadius} | {promptState} | inRange={_playerInRange}"
            );
#endif
        }
    }

    // ── Stack Visual Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Activate the correct stack group based on quantity and apply the sprite
    /// to every SpriteRenderer child within that group.
    /// Quantity ranges: 1\u201333 = Single, 34\u201366 = Medium, 67\u201399 = Multiple.
    /// </summary>
    private void ApplyStackVisual(int quantity, Sprite icon)
    {
        // quantity >= 67 : all three groups ON
        // quantity 34–66 : Medium + Single ON, Multiple OFF
        // quantity <= 33 : Single only ON,    Multiple + Medium OFF
        bool showMultiple = quantity >= 67;
        bool showMedium   = quantity >= 34;
        bool showSingle   = true;             // always visible

        if (stackMultiple != null) stackMultiple.SetActive(showMultiple);
        if (stackMedium   != null) stackMedium.SetActive(showMedium);
        if (stackSingle   != null) stackSingle.SetActive(showSingle);

        // Apply sprite + staggered sortingOrder to every active renderer
        SpriteRenderer[] renderers = GetActiveRenderers();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (icon != null) renderers[i].sprite = icon;
            renderers[i].sortingLayerName = sortingLayerName;
            renderers[i].sortingOrder     = sortingOrder + i;
        }
    }

    /// <summary>
    /// Returns all SpriteRenderers inside whichever stack group is currently active.
    /// </summary>
    private SpriteRenderer[] GetActiveRenderers()
    {
        // Collect renderers from every group that is currently active.
        // Order: Multiple → Medium → Single so sortingOrder index is consistent.
        var list = new System.Collections.Generic.List<SpriteRenderer>();

        if (stackMultiple != null && stackMultiple.activeSelf)
            list.AddRange(stackMultiple.GetComponentsInChildren<SpriteRenderer>(true));
        if (stackMedium != null && stackMedium.activeSelf)
            list.AddRange(stackMedium.GetComponentsInChildren<SpriteRenderer>(true));
        if (stackSingle != null && stackSingle.activeSelf)
            list.AddRange(stackSingle.GetComponentsInChildren<SpriteRenderer>(true));

        return list.ToArray();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Check if the collider belongs to the local player.
    /// Requires the player object to have the "PlayerEntity" tag
    /// and a PhotonView that is owned by this client.
    /// </summary>
    private bool IsLocalPlayer(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return false;

        PhotonView pv = other.GetComponentInParent<PhotonView>();
        return pv != null && pv.IsMine;
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    /// <summary>Hide the view when this object is about to be destroyed.</summary>
    public void Cleanup()
    {
        HideItem();
        _data = null;
    }

    private void OnDisable()
    {
        StopBlinking();
        _playerInRange = false;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
