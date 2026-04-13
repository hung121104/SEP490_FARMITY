using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using System.Reflection;
public class CropHarvestingView : MonoBehaviourPun
{
    // ── Settings ──────────────────────────────────────────────────────────
    [Header("Harvest Settings")]
    public float checkRadius = 2f;
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
    private bool  _isHoldingHarvest = false;
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
        hotbarView = FindAnyObjectByType<HotbarView>();
        if (hotbarView != null)
            hotbarLeftClickField = typeof(HotbarView).GetField("enableLeftClick", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    void Start()
    {
        var cropManagerView  = FindAnyObjectByType<CropManagerView>();
        var syncManager      = FindAnyObjectByType<ChunkDataSyncManager>();

        // Lazy provider: avoids race when InventoryGameView is on an inactive UI panel
        // or hasn't finished its own Awake by the time these services are constructed.
        var pollenService = new CropPollenService(
            WorldDataManager.Instance,
            cropManagerView,
            InventoryServiceLocator.Resolve,
            syncManager
        );

        var service = new CropHarvestingService(
            WorldDataManager.Instance,
            cropManagerView,
            syncManager,
            FindAnyObjectByType<ChunkLoadingManager>(),
            InventoryServiceLocator.Resolve,
            pollenService
        );

        presenter = new CropHarvestingPresenter(this, service);

        FindLocalPlayer();
    }

    void OnEnable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.Interact.started  += OnHarvestPerformed;
        InputManager.Instance.Interact.canceled += OnHarvestCanceled;
    }

    void OnDisable()
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.Interact.started  -= OnHarvestPerformed;
        InputManager.Instance.Interact.canceled -= OnHarvestCanceled;
    }

    private void OnHarvestPerformed(InputAction.CallbackContext ctx)
    {
        if (playerTransform == null) 
        {
            Debug.LogWarning("[CropHarvestingView] OnHarvestPerformed: playerTransform is null");
            return;
        }
        Debug.Log("[CropHarvestingView] OnHarvestPerformed: calling HandleHarvestInput");
        HandleHarvestInput();
        if (allowHoldToHarvest)
        {
            _isHoldingHarvest = true;
            holdTimer = harvestRepeatInterval;
        }
    }

    private void OnHarvestCanceled(InputAction.CallbackContext ctx)
    {
        _isHoldingHarvest = false;
        holdTimer = 0f;
        lastHarvestTile = new Vector2Int(int.MinValue, int.MinValue);
    }

    // ── Update ────────────────────────────────────────────────────────────
    void Update()
    {
        if (presenter == null) return;
        if (playerTransform == null) FindLocalPlayer();
        if (playerTransform == null) return;

        if (allowHoldToHarvest && _isHoldingHarvest)
        {
            holdTimer -= Time.deltaTime;
            if (holdTimer <= 0f)
            {
                // Reset deduplication so the same tile can be targeted again on repeat ticks.
                lastHarvestTile = new Vector2Int(int.MinValue, int.MinValue);
                HandleHarvestInput();
                holdTimer = harvestRepeatInterval;
            }
        }

        ManageHotbarInterception();
    }

    // ── View callbacks (called by Presenter) ──────────────────────────────

    public void OnHarvestSuccess(Vector3 tilePos, ItemData harvestedItem) { }

    public void OnHarvestFailed(Vector3 tilePos) { }

    /// <summary>Called by the Presenter when pollen was successfully collected.</summary>
    public void OnPollenCollectSuccess(Vector3 tilePos, PollenData pollen)
    {
        // TODO: play a VFX/SFX, show a pickup notification, etc.
        Debug.Log($"[CropHarvestingView] Pollen '{pollen?.itemName}' collected at {tilePos}.");
    }

    /// <summary>Called by the Presenter when pollen collection failed (wrong stage, full inventory, etc.).</summary>
    public void OnPollenCollectFailed(Vector3 tilePos)
    {
        // TODO: play a failure sound or show a UI hint.
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private void HandleHarvestInput()
    {
        // If the player has pollen selected, the left-click is for cross-breeding (via the
        // hotbar item-use pipeline), not for harvesting or collecting pollen. Skip entirely
        // to avoid accidentally collecting pollen from the intended breeding target.
        if (hotbarView?.GetCurrentItem()?.ItemData is PollenData) 
        {
            Debug.Log("[CropHarvestingView] HandleHarvestInput: pollen is selected, skipping");
            return;
        }

        Vector3 target = GetDirectionalTileForHarvesting();
        Debug.Log($"[CropHarvestingView] HandleHarvestInput: target = {target}");
        if (target == Vector3.zero) 
        {
            Debug.LogWarning("[CropHarvestingView] HandleHarvestInput: no valid target tile found");
            return;
        }

        // Context-sensitive: prefer pollen collection when the crop is flowering.
        if (presenter.IsReadyToCollectPollen(target))
        {
            Debug.Log($"[CropHarvestingView] HandleHarvestInput: collecting pollen at {target}");
            presenter.HandleCollectPollenAction(target);
        }
        else
        {
            Debug.Log($"[CropHarvestingView] HandleHarvestInput: harvesting at {target}");
            presenter.HandleHarvestAction(target);
        }
    }

    private void ManageHotbarInterception()
    {
        if (hotbarView == null || hotbarLeftClickField == null) return;

        bool targetingReadyCrop = false;

        if (Camera.main != null && playerTransform != null)
        {
            // If the player has pollen selected, never suppress the hotbar click —
            // they intend to APPLY pollen, not collect it or harvest.
            if (hotbarView.GetCurrentItem()?.ItemData is PollenData)
            {
                hotbarLeftClickField.SetValue(hotbarView, true);
                return;
            }

            Vector3 playerPos     = playerTransform.position;
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector2Int dummyTile  = new Vector2Int(int.MinValue, int.MinValue);

            Vector3 target = CropTileSelector.GetDirectionalTile(playerPos, mouseWorldPos, checkRadius, ref dummyTile);
            if (target != Vector3.zero)
                targetingReadyCrop = presenter.IsReadyToHarvest(target)
                                  || presenter.IsReadyToCollectPollen(target);
        }

        hotbarLeftClickField.SetValue(hotbarView, !targetingReadyCrop);
    }

    private void FindLocalPlayer()
    {
        playerTransform = null;

        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        if (players == null || players.Length == 0)
            return;

        // Online: always prefer the locally owned Photon entity.
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (PhotonNetwork.IsConnected && (pv == null || !pv.IsMine))
                continue;

            Transform center = player.transform.Find("CenterPoint");
            playerTransform = center != null ? center : player.transform;
            return;
        }

        // Offline fallback: use the first tagged player entity.
        if (!PhotonNetwork.IsConnected)
        {
            Transform center = players[0].transform.Find("CenterPoint");
            playerTransform = center != null ? center : players[0].transform;
        }
    }

    private Vector3 GetDirectionalTileForHarvesting()
    {
        if (Camera.main == null || playerTransform == null) return Vector3.zero;

        return CropTileSelector.GetDirectionalTile(
            playerTransform.position,
            GetMouseWorldPosition(),
            checkRadius,
            ref lastHarvestTile);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 screenPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;
        return worldPos;
    }
}
