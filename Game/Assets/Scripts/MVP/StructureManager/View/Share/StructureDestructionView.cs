using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StructureDestructionView : MonoBehaviour
{
    [Header("Settings")]
    public List<ToolType> validTools = new List<ToolType> { ToolType.Axe, ToolType.Pickaxe };
    public float regenDelaySeconds = 10f;
    public float interactionRange = 2f;
    public bool showDebugLogs = true;

    [Header("Visual Feedback")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;

    private StructurePresenter presenter;
    private ChunkLoadingManager chunkLoadingManager;
    private Transform playerTransform;

    // Track active regen coroutines so we can reset them if hit again
    private Dictionary<Vector3Int, Coroutine> activeRegenTimers = new Dictionary<Vector3Int, Coroutine>();

    private void Start()
    {
        chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        // Initialize MVP — single unified service & presenter
        StructurePool pool = FindAnyObjectByType<StructurePool>();
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        ChunkLoadingManager loadingManager = FindAnyObjectByType<ChunkLoadingManager>();

        IStructureService structureService = new StructureService(syncManager, loadingManager, pool, showDebugLogs);
        presenter = new StructurePresenter(structureService, this, showDebugLogs);

        // Wire static delegate so Service can add items to inventory
        // without depending on InventoryGameView (View class) directly
        StructureService.OnAddItemToInventory = (id, qty, quality) =>
        {
            var invView = FindAnyObjectByType<InventoryGameView>();
            return invView != null && invView.AddItem(id, qty, quality);
        };

        // Subscribe to tool events
        UseToolService.OnAxeImpactRequested += HandleToolUse;
        UseToolService.OnPickaxeImpactRequested += HandleToolUse;
        UseToolService.OnHoeRequested += HandleToolUse;
        UseToolService.OnWateringCanRequested += HandleToolUse;
        UseToolService.OnFishingRodRequested += HandleToolUse;
    }

    private void OnDestroy()
    {
        // Stop all active regen coroutines to prevent MissingReferenceException
        StopAllCoroutines();
        activeRegenTimers.Clear();

        // Unwire static delegate
        StructureService.OnAddItemToInventory = null;

        UseToolService.OnAxeImpactRequested -= HandleToolUse;
        UseToolService.OnPickaxeImpactRequested -= HandleToolUse;
        UseToolService.OnHoeRequested -= HandleToolUse;
        UseToolService.OnWateringCanRequested -= HandleToolUse;
        UseToolService.OnFishingRodRequested -= HandleToolUse;
    }

    private void Update()
    {
        if (playerTransform == null)
            TryResolvePlayerTransform(out playerTransform);
    }

    private bool TryResolvePlayerTransform(out Transform resolvedTransform)
    {
        resolvedTransform = null;

        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
        if (players == null || players.Length == 0)
            return false;

        foreach (GameObject player in players)
        {
            Photon.Pun.PhotonView pv = player.GetComponent<Photon.Pun.PhotonView>();
            if (Photon.Pun.PhotonNetwork.IsConnected && (pv == null || !pv.IsMine))
                continue;

            Transform center = player.transform.Find("CenterPoint");
            resolvedTransform = center != null ? center : player.transform;
            return true;
        }

        if (!Photon.Pun.PhotonNetwork.IsConnected)
        {
            Transform center = players[0].transform.Find("CenterPoint");
            resolvedTransform = center != null ? center : players[0].transform;
            return true;
        }

        return false;
    }

    private void HandleToolUse(ToolData tool, Vector3 mouseWorldPos)
    {
        if (!validTools.Contains(tool.toolType)) return;
        if (playerTransform == null) return;

        Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
        Vector3 snappedPos = CropTileSelector.GetDirectionalTile(
            playerTransform.position, mouseWorldPos, interactionRange, ref dummy);

        if (snappedPos != Vector3.zero)
        {
            presenter.HandleToolUse(snappedPos, tool);
        }
    }

    /// <summary>
    /// Starts or resets the 10-second regeneration timer for a structure.
    /// </summary>
    public void StartRegenTimer(Vector3Int tilePos)
    {
        if (this == null || !gameObject) return; // Guard: object already destroyed

        if (activeRegenTimers.TryGetValue(tilePos, out Coroutine existing))
        {
            if (existing != null) StopCoroutine(existing);
            activeRegenTimers.Remove(tilePos);
        }

        Coroutine newTimer = StartCoroutine(RegenRoutine(tilePos));
        activeRegenTimers[tilePos] = newTimer;
    }

    private IEnumerator RegenRoutine(Vector3Int tilePos)
    {
        yield return new WaitForSeconds(regenDelaySeconds);
        if (this == null || presenter == null) yield break; // Guard: destroyed during wait
        activeRegenTimers.Remove(tilePos);
        presenter.HandleRegenTimerComplete(tilePos);
    }

    /// <summary>
    /// Plays the flash and shake effect on the Structure GameObject.
    /// </summary>
    public void PlayHitEffect(Vector3Int tilePos)
    {
        if (chunkLoadingManager == null) return;

        GameObject visualGo = chunkLoadingManager.GetStructureVisualAt(tilePos);
        if (visualGo == null) return;

        SpriteRenderer sr = visualGo.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            StartCoroutine(FlashRoutine(sr));
            StartCoroutine(ShakeRoutine(visualGo.transform));
        }
    }

    private IEnumerator FlashRoutine(SpriteRenderer sr)
    {
        Color original = Color.white;
        sr.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        if (sr != null) sr.color = original;
    }

    private IEnumerator ShakeRoutine(Transform target)
    {
        Vector3 originalPos = target.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            if (target == null) yield break;

            float offsetX = Random.Range(-1f, 1f) * shakeIntensity;
            float offsetY = Random.Range(-1f, 1f) * shakeIntensity;
            target.position = originalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null) target.position = originalPos;
    }
}
