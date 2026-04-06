using UnityEngine;
using Photon.Pun;

/// <summary>
/// View layer for the crop-fertilizing action.
/// Responsibilities:
///   - Subscribes to UseToolService.OnFertilizerRequested.
///   - Resolves the target tile via CropTileSelector and delegates to CropFertilizingPresenter.
///   - Fires OnFertilizeAnimationRequested so the character animator can play the fertilizing clip.
/// Zero business logic — only calls Presenter methods.
/// </summary>
public class CropFertilizingView : MonoBehaviour
{
    public static event System.Action<bool> OnFertilizeResult;

    [Header("Fertilizing Settings")]
    [Tooltip("Range (in world units) within which the player can fertilize a tile.")]
    [SerializeField] private float fertilizingRange = 2f;

    [Header("Player Settings")]
    [Tooltip("Tag used to locate the local player GameObject.")]
    [SerializeField] private string playerTag = "PlayerEntity";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // ── Internal state ────────────────────────────────────────────────────
    private CropFertilizingPresenter presenter;
    private Transform playerTransform;

    // ─────────────────────────────────────────────────────────────────────
    private void Start()
    {
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        ICropFertilizingService service   = new CropFertilizingService(syncManager, showDebugLogs);
        presenter = new CropFertilizingPresenter(this, service);
        presenter.Initialize();

        UseToolService.OnFertilizerRequested += HandleFertilizerRequested;
    }

    private void Update()
    {
        // Re-find local player if reference becomes null (e.g. after scene reload).
        if (playerTransform == null)
            TryResolvePlayerTransform(out playerTransform);
    }

    private bool TryResolvePlayerTransform(out Transform resolvedTransform)
    {
        resolvedTransform = null;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        if (players == null || players.Length == 0)
            return false;

        // Online: always prefer the locally owned Photon entity.
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (PhotonNetwork.IsConnected && (pv == null || !pv.IsMine))
                continue;

            Transform centerPoint = player.transform.Find("CenterPoint");
            resolvedTransform = centerPoint != null ? centerPoint : player.transform;
            return true;
        }

        // Offline fallback: use the first tagged player entity.
        if (!PhotonNetwork.IsConnected)
        {
            Transform centerPoint = players[0].transform.Find("CenterPoint");
            resolvedTransform = centerPoint != null ? centerPoint : players[0].transform;
            return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        UseToolService.OnFertilizerRequested -= HandleFertilizerRequested;
    }

    // ── UseToolService event handler ──────────────────────────────────────

    private void HandleFertilizerRequested(FertilizerData fertilizer, Vector3 mouseWorldPos)
    {
        if (presenter == null || playerTransform == null)
        {
            OnFertilizeResult?.Invoke(false);
            return;
        }

        Vector2Int dummy   = new Vector2Int(int.MinValue, int.MinValue);
        Vector3 snappedTile = CropTileSelector.GetDirectionalTile(
            playerTransform.position, mouseWorldPos, fertilizingRange, ref dummy);

        if (snappedTile == Vector3.zero)
        {
            OnFertilizeResult?.Invoke(false);
            return;
        }

        presenter.HandleFertilizeAction(snappedTile);
    }

    // ── View callbacks (called by Presenter) ─────────────────────────────

    /// <summary>Called by the Presenter when fertilizing succeeds.</summary>
    public void OnFertilizeSuccess(Vector3 worldPosition)
    {
        OnFertilizeResult?.Invoke(true);

        if (showDebugLogs)
            Debug.Log($"[CropFertilizingView] ✓ Fertilized tile at {worldPosition}.");
    }

    /// <summary>Called by the Presenter when fertilizing fails.</summary>
    public void OnFertilizeFailed(Vector3 worldPosition)
    {
        OnFertilizeResult?.Invoke(false);

        if (showDebugLogs)
            Debug.Log($"[CropFertilizingView] Fertilizing failed at {worldPosition}.");
    }
}
