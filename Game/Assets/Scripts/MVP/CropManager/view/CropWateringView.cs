using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// View layer for the crop-watering action.
/// Responsibilities:
///   - Subscribes to UseToolService.OnWateringCanRequested.
///   - Resolves the target tile via CropTileSelector and delegates to CropWateringPresenter.
///   - Pushes the configurable WateringSpeedMultiplier to ICropGrowthService on Start.
///   - Fires OnWaterAnimationRequested so the character animator can play the watering clip.
/// Zero business logic — only calls Presenter methods.
/// </summary>
public class CropWateringView : MonoBehaviour
{
    [Header("Tile Reference")]
    [SerializeField] private TileBase wateredTile;

    [Header("Watering Settings")]
    [Tooltip("Range (in world units) within which the player can water a tile.")]
    [SerializeField] private float wateringRange = 2f;

    [Tooltip("Growth speed multiplier applied to watered crops (1 = no bonus, 2 = twice as fast).")]
    [SerializeField] private float wateringSpeedMultiplier = 2f;

    [Header("Player Settings")]
    [Tooltip("Tag used to locate the local player GameObject.")]
    [SerializeField] private string playerTag = "PlayerEntity";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    /// <summary>Fired when a tile is successfully watered. Carries the watering direction (cardinal Vector2).</summary>
    public static event System.Action<Vector2> OnWaterAnimationRequested;

    // ── Internal state ────────────────────────────────────────────────────
    private CropWateringPresenter presenter;
    private Transform playerTransform;
    private Vector3 _lastMouseWorldPos;

    // ─────────────────────────────────────────────────────────────────────
    private void Start()
    {
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        ICropWateringService service     = new CropWateringService(syncManager, showDebugLogs);
        presenter = new CropWateringPresenter(this, service);
        presenter.Initialize(wateredTile);

        if (wateredTile == null)
            Debug.LogError("[CropWateringView] WateredTile is not assigned in the Inspector!");

        // Push the serialized multiplier to the growth service so it takes effect immediately.
        if (CropManagerView.Instance != null && CropManagerView.Instance.GrowthService != null)
            CropManagerView.Instance.GrowthService.WateringSpeedMultiplier = wateringSpeedMultiplier;

        UseToolService.OnWateringCanRequested += HandleWateringCanRequested;
    }

    private void Update()
    {
        // Re-find local player if reference becomes null (e.g. after scene reload).
        if (playerTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                Transform centerPoint = playerEntity.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : playerEntity.transform;
            }
        }

        // Live-push multiplier changes made in the Inspector during Play mode.
        if (CropManagerView.Instance != null && CropManagerView.Instance.GrowthService != null)
        {
            if (!Mathf.Approximately(CropManagerView.Instance.GrowthService.WateringSpeedMultiplier, wateringSpeedMultiplier))
                CropManagerView.Instance.GrowthService.WateringSpeedMultiplier = wateringSpeedMultiplier;
        }
    }

    private void OnDestroy()
    {
        UseToolService.OnWateringCanRequested -= HandleWateringCanRequested;
    }

    // ── UseToolService event handler ──────────────────────────────────────

    private void HandleWateringCanRequested(ToolData tool, Vector3 mouseWorldPos)
    {
        if (presenter == null || playerTransform == null) return;

        _lastMouseWorldPos = mouseWorldPos;

        Vector2Int dummy   = new Vector2Int(int.MinValue, int.MinValue);
        Vector3 snappedTile = CropTileSelector.GetDirectionalTile(
            playerTransform.position, mouseWorldPos, wateringRange, ref dummy);

        if (snappedTile == Vector3.zero) return;

        presenter.HandleWaterAction(snappedTile);
    }

    // ── View callbacks (called by Presenter) ─────────────────────────────

    /// <summary>Called by the Presenter when watering succeeds.</summary>
    public void OnWaterSuccess(Vector3 worldPosition)
    {
        if (showDebugLogs)
            Debug.Log($"[CropWateringView] ✓ Watered tile at {worldPosition}.");

        Vector2 dir = Vector2.zero;
        if (playerTransform != null && _lastMouseWorldPos != Vector3.zero)
        {
            float rawX = _lastMouseWorldPos.x - playerTransform.position.x;
            dir = new Vector2(Mathf.Sign(rawX), 0f);
        }

        OnWaterAnimationRequested?.Invoke(dir);
    }

    /// <summary>Called by the Presenter when watering fails.</summary>
    public void OnWaterFailed(Vector3 worldPosition)
    {
        if (showDebugLogs)
            Debug.Log($"[CropWateringView] Watering failed at {worldPosition}.");
    }
}
