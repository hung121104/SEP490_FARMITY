using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class CropHarvestingView : MonoBehaviourPun
{
    [Header("Harvest Settings")]
    public float checkRadius = 1.5f; // radius (in world units) to search for crops
    public KeyCode harvestKey = KeyCode.F;
    [Tooltip("Tag to find the player GameObject (used to locate CenterPoint child)")]
    public string playerTag = "PlayerEntity";
    [Tooltip("How often (seconds) to scan for nearby harvestable crops")]
    public float checkInterval = 0.12f; // default ~8 checks/sec
    private float nextScanTime = 0f;

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
    private Transform playerTransform;

    void Awake()
    {
        worldDataManager = WorldDataManager.Instance;
        cropManagerView = FindAnyObjectByType<CropManagerView>();
        syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        loadingManager = FindAnyObjectByType<ChunkLoadingManager>();
    }

    void Start()
    {
        if (!photonView.IsMine)
        {
            // only the local player should run the UI / input logic
            enabled = false;
            return;
        }

        SetupUIIfNeeded();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Run proximity scan at the configured interval to reduce work per-frame
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + Mathf.Max(0.01f, checkInterval);
            ScanForNearbyHarvestable();
        }

        if (Input.GetKeyDown(harvestKey))
        {
            Vector3 target = GetDirectionalTileForHarvesting();
            if (target != Vector3.zero)
            {
                // Only attempt harvest if there's a ready crop
                int wx = Mathf.FloorToInt(target.x);
                int wy = Mathf.FloorToInt(target.y);
                if (worldDataManager != null && worldDataManager.HasCropAtWorldPosition(target) && cropManagerView.IsCropReadyToHarvest(wx, wy))
                {
                    PerformHarvest(target);
                }
            }
        }
    }

    private void SetupUIIfNeeded()
    {
        if (promptText != null) return;

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

    private void ScanForNearbyHarvestable()
    {
        canHarvestNearby = false;
        currentHarvestTile = Vector3.zero;

        if (worldDataManager == null || cropManagerView == null) return;

        // Determine player reference (try cached first)
        if (playerTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                Transform centerPoint = playerEntity.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : playerEntity.transform;
            }
        }

        Vector3 pos = playerTransform != null ? playerTransform.position : transform.position;

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
        promptText.text = $"{harvestKey.ToString()} to harvest";
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

        HidePrompt();
    }

    /// <summary>
    /// Determine directional tile to harvest using same 8-direction logic as plowing.
    /// Uses player's CenterPoint if available; returns Vector3.zero if invalid.
    /// </summary>
    private Vector3 GetDirectionalTileForHarvesting()
    {
        if (playerTransform == null)
        {
            GameObject playerEntity = GameObject.FindGameObjectWithTag(playerTag);
            if (playerEntity != null)
            {
                Transform centerPoint = playerEntity.transform.Find("CenterPoint");
                playerTransform = centerPoint != null ? centerPoint : playerEntity.transform;
            }
        }

        if (Camera.main == null || playerTransform == null)
        {
            return Vector3.zero;
        }

        Vector3 playerPos = playerTransform.position;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        int playerTileX = Mathf.RoundToInt(playerPos.x);
        int playerTileY = Mathf.RoundToInt(playerPos.y);

        Vector2 direction = new Vector2(mouseWorldPos.x - playerPos.x, mouseWorldPos.y - playerPos.y);
        float distance = direction.magnitude;

        // if mouse very close, target player's tile
        if (distance < 0.5f)
        {
            Vector2Int playerTileCoords = new Vector2Int(playerTileX, playerTileY);
            if (playerTileCoords == lastHarvestTile)
                return Vector3.zero;

            lastHarvestTile = playerTileCoords;
            return new Vector3(playerTileX, playerTileY, 0);
        }

        direction.Normalize();

        int offsetX = 0;
        int offsetY = 0;
        if (direction.x > 0.4f) offsetX = 1;
        else if (direction.x < -0.4f) offsetX = -1;
        if (direction.y > 0.4f) offsetY = 1;
        else if (direction.y < -0.4f) offsetY = -1;

        int targetX = playerTileX + offsetX;
        int targetY = playerTileY + offsetY;
        Vector2Int targetTile = new Vector2Int(targetX, targetY);

        Vector3 targetTileCenter = new Vector3(targetX, targetY, 0);
        float distanceToTarget = Vector3.Distance(playerPos, targetTileCenter);

        if (distanceToTarget > checkRadius)
        {
            return Vector3.zero;
        }

        if (targetTile == lastHarvestTile)
        {
            return Vector3.zero;
        }

        lastHarvestTile = targetTile;
        return new Vector3(targetX, targetY, 0);
    }
}
