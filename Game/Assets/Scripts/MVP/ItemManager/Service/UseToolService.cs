using UnityEngine;
using Photon.Pun;

public class UseToolService : IUseToolService
{
    // ── Plowing integration ───────────────────────────────────────────────
    private readonly CropPlowingView plowingView;   // resolved lazily via GetPresenter()
    private readonly string playerTag;
    private readonly float plowingRange;
    /// Deduplication across rapid calls — mirrors CropPlowingView.lastPlowedTile
    private Vector2Int lastPlowTile = new Vector2Int(int.MinValue, int.MinValue);

    /// <summary>
    /// Default constructor — no plowing integration (keep existing behaviour).
    /// </summary>
    public UseToolService() { }

    /// <summary>
    /// Constructor used when the Hoe should delegate to the Plowing MVP.
    /// Pass the view; the presenter is resolved lazily so timing is not an issue.
    /// </summary>
    public UseToolService(CropPlowingView plowingView, string playerTag = "PlayerEntity", float plowingRange = 2f)
    {
        this.plowingView  = plowingView;
        this.playerTag    = playerTag;
        this.plowingRange = plowingRange;
    }

    public bool UseHoe(ToolDataSO item, Vector3 mouseWorldPos)
    {
        Debug.Log("[UseToolService] UseHoe: " + item + " at: " + mouseWorldPos);

        // Resolve presenter lazily — avoids the timing issue where the view exists
        // but its Start() hasn't run yet at subscription time.
        CropPlowingPresenter plowingPresenter = plowingView != null ? plowingView.GetPresenter() : null;

        if (plowingPresenter == null)
        {
            Debug.LogWarning("[UseToolService] UseHoe: plowing presenter not ready yet (CropPlowingView.Start may not have run).");
            return true; // log only — nothing destructive
        }

        // Find the local player (same logic as CropPlowingView)
        Transform playerTransform = FindLocalPlayer();
        if (playerTransform == null)
        {
            Debug.LogWarning("[UseToolService] UseHoe: local player not found.");
            return false;
        }

        // Apply the same 8-directional tile snapping CropPlowingView uses
        Vector3 snappedTile = CropTileSelector.GetDirectionalTile(
            playerTransform.position,
            mouseWorldPos,
            plowingRange,
            ref lastPlowTile);

        if (snappedTile == Vector3.zero)
        {
            Debug.Log("[UseToolService] UseHoe: target tile out of range or same as last tile.");
            return false;
        }

        // Delegate to the Plowing MVP presenter — identical call that CropPlowingView makes
        plowingPresenter.HandlePlowAction(snappedTile);
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Finds the Photon-local player by tag, returns its CenterPoint (or root).</summary>
    private Transform FindLocalPlayer()
    {
        if (string.IsNullOrEmpty(playerTag)) return null;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag(playerTag))
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                Transform center = player.transform.Find("CenterPoint");
                return center != null ? center : player.transform;
            }
        }
        return null;
    }

    // ── Other tools (unchanged) ───────────────────────────────────────────

    public bool UseWateringCan(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseWateringCan: " + item + " at: " + pos);
        return true;
    }

    public bool UsePickaxe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UsePickaxe: " + item + " at: " + pos);
        return true;
    }

    public bool UseAxe(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseAxe: " + item + " at: " + pos);
        return true;
    }

    public bool UseFishingRod(ToolDataSO item, Vector3 pos)
    {
        Debug.Log("[UseToolService] UseFishingRod: " + item + " at: " + pos);
        return true;
    }
}
