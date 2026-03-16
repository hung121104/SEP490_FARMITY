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

    private StructureDestructionPresenter presenter;
    private ChunkLoadingManager chunkLoadingManager;
    private Transform playerTransform;

    // Track active regen coroutines so we can reset them if hit again
    private Dictionary<Vector3Int, Coroutine> activeRegenTimers = new Dictionary<Vector3Int, Coroutine>();

    private void Start()
    {
        chunkLoadingManager = FindAnyObjectByType<ChunkLoadingManager>();
        
        // Initialize MVP
        StructurePool pool = FindAnyObjectByType<StructurePool>();
        ChunkDataSyncManager syncManager = FindAnyObjectByType<ChunkDataSyncManager>();
        IStructureDestructionService destService = new StructureDestructionService(pool, syncManager, showDebugLogs);
        
        IStructureService structService = new StructureService(syncManager, chunkLoadingManager, showDebugLogs);
        
        InventoryGameView invGameView = FindAnyObjectByType<InventoryGameView>();
        IInventoryService invService = invGameView != null ? invGameView.GetInventoryService() : null;
        
        presenter = new StructureDestructionPresenter(this, destService, structService, invService, syncManager, showDebugLogs);

        // Subscribe to tool events
        UseToolService.OnAxeRequested += HandleToolUse;
        UseToolService.OnPickaxeRequested += HandleToolUse;
        UseToolService.OnHoeRequested += HandleToolUse;
        UseToolService.OnWateringCanRequested += HandleToolUse;
        UseToolService.OnFishingRodRequested += HandleToolUse;
    }

    private void OnDestroy()
    {
        UseToolService.OnAxeRequested -= HandleToolUse;
        UseToolService.OnPickaxeRequested -= HandleToolUse;
        UseToolService.OnHoeRequested -= HandleToolUse;
        UseToolService.OnWateringCanRequested -= HandleToolUse;
        UseToolService.OnFishingRodRequested -= HandleToolUse;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("PlayerEntity");
            if (player != null)
            {
                Transform center = player.transform.Find("CenterPoint");
                playerTransform = center != null ? center : player.transform;
            }
        }
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
