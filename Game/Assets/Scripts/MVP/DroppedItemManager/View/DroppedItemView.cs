using System.Collections;
using UnityEngine;
using Photon.Pun;
using TMPro;


/// <summary>
/// Visual representation of a single dropped item in the world.
/// Attach this to the DroppedItem prefab. 
/// Requires: SpriteRenderer, BoxCollider2D (trigger), child TextMeshPro for prompt.
/// 
/// Prefab setup:
///   DroppedItem (this script, SpriteRenderer, BoxCollider2D isTrigger)
///     └─ PickupPrompt (TextMeshPro "Press [F] to pick up", disabled by default)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DroppedItemView : MonoBehaviour, IDroppedItemView
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("SpriteRenderer showing the item icon.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

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

    // ── Runtime State ─────────────────────────────────────────────────────────

    private DroppedItemData _data;
    private Coroutine _blinkCoroutine;
    private bool _isBlinking;
    private bool _playerInRange;

    /// <summary>Presenter reference set by DroppedItemPresenter on creation.</summary>
    public DroppedItemPresenter Presenter { get; set; }

    /// <summary>Drop ID this view represents.</summary>
    public string DropId => _data?.dropId;

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

        if (icon != null)
        {
            spriteRenderer.sprite = icon;
        }
        else
        {
            Debug.LogWarning($"[DroppedItemView] No cached sprite for item '{data.itemId}'. Using fallback.");
        }

        // Configure sorting
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;

        // Position in world
        transform.position = new Vector3(data.worldX, data.worldY, 0f);

        // Prompt hidden by default
        SetPickupPromptVisible(false);

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

    /// <summary>Stop alpha oscillation and restore full opacity.</summary>
    public void StopBlinking()
    {
        if (!_isBlinking) return;
        _isBlinking = false;

        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        // Restore full alpha
        Color c = spriteRenderer.color;
        c.a = 1f;
        spriteRenderer.color = c;
    }

    /// <summary>Show or hide the "[F] Pick up" text prompt.</summary>
    public void SetPickupPromptVisible(bool visible)
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.gameObject.SetActive(visible);
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
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
            yield return null;
        }
    }

    // ── Trigger Detection ─────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLocalPlayer(other)) return;
        _playerInRange = true;
        SetPickupPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsLocalPlayer(other)) return;
        _playerInRange = false;
        SetPickupPromptVisible(false);
    }

    // ── Update (pickup input) ─────────────────────────────────────────────────

    private void Update()
    {
        if (!_playerInRange) return;
        if (_data == null) return;

        // Listen for pickup key press
        if (Input.GetKeyDown(pickupKey))
        {
            Presenter?.OnPickupRequested(_data.dropId);
        }
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

    private void OnDisable()
    {
        StopBlinking();
        _playerInRange = false;
    }
}
