using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MVP View — Item-pickup feed toast.
/// Shows a stacked, auto-dismissing "+N ItemName" entry whenever an item
/// is successfully added to the local player's inventory.
///
/// Placement: attach to a persistent HUD canvas object.
///
/// Prefab requirements for entryPrefab:
///   - CanvasGroup on the root GameObject
///   - One Image  component (anywhere in children) → item icon
///   - One TextMeshProUGUI component (anywhere in children) → pickup label
///
/// Wire-up:
///   InventoryGameView calls ItemPickupToastView.NotifyItemPickedUp()
///   after a successful AddItem().
/// </summary>
public class ItemPickupToastView : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static ItemPickupToastView Instance { get; private set; }

    // ── Static Event ──────────────────────────────────────────────────────────
    /// <summary>
    /// Fire this whenever an item is successfully added to inventory.
    /// InventoryGameView already calls NotifyItemPickedUp() — other systems
    /// (fishing, quests, crafting loot) may also call it directly.
    /// </summary>
    public static event System.Action<string, int> OnItemPickedUp;

    /// <summary>Convenience helper so callers don't need to touch the event directly.</summary>
    public static void NotifyItemPickedUp(string itemId, int quantity)
        => OnItemPickedUp?.Invoke(itemId, quantity);

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Entry Prefab")]
    [Tooltip("Root must have a CanvasGroup. Needs one Image + one TextMeshProUGUI child.")]
    [SerializeField] private GameObject entryPrefab;

    [Header("Layout")]
    [Tooltip("VerticalLayoutGroup transform where entries are spawned.")]
    [SerializeField] private Transform entriesContainer;

    [Header("Settings")]
    [SerializeField] private float displayDuration  = 2.5f;
    [SerializeField] private float fadeDuration     = 0.35f;
    [SerializeField] private int   maxVisibleEntries = 6;

    // ── Runtime State ─────────────────────────────────────────────────────────
    // Explicit queue so the cap check never depends on Transform.childCount,
    // which only updates after Unity's deferred Destroy() (end-of-frame).
    private readonly System.Collections.Generic.Queue<GameObject> _activeEntries = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()  => OnItemPickedUp += HandleItemPickedUp;
    private void OnDisable() => OnItemPickedUp -= HandleItemPickedUp;

    // ── Event Handler ─────────────────────────────────────────────────────────

    private void HandleItemPickedUp(string itemId, int quantity)
    {
        string itemName = itemId;
        Sprite icon     = null;

        if (ItemCatalogService.Instance != null)
        {
            var data = ItemCatalogService.Instance.GetItemData(itemId);
            if (data != null) itemName = data.itemName;
            icon = ItemCatalogService.Instance.GetCachedSprite(itemId);
        }

        SpawnEntry(icon, itemName, quantity);
    }

    // ── Entry Management ──────────────────────────────────────────────────────

    private void SpawnEntry(Sprite icon, string itemName, int quantity)
    {
        if (entryPrefab == null || entriesContainer == null) return;

        // Evict oldest entry when cap is reached.
        // Uses _activeEntries count — NOT Transform.childCount — because
        // Destroy() is deferred to end-of-frame and childCount doesn't
        // update immediately, which would turn a while-loop into an infinite loop.
        if (_activeEntries.Count >= maxVisibleEntries)
        {
            var oldest = _activeEntries.Dequeue();
            if (oldest != null) Destroy(oldest);
        }

        var go = Instantiate(entryPrefab, entriesContainer);
        _activeEntries.Enqueue(go);

        // ── Icon ──
        var img = go.GetComponentInChildren<Image>();
        if (img != null)
        {
            img.sprite  = icon;
            img.enabled = icon != null;
        }

        // ── Label ──
        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = quantity > 1 ? $"+{quantity} {itemName}" : $"+1 {itemName}";

        // ── Fade animation ──
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        StartCoroutine(AnimateEntry(go, cg));
    }

    private IEnumerator AnimateEntry(GameObject go, CanvasGroup cg)
    {
        // Fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            if (go == null) yield break;
            t       += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        cg.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade out
        t = 0f;
        while (t < fadeDuration)
        {
            if (go == null) yield break;
            t       += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(1f - t / fadeDuration);
            yield return null;
        }

        if (go == null) yield break;

        // Remove from tracking queue before destroying
        // (uses a new queue to avoid modifying while iterating)
        var remaining = new System.Collections.Generic.Queue<GameObject>();
        while (_activeEntries.Count > 0)
        {
            var entry = _activeEntries.Dequeue();
            if (entry != null && entry != go)
                remaining.Enqueue(entry);
        }
        foreach (var e in remaining)
            _activeEntries.Enqueue(e);

        Destroy(go);
    }
}
