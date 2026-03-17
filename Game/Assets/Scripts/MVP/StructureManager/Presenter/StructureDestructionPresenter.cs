using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class StructureDestructionPresenter
{
    private readonly IStructureDestructionService destructionService;
    private readonly IStructureService structureService;
    private readonly DroppedItemManagerView droppedItemManager;
    private readonly StructureDestructionView view;
    private readonly IInventoryService inventoryService;
    private readonly bool showDebugLogs;
    private readonly ChunkDataSyncManager syncManager;

    public StructureDestructionPresenter(
        StructureDestructionView view,
        IStructureDestructionService destructionService,
        IStructureService structureService,
        IInventoryService inventoryService,
        ChunkDataSyncManager syncManager,
        bool showDebugLogs = true)
    {
        this.view = view;
        this.destructionService = destructionService;
        this.structureService = structureService;
        this.inventoryService = inventoryService;
        this.syncManager = syncManager;
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
        
        // Cast to concrete class to access ProcessHitRequest
        if (destructionService is StructureDestructionService concreteService)
        {
            concreteService.ProcessHitRequest(tilePos, damage, playerActorId);
        }
        else if (showDebugLogs)
        {
            Debug.LogError($"[StructureDestructionPresenter] Cannot process hit request - service is not StructureDestructionService");
        }
    }

    private void OnStructureHpUpdated(int worldX, int worldY, int newHp)
    {
        Vector3Int tilePos = new Vector3Int(worldX, worldY, 0);

        // newHp = -1 means hit effect only (visual feedback for other player's hit)
        // newHp >= 0 means actual HP update
        
        if (newHp == -1)
        {
            // Hit effect only - another player hit the structure
            view.PlayHitEffect(tilePos);
            view.StartRegenTimer(tilePos);
            return;
        }

        // Play hit effect on all clients when HP updates
        view.PlayHitEffect(tilePos);

        // Reset regen timer
        view.StartRegenTimer(tilePos);
    }

    public void HandleToolUse(Vector3 targetWorldPos, ToolData tool)
    {
        Vector3Int tilePos = new Vector3Int(Mathf.FloorToInt(targetWorldPos.x), Mathf.FloorToInt(targetWorldPos.y), 0);

        // Check if already destroyed (HP = 0)
        UnifiedChunkData chunk = WorldDataManager.Instance.GetChunkAtWorldPosition(tilePos);
        if (chunk != null)
        {
            int currentHp = chunk.GetStructureHp(tilePos.x, tilePos.y);
            if (currentHp <= 0 && chunk.HasStructure(tilePos.x, tilePos.y))
            {
                // Structure already marked for destruction, don't process further hits
                if (showDebugLogs)
                    Debug.Log($"[StructureDestructionPresenter] Structure at {tilePos} already destroyed, ignoring hit");
                return;
            }
        }

        bool success = destructionService.DealDamage(tilePos, tool.toolPower, out bool isRemoved, out string structureId);
    }

    public void HandleRegenTimerComplete(Vector3Int tilePos)
    {
        destructionService.RegenerateHP(tilePos);
    }
}
