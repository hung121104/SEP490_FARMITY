using UnityEngine;
using Photon.Pun;

public class StructureDestructionPresenter
{
    private readonly IStructureDestructionService destructionService;
    private readonly StructureDestructionView view;
    private readonly bool showDebugLogs;

    public StructureDestructionPresenter(
        StructureDestructionView view,
        IStructureDestructionService destructionService,
        bool showDebugLogs = true)
    {
        this.view = view;
        this.destructionService = destructionService;
        this.showDebugLogs = showDebugLogs;

        // Subscribe to HP update events for visual feedback
        ChunkDataSyncManager.OnStructureHpUpdated += OnStructureHpUpdated;
        
        // Master: Subscribe to hit requests from clients
        if (PhotonNetwork.IsMasterClient)
        {
            ChunkDataSyncManager.OnStructureHitRequest += OnStructureHitRequest;
        }
    }

    ~StructureDestructionPresenter()
    {
        ChunkDataSyncManager.OnStructureHpUpdated -= OnStructureHpUpdated;
        
        if (PhotonNetwork.IsMasterClient)
        {
            ChunkDataSyncManager.OnStructureHitRequest -= OnStructureHitRequest;
        }
    }

    /// <summary>
    /// Master only: Handle hit request from client.
    /// </summary>
    private void OnStructureHitRequest(int worldX, int worldY, int damage, string playerActorId)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        Vector3Int tilePos = new Vector3Int(worldX, worldY, 0);
        
        // Call service
        destructionService.ProcessHitRequest(tilePos, damage, playerActorId);
    }

    private void OnStructureHpUpdated(int worldX, int worldY, int newHp)
    {
        Vector3Int tilePos = new Vector3Int(worldX, worldY, 0);

        // newHp = -1: hit effect only (visual feedback for other player's hit)
        // newHp > 0 but less than max: took damage → hit effect + regen timer
        // newHp = maxHp: regen completed → no effect needed
        // newHp <= 0: destroyed → no effect (removal handles visuals)

        if (newHp == -1)
        {
            view.PlayHitEffect(tilePos);
            view.StartRegenTimer(tilePos);
            return;
        }

        // Regen or full HP — no hit effect, no regen timer
        if (IsFullHp(worldX, worldY, newHp))
            return;

        // Destroyed — removal handles visuals
        if (newHp <= 0)
            return;

        // Took damage — play hit effect and reset regen timer
        view.PlayHitEffect(tilePos);
        view.StartRegenTimer(tilePos);
    }

    private bool IsFullHp(int worldX, int worldY, int currentHp)
    {
        UnifiedChunkData chunk = WorldDataManager.Instance?.GetChunkAtWorldPosition(new Vector3(worldX, worldY, 0));
        if (chunk == null || !chunk.TryGetStructure(worldX, worldY, out var structureData))
            return false;

        StructureData so = Object.FindAnyObjectByType<StructurePool>()?.GetStructureData(structureData.StructureId);
        int maxHp = so?.MaxHealth ?? 3;
        return currentHp >= maxHp;
    }

    public void HandleToolUse(Vector3 targetWorldPos, ToolData tool)
    {
        Vector3Int tilePos = new Vector3Int(Mathf.FloorToInt(targetWorldPos.x), Mathf.FloorToInt(targetWorldPos.y), 0);

        // Delegate business logic check to Service — Presenter không truy cập WorldDataManager
        if (destructionService.IsStructureAlreadyDestroyed(tilePos))
        {
            if (showDebugLogs)
                Debug.Log($"[StructureDestructionPresenter] Structure at {tilePos} already destroyed, ignoring hit");
            return;
        }

        bool success = destructionService.DealDamage(tilePos, tool.toolPower, out bool isRemoved, out string structureId);
    }

    public void HandleRegenTimerComplete(Vector3Int tilePos)
    {
        destructionService.RegenerateHP(tilePos);
    }
}
