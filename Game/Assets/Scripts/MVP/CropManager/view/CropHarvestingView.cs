using UnityEngine;
using Photon.Pun;
using System.Reflection;
public class CropHarvestingView : MonoBehaviourPun
{
    // ── Settings ──────────────────────────────────────────────────────────
    [Header("Harvest Settings")]
    public float checkRadius = 2f;
    public KeyCode harvestKey = KeyCode.Mouse0;
    public bool allowHoldToHarvest = true;
    [Tooltip("How often (seconds) to attempt harvesting while holding the harvest key.")]
    public float harvestRepeatInterval = 0.15f;
    [Tooltip("Tag to find the player GameObject (used to locate CenterPoint child)")]
    public string playerTag = "PlayerEntity";
    [Tooltip("How often (seconds) to scan for nearby harvestable crops")]
    public float checkInterval = 0.12f;

    // ── Runtime state ─────────────────────────────────────────────────────
    private float nextScanTime = 0f;
    private float holdTimer    = 0f;
    private Vector2Int lastHarvestTile = new Vector2Int(int.MinValue, int.MinValue);

    // ── MVP ───────────────────────────────────────────────────────────────
    private CropHarvestingPresenter presenter;
    public CropHarvestingPresenter GetPresenter() => presenter;

    // ── Dependencies ──────────────────────────────────────────────────────
    private Transform playerTransform;

    // Hotbar interception
    private HotbarView hotbarView;
    private FieldInfo hotbarLeftClickField;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    void Awake()
    {
        var service = new CropHarvestingService(
            WorldDataManager.Instance,
            FindAnyObjectByType<CropManagerView>(),
            FindAnyObjectByType<ChunkDataSyncManager>(),
            FindAnyObjectByType<ChunkLoadingManager>(),
            FindAnyObjectByType<InventoryGameView>()
        );

        presenter = new CropHarvestingPresenter(this, service);

        hotbarView = FindAnyObjectByType<HotbarView>();
        if (hotbarView != null)
            hotbarLeftClickField = typeof(HotbarView).GetField("enableLeftClick", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    void Start()
    {
        FindLocalPlayer();
    }

    // ── Update ────────────────────────────────────────────────────────────
    void Update()
    {
        if (playerTransform == null) FindLocalPlayer();
        if (playerTransform == null) return;

        if (allowHoldToHarvest)
        {
            if (Input.GetKeyDown(harvestKey))
            {
                HandleHarvestInput();
                holdTimer = harvestRepeatInterval;
            }

            if (Input.GetKey(harvestKey))
            {
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f)
                {
                    HandleHarvestInput();
                    holdTimer = harvestRepeatInterval;
                }
            }

            if (Input.GetKeyUp(harvestKey))
            {
                holdTimer = 0f;
                lastHarvestTile = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
        else
        {
            if (Input.GetKeyDown(harvestKey))
                HandleHarvestInput();

            if (Input.GetKeyUp(harvestKey))
                lastHarvestTile = new Vector2Int(int.MinValue, int.MinValue);
        }

        ManageHotbarInterception();
    }

    // ── View callbacks (called by Presenter) ──────────────────────────────

    public void OnHarvestSuccess(Vector3 tilePos, ItemDataSO harvestedItem) { }

    public void OnHarvestFailed(Vector3 tilePos) { }

    // ── Private helpers ───────────────────────────────────────────────────

    private void HandleHarvestInput()
    {
        Vector3 target = GetDirectionalTileForHarvesting();
        if (target == Vector3.zero) return;
        presenter.HandleHarvestAction(target);
    }

    private void ManageHotbarInterception()
    {
        if (hotbarView == null || hotbarLeftClickField == null) return;

        bool targetingReadyCrop = false;

        if (harvestKey == KeyCode.Mouse0 && Camera.main != null && playerTransform != null)
        {
            Vector3 playerPos     = playerTransform.position;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int dummyTile  = new Vector2Int(int.MinValue, int.MinValue);

            Vector3 target = CropTileSelector.GetDirectionalTile(playerPos, mouseWorldPos, checkRadius, ref dummyTile);
            if (target != Vector3.zero)
                targetingReadyCrop = presenter.IsReadyToHarvest(target);
        }

        hotbarLeftClickField.SetValue(hotbarView, !targetingReadyCrop);
    }

    private void FindLocalPlayer()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag(playerTag))
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Transform center = player.transform.Find("CenterPoint");
                playerTransform = center != null ? center : player.transform;
                return;
            }
        }
    }

    private Vector3 GetDirectionalTileForHarvesting()
    {
        if (Camera.main == null || playerTransform == null) return Vector3.zero;

        return CropTileSelector.GetDirectionalTile(
            playerTransform.position,
            Camera.main.ScreenToWorldPoint(Input.mousePosition),
            checkRadius,
            ref lastHarvestTile);
    }
}
