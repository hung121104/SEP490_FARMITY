using UnityEngine;
using Photon.Pun;

/// <summary>
/// Forwards resource hit requests to the host-authoritative RPC path via
/// ResourceInteractionManager. All validation and state mutation happen on the
/// MasterClient inside RPC_Host_ProcessHit — this service only resolves the
/// target tile and dispatches the request.
/// </summary>
public class ResourceHarvestingService : IResourceHarvestingService
{
    private readonly ResourceInteractionManager _interactionManager;
    private readonly float interactionRange;
    private Transform localPlayerTransform;

    public ResourceHarvestingService(
        ResourceInteractionManager interactionManager,
        float interactionRange)
    {
        _interactionManager = interactionManager;
        this.interactionRange = Mathf.Max(0.1f, interactionRange);

        // Bind to delayed impact events so gameplay timing matches chop animation timing.
        UseToolService.OnAxeImpactRequested += HandleAxeRequested;
        UseToolService.OnPickaxeImpactRequested += HandlePickaxeRequested;
    }

    ~ResourceHarvestingService()
    {
        UseToolService.OnAxeImpactRequested -= HandleAxeRequested;
        UseToolService.OnPickaxeImpactRequested -= HandlePickaxeRequested;
    }

    private void HandleAxeRequested(ToolData tool, Vector3 pos)
    {
        TryHitResource(tool, pos);
    }

    private void HandlePickaxeRequested(ToolData tool, Vector3 pos)
    {
        TryHitResource(tool, pos);
    }

    private bool TryHitResource(ToolData tool, Vector3 worldPos)
    {
        if (_interactionManager == null) return false;

        if (!TryGetSnappedTargetTile(worldPos, out Vector3 snappedPos))
            return false;

        int chunkSize = WorldDataManager.Instance != null ? WorldDataManager.Instance.chunkSizeTiles : 30;
        int wx = Mathf.FloorToInt(snappedPos.x);
        int wy = Mathf.FloorToInt(snappedPos.y);
        int chunkX = Mathf.FloorToInt(wx / (float)chunkSize);
        int chunkY = Mathf.FloorToInt(wy / (float)chunkSize);
        int localX = wx - chunkX * chunkSize;
        int localY = wy - chunkY * chunkSize;
        int tileIndex = localY * chunkSize + localX;

        // Delegate all validation and state mutation to the host via RPC.
        _interactionManager.RequestHitResource(chunkX, chunkY, tileIndex, 1, tool.itemID);
        return true;
    }

    private bool TryGetSnappedTargetTile(Vector3 mouseWorldPos, out Vector3 snappedTile)
    {
        snappedTile = Vector3.zero;

        if (!TryGetLocalPlayerTransform(out Transform playerTransform))
            return false;

        Vector2Int dummy = new Vector2Int(int.MinValue, int.MinValue);
        snappedTile = CropTileSelector.GetDirectionalTile(
            playerTransform.position,
            mouseWorldPos,
            interactionRange,
            ref dummy);

        return snappedTile != Vector3.zero;
    }

    private bool TryGetLocalPlayerTransform(out Transform playerTransform)
    {
        playerTransform = null;

        if (localPlayerTransform != null && localPlayerTransform.gameObject.activeInHierarchy)
        {
            playerTransform = localPlayerTransform;
            return true;
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("PlayerEntity");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv == null || !pv.IsMine) continue;

            Transform center = player.transform.Find("CenterPoint");
            localPlayerTransform = center != null ? center : player.transform;
            playerTransform = localPlayerTransform;
            return true;
        }

        return false;
    }

}

