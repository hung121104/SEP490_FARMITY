using UnityEngine;
using Photon.Pun;

/// <summary>
/// Handles player input for applying pollen to a flowering crop.
/// Subscribe to UseToolService.OnPollenRequested — no direct key polling needed.
/// Place on the same GameObject as other CropManager views (e.g. CropManager).
/// </summary>
public class CropBreedingView : MonoBehaviourPun
{
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
    void Awake()
    {
        var service = new CropBreedingService(
            WorldDataManager.Instance,
            FindAnyObjectByType<CropManagerView>(),
            FindAnyObjectByType<ChunkDataSyncManager>());
        presenter = new CropBreedingPresenter(this, service);
    }

    void Start()      => FindLocalPlayer();
    void Update()     { if (playerTransform == null) FindLocalPlayer(); }

    void OnEnable()   => UseToolService.OnPollenRequested += HandlePollenUseRequested;
    void OnDisable()  => UseToolService.OnPollenRequested -= HandlePollenUseRequested;

    // ── Event handler ─────────────────────────────────────────────────────

    private void HandlePollenUseRequested(PollenDataSO pollen, Vector3 mouseWorldPos)
    {
        if (playerTransform == null || pollen == null) return;

        Vector3 tile = CropTileSelector.GetDirectionalTile(
            playerTransform.position, mouseWorldPos, interactionRadius, ref lastTile);
        if (tile == Vector3.zero) return;

        presenter.HandleApplyPollen(pollen, tile);
    }

    // ── View callbacks (called by Presenter) ──────────────────────────────

    public void OnBreedingSuccess(Vector3 tilePos)
    {
        Debug.Log($"[CropBreedingView] Crossbreeding succeeded at ({tilePos.x:F0},{tilePos.y:F0}).");
        // TODO: spawn particles, play sound
    }

    public void OnBreedingFailed(Vector3 tilePos)
    {
        // Silent — crop is not flowering, already pollinated, or wrong species
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
