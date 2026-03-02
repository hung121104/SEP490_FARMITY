using UnityEngine;

/// <summary>
/// Presenter for a single dropped item. 
/// Manages the despawn timer, blink trigger, and forwards pickup requests
/// to the DroppedItemManager.
///
/// Lifecycle:
///   1. Created by DroppedItemManager when spawning a visual.
///   2. Calls view.ShowItem(data) immediately.
///   3. Every frame checks remaining time; triggers blink at <=30s.
///   4. When timer reaches 0, MasterClient broadcasts despawn event.
///   5. Destroyed by DroppedItemManager on despawn or pickup.
/// </summary>
public class DroppedItemPresenter : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────────────

    /// <summary>Seconds remaining when blink animation should start.</summary>
    private const float BLINK_THRESHOLD_SECONDS = 30f;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private DroppedItemData _data;
    private IDroppedItemView _view;
    private bool _blinkStarted;
    private bool _despawnBroadcast;

    /// <summary>Drop ID this presenter manages.</summary>
    public string DropId => _data?.dropId;

    // ── Initialization ────────────────────────────────────────────────────────

    /// <summary>
    /// Bind the presenter to a data model and a view.
    /// Called once by DroppedItemManager right after instantiation.
    /// </summary>
    /// <param name="data">The dropped item data from service/sync.</param>
    /// <param name="view">The IDroppedItemView on the same GameObject.</param>
    public void Initialize(DroppedItemData data, IDroppedItemView view)
    {
        _data = data;
        _view = view;
        _blinkStarted = false;
        _despawnBroadcast = false;

        // Wire the view back-reference so it can call OnPickupRequested
        if (view is DroppedItemView concreteView)
        {
            concreteView.Presenter = this;
        }

        // Show the visual immediately
        _view.ShowItem(data);
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
            _view.StartBlinking();
        }

        // Despawn when expired — only MasterClient broadcasts to avoid duplicate events
        if (!_despawnBroadcast && _data.IsExpired)
        {
            _despawnBroadcast = true;

            if (Photon.Pun.PhotonNetwork.IsMasterClient)
            {
                // Notify all clients to despawn this item
                var syncManager = FindAnyObjectByType<DroppedItemSyncManager>();
                if (syncManager != null)
                {
                    syncManager.BroadcastItemDespawn(_data.dropId);
                }
            }

            // The actual destroy will be handled by DroppedItemManager 
            // when it processes the despawn event from SyncManager.
        }
    }

    // ── Pickup ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by DroppedItemView when the local player presses the pickup key.
    /// Delegates to DroppedItemManager which handles the Photon request flow.
    /// </summary>
    /// <param name="dropId">The unique drop ID to pick up.</param>
    public void OnPickupRequested(string dropId)
    {
        if (_data == null) return;
        if (_data.IsExpired)
        {
            Debug.Log($"[DroppedItemPresenter] Item '{dropId}' already expired, ignoring pickup.");
            return;
        }

        // Forward to manager (Phase 6)
        var manager = DroppedItemManager.Instance;
        if (manager != null)
        {
            manager.RequestPickupItem(dropId);
        }
        else
        {
            Debug.LogError("[DroppedItemPresenter] DroppedItemManager.Instance is null!");
        }
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    /// <summary>Hide the view when this presenter is about to be destroyed.</summary>
    public void Cleanup()
    {
        _view?.HideItem();
        _data = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
