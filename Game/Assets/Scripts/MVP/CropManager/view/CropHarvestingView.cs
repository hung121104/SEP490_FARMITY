using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class CropHarvestingView : MonoBehaviourPun
{
    [Header("Harvest Settings")]
    public float checkRadius = 2f; // radius (in world units) to search for crops
    public KeyCode harvestKey = KeyCode.F;
    public bool allowHoldToHarvest = true;
    [Tooltip("How often (seconds) to attempt harvesting while holding the harvest key.")]
    public float harvestRepeatInterval = 0.15f;
    [Tooltip("Tag to find the player GameObject (used to locate CenterPoint child)")]
    public string playerTag = "PlayerEntity";
    [Tooltip("How often (seconds) to scan for nearby harvestable crops")]
    public float checkInterval = 0.12f; // default ~8 checks/sec
    private float nextScanTime = 0f;
    private float holdTimer = 0f;

    [Header("UI")]
    public Canvas uiCanvas; // optional: assign a screen-space canvas; if null one will be created on the main camera
    public TextMeshProUGUI promptText; // optional: assign a TMP UI element to reuse

    private Vector3 currentHarvestTile = Vector3.zero;
    private bool canHarvestNearby = false;
    private Vector2Int lastHarvestTile = new Vector2Int(int.MinValue, int.MinValue);

    // Cached managers
    private WorldDataManager worldDataManager;
    private CropManagerView cropManagerView;
    private ChunkDataSyncManager syncManager;
    private ChunkLoadingManager loadingManager;
    private InventoryGameView inventoryGameView;
    private Transform playerTransform;

    void Awake()
    {
        worldDataManager = WorldDataManager.Instance;
        cropManagerView = FindAnyObjectByType<CropManagerView>();
        syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        loadingManager = FindAnyObjectByType<ChunkLoadingManager>();
        inventoryGameView = FindAnyObjectByType<InventoryGameView>();

        if (inventoryGameView == null)
            Debug.LogWarning("[CropHarvestingView] InventoryGameView not found in scene! Harvested items will not be added to inventory.");
    }

    void Start()
    {
        // Find the local player (the one owned by this client)
        FindLocalPlayer();
        SetupUIIfNeeded();
    }

    void Update()
    {
        // Re-check if player becomes null (e.g., respawn)
        if (playerTransform == null)
        {
            FindLocalPlayer();
        }

        if (playerTransform == null) return;

        // Run proximity scan at the configured interval to reduce work per-frame
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + Mathf.Max(0.01f, checkInterval);
            ScanForNearbyHarvestable();
        }

        if (allowHoldToHarvest)
        {
            if (Input.GetKeyDown(harvestKey))
            {
                // Immediate harvest on key down
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
            {
                HandleHarvestInput();
            }

            if (Input.GetKeyUp(harvestKey))
            {
                lastHarvestTile = new Vector2Int(int.MinValue, int.MinValue);
            }
        }
    }

    /// <summary>
    /// Evaluates the tile under the cursor and performs a harvest if the crop is ready.
    /// Called on immediate key-down and on every hold-repeat tick.
    /// </summary>
    private void HandleHarvestInput()
    {
        Vector3 target = GetDirectionalTileForHarvesting();
        if (target == Vector3.zero) return;

        int wx = Mathf.FloorToInt(target.x);
        int wy = Mathf.FloorToInt(target.y);

        if (worldDataManager != null
            && worldDataManager.HasCropAtWorldPosition(target)
            && cropManagerView != null
            && cropManagerView.IsCropReadyToHarvest(wx, wy))
        {
            PerformHarvest(target);
        }
    }

    private void SetupUIIfNeeded()
    {
        // Check if promptText is valid and usable
        if (promptText != null && promptText.gameObject != null) return;

        // Ensure there's a Canvas to host the prompt (screen-space overlay)
        if (uiCanvas == null)
        {
            // Try to find existing Canvas on main camera
            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null)
            {
                uiCanvas = existing;
            }
            else
            {
                GameObject canvasGO = new GameObject("HarvestPromptCanvas");
                uiCanvas = canvasGO.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
        }

        // Create TextMeshProUGUI prompt
        GameObject txt = new GameObject("HarvestPromptText");
        txt.transform.SetParent(uiCanvas.transform, false);
        promptText = txt.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        // try to assign default TMP font asset if available
        try
        {
            promptText.font = TMPro.TMP_Settings.defaultFontAsset;
        }
        catch { }
        promptText.fontSize = 18;
        promptText.color = Color.white;
        promptText.raycastTarget = false;
        promptText.enabled = false;

        RectTransform rt = promptText.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(300, 30);
    }

    /// <summary>
    /// Finds the local player (the one controlled by this client) in multiplayer.
    /// </summary>
    private void FindLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                // Found the local player
                Transform centerPoint = player.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : player.transform;
                return;
            }
        }
    }

    private void ScanForNearbyHarvestable()
    {
        canHarvestNearby = false;
        currentHarvestTile = Vector3.zero;

        if (worldDataManager == null || cropManagerView == null || playerTransform == null) return;

        Vector3 pos = playerTransform.position;

        // check the tile player stands on and immediate neighbors (3x3)
        int px = Mathf.RoundToInt(pos.x);
        int py = Mathf.RoundToInt(pos.y);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector3 tilePos = new Vector3(px + dx, py + dy, 0);
                float dist = Vector2.Distance(new Vector2(pos.x, pos.y), new Vector2(tilePos.x, tilePos.y));
                if (dist > checkRadius) continue;

                // Is there a crop here?
                if (!worldDataManager.HasCropAtWorldPosition(tilePos)) continue;

                int worldX = Mathf.FloorToInt(tilePos.x);
                int worldY = Mathf.FloorToInt(tilePos.y);

                if (cropManagerView.IsCropReadyToHarvest(worldX, worldY))
                {
                    canHarvestNearby = true;
                    currentHarvestTile = tilePos;
                    ShowPrompt(tilePos);
                    return;
                }
            }
        }

        HidePrompt();
    }

    private void ShowPrompt(Vector3 tilePos)
    {
        if (promptText == null) return;

        promptText.enabled = true;
        //harvest promt
        promptText.text = $"{harvestKey.ToString()}";
    }

    private void HidePrompt()
    {
        if (promptText == null) return;
        promptText.enabled = false;
    }

    private void PerformHarvest(Vector3 tilePos)
    {
        if (worldDataManager == null) return;

        int worldX = Mathf.FloorToInt(tilePos.x);
        int worldY = Mathf.FloorToInt(tilePos.y);
        Vector3 worldPos = new Vector3(worldX, worldY, 0);

        // ── Resolve the harvested item BEFORE removing the crop data ──
        ItemDataSO harvestedItem = null;
        if (worldDataManager.TryGetCropAtWorldPosition(worldPos, out CropChunkData.TileData tileData)
            && cropManagerView != null
            && !string.IsNullOrEmpty(tileData.PlantId))
        {
            PlantDataSO plantData = cropManagerView.GetPlantData(tileData.PlantId);
            if (plantData != null)
                harvestedItem = plantData.HarvestedItem;
            else
                Debug.LogWarning($"[CropHarvestingView] No PlantDataSO found for plantId '{tileData.PlantId}'.");
        }

        // ── Remove the crop from world data ──
        bool removed = worldDataManager.RemoveCropAtWorldPosition(worldPos);
        if (!removed)
        {
            Debug.LogWarning($"[Harvest] Failed to remove crop at ({worldX},{worldY})");
            return;
        }

        // Unregister growth/tracker and update visuals
        if (cropManagerView != null)
            cropManagerView.UnregisterCrop(worldX, worldY);

        // Broadcast removal to other players
        if (syncManager != null)
            syncManager.BroadcastCropRemoved(worldX, worldY);

        // Refresh visuals on this client
        if (loadingManager != null)
        {
            Vector2Int chunkPos = WorldDataManager.Instance.WorldToChunkCoords(worldPos);
            loadingManager.RefreshChunkVisuals(chunkPos);
        }

        // ── Add the harvested item to the player's inventory ──
        if (harvestedItem != null)
        {
            if (inventoryGameView != null)
            {
                bool added = inventoryGameView.AddItem(harvestedItem, 1);
                if (!added)
                    Debug.LogWarning($"[CropHarvestingView] Inventory full — could not add '{harvestedItem.itemName}'.");
                else
                    Debug.Log($"[CropHarvestingView] Added '{harvestedItem.itemName}' to inventory from harvest at ({worldX},{worldY}).");
            }
        }
        else
        {
            Debug.LogWarning($"[CropHarvestingView] Crop at ({worldX},{worldY}) has no HarvestedItem set in its PlantDataSO.");
        }

        HidePrompt();
    }

    /// <summary>
    /// Determine directional tile to harvest using 8-direction logic.
    /// Delegates to the shared <see cref="CropTileSelector"/> utility.
    /// </summary>
    private Vector3 GetDirectionalTileForHarvesting()
    {
        if (Camera.main == null || playerTransform == null)
            return Vector3.zero;

        Vector3 playerPos = playerTransform.position;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        return CropTileSelector.GetDirectionalTile(
            playerPos,
            mouseWorldPos,
            checkRadius,
            ref lastHarvestTile);
    }
}
