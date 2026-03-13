using UnityEngine;
using Photon.Pun;
using System;

/// <summary>
/// Handles player input for applying pollen to a flowering crop.
/// Subscribe to UseToolService.OnPollenRequested — no direct key polling needed.
/// Place on the same GameObject as other CropManager views (e.g. CropManager).
/// </summary>
public class CropBreedingView : MonoBehaviourPun
{
    // ── Static result event — ItemUsageController subscribes to conditionally consume pollen ──
    /// <summary>Fired after a pollen-use attempt. True = breeding succeeded and pollen should be consumed.</summary>
    public static event Action<bool> OnBreedingResult;
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Interaction")]
    [Tooltip("Max distance from player to target tile.")]
    [SerializeField] private float interactionRadius = 2f;
    [Tooltip("Tag used to find the local Photon player.")]
    [SerializeField] private string playerTag = "PlayerEntity";

    // ── MVP ───────────────────────────────────────────────────────────────
    private CropBreedingPresenter presenter;

    // ── Runtime state ─────────────────────────────────────────────────────
    private Transform playerTransform;
    private Vector2Int lastTile = new Vector2Int(int.MinValue, int.MinValue);

    // ── Lifecycle ─────────────────────────────────────────────────────────
    // NOTE: Service construction is deferred to Start() so that WorldDataManager,
    // CropManagerView, and ChunkDataSyncManager have finished their own Awake()
    // before we resolve them. Building in Awake() captured null references, causing
    // CanApplyPollen() to silently fail on every attempt.
    void Start()
    {
        var service = new CropBreedingService(
            WorldDataManager.Instance,
            FindAnyObjectByType<CropManagerView>(),
            FindAnyObjectByType<ChunkDataSyncManager>());
        presenter = new CropBreedingPresenter(this, service);
        FindLocalPlayer();
    }
    void Update()     { if (playerTransform == null) FindLocalPlayer(); }

    void OnEnable()   => UseToolService.OnPollenRequested += HandlePollenUseRequested;
    void OnDisable()  => UseToolService.OnPollenRequested -= HandlePollenUseRequested;

    // ── Event handler ─────────────────────────────────────────────────────

    private void HandlePollenUseRequested(PollenData pollen, Vector3 mouseWorldPos)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[CropBreedingView] playerTransform is null.");
            OnBreedingResult?.Invoke(false);
            return;
        }
        if (pollen == null)
        {
            Debug.LogWarning("[CropBreedingView] pollen is null.");
            OnBreedingResult?.Invoke(false);
            return;
        }

        Debug.Log($"[CropBreedingView] HandlePollenUseRequested: pollen='{pollen.itemID}' crossResults={pollen.crossResults?.Length ?? 0} mousePos={mouseWorldPos}");

        // Reset deduplication guard — pollen is a discrete press, not a held action,
        // so the same tile must be targetable on every attempt.
        lastTile = new Vector2Int(int.MinValue, int.MinValue);

        Vector3 tile = CropTileSelector.GetDirectionalTile(
            playerTransform.position, mouseWorldPos, interactionRadius, ref lastTile);

        if (tile == Vector3.zero)
        {
            Debug.LogWarning($"[CropBreedingView] GetDirectionalTile returned zero for mousePos={mouseWorldPos}. Player may be out of range (interactionRadius={interactionRadius}).");
            OnBreedingResult?.Invoke(false);
            return;
        }

        Debug.Log($"[CropBreedingView] Applying pollen at tile {tile}.");
        presenter.HandleApplyPollen(pollen, tile);
    }

    // ── View callbacks (called by Presenter) ──────────────────────────────

    public void OnBreedingSuccess(Vector3 tilePos)
    {
        Debug.Log($"[CropBreedingView] Crossbreeding succeeded at ({tilePos.x:F0},{tilePos.y:F0}).");
        OnBreedingResult?.Invoke(true);
        // TODO: spawn particles, play sound
    }

    public void OnBreedingFailed(Vector3 tilePos)
    {
        Debug.LogWarning($"[CropBreedingView] Crossbreeding failed at ({tilePos.x:F0},{tilePos.y:F0}) — check CanApplyPollen logs above.");
        OnBreedingResult?.Invoke(false);
    }

    // ── Private helper ────────────────────────────────────────────────────

    private void FindLocalPlayer()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(playerTag))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Transform center = go.transform.Find("CenterPoint");
                playerTransform = center != null ? center : go.transform;
                return;
            }
        }
    }
}
