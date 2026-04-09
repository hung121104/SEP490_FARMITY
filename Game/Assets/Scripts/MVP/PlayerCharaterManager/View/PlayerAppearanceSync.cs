using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Syncs paper-doll appearance (hair, outfit, hat, tool configIds) across the
/// Photon network.
///
/// Live changes use per-object RPCs (targeted at this PhotonView's RpcTarget.All)
/// so the update is applied to the correct entity on every client with no
/// actor-number confusion.
///
/// Player Custom Properties are written alongside every change so late-joining
/// clients can restore the current appearance in Start().
///
/// How it works
/// ------------
///   LOCAL player  → call SetHair/SetOutfit/SetHat/SetTool
///                 → fires RPC_SyncAppearanceSlot on all clients (live visual)
///                 → also writes Custom Property for late-joiner persistence
///
///   REMOTE player → RPC arrives on this entity's PhotonView → applied locally
///
///   ON JOIN       → Start() reads owner's Custom Properties and applies them
///
/// Inspector
/// ---------
///   equipmentManager — drag the EquipmentManager into this field.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerAppearanceSync : MonoBehaviourPunCallbacks
{
    // Custom Property keys (short to save bandwidth; used only for persistence)
    private const string KEY_HAIR   = "apHair";
    private const string KEY_OUTFIT = "apOutfit";
    private const string KEY_HAT    = "apHat";
    private const string KEY_TOOL   = "apTool";
    private const string KEY_WEAPON = "apWeapon";

    [SerializeField] private EquipmentManager equipmentManager;

    private void Awake()
    {
        if (equipmentManager == null)
            equipmentManager = GetComponent<EquipmentManager>();
    }

    private void Start()
    {
        // Apply whatever is already in the owner's custom properties.
        // Covers: remote players that were already in the room when we joined,
        // and our own re-join to an existing world.
        if (photonView.Owner != null)
            ApplyFromProperties(photonView.Owner.CustomProperties);
    }

    // ── Public API (call these on the LOCAL player only) ─────────────────────

    public void SetHair(string configId)   => BroadcastSlot(KEY_HAIR,   configId);
    public void SetOutfit(string configId) => BroadcastSlot(KEY_OUTFIT, configId);
    public void SetHat(string configId)    => BroadcastSlot(KEY_HAT,    configId);
    public void SetTool(string configId)   => BroadcastSlot(KEY_TOOL,   configId);
    public void SetWeapon(string itemId)   => BroadcastSlot(KEY_WEAPON, itemId);

    /// <summary>
    /// Bulk-set all 4 appearance slots at once (1 Custom Property message +
    /// 1 RPC instead of 4 separate ones).
    /// </summary>
    public void SetAll(string hair, string outfit, string hat, string tool)
    {
        if (!photonView.IsMine) return;

        // Persist full state in Custom Properties for late joiners
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { KEY_HAIR,   hair   ?? string.Empty },
            { KEY_OUTFIT, outfit ?? string.Empty },
            { KEY_HAT,    hat    ?? string.Empty },
            { KEY_TOOL,   tool   ?? string.Empty },
        });

        // Live sync via RPC — applies to THIS object on every client
        photonView.RPC(nameof(RPC_SyncAllAppearance), RpcTarget.AllBuffered,
            hair   ?? string.Empty,
            outfit ?? string.Empty,
            hat    ?? string.Empty,
            tool   ?? string.Empty);
    }

    /// <summary>
    /// Returns the current appearance configIds from Photon custom properties.
    /// Works for both local and remote players.
    /// </summary>
    public (string hair, string outfit, string hat, string tool) GetCurrentAppearance()
    {
        var props = photonView.Owner?.CustomProperties;
        if (props == null)
            return (string.Empty, string.Empty, string.Empty, string.Empty);

        return (
            props.TryGetValue(KEY_HAIR,   out object h) ? h as string ?? string.Empty : string.Empty,
            props.TryGetValue(KEY_OUTFIT, out object o) ? o as string ?? string.Empty : string.Empty,
            props.TryGetValue(KEY_HAT,    out object a) ? a as string ?? string.Empty : string.Empty,
            props.TryGetValue(KEY_TOOL,   out object t) ? t as string ?? string.Empty : string.Empty
        );
    }

    public string GetCurrentWeaponItemId()
    {
        var props = photonView.Owner?.CustomProperties;
        if (props == null)
            return string.Empty;

        return props.TryGetValue(KEY_WEAPON, out object w)
            ? w as string ?? string.Empty
            : string.Empty;
    }

    // ── RPCs ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called via RPC on every client to apply a single appearance slot.
    /// Because this RPC is sent on THIS entity's PhotonView, it is guaranteed
    /// to reach the correct player entity with no actor-number ambiguity.
    /// </summary>
    [PunRPC]
    private void RPC_SyncAppearanceSlot(string key, string configId)
    {
        ApplySingleSlot(key, configId ?? string.Empty);
    }

    /// <summary>Bulk RPC — restores all 4 slots at once (used by SetAll and master restore).</summary>
    [PunRPC]
    private void RPC_SyncAllAppearance(string hair, string outfit, string hat, string tool)
    {
        if (equipmentManager == null) return;
        equipmentManager.EquipHair(hair     ?? string.Empty);
        equipmentManager.EquipOutfit(outfit ?? string.Empty);
        equipmentManager.EquipHat(hat       ?? string.Empty);
        equipmentManager.EquipTool(tool     ?? string.Empty);
    }

    /// <summary>Called by master to restore saved appearance on the owning client.
    /// Also broadcasts to all so every client sees the correct visual.</summary>
    [PunRPC]
    private void RPC_RestoreAppearance(string hair, string outfit, string hat, string tool)
    {
        if (!photonView.IsMine) return;
        SetAll(hair, outfit, hat, tool);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a single-slot appearance change to all clients via RPC and
    /// persists the new value in the local player's Custom Properties.
    /// Only runs on the owning (local) client.
    /// </summary>
    private void BroadcastSlot(string key, string configId)
    {
        if (!photonView.IsMine) return;

        string value = configId ?? string.Empty;

        // Persist for late joiners
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { key, value } });

        // Live sync: targeted at this specific PhotonView so only this entity's
        // EquipmentManager is updated on every client
        photonView.RPC(nameof(RPC_SyncAppearanceSlot), RpcTarget.AllBuffered, key, value);
    }

    private void ApplySingleSlot(string key, string configId)
    {
        if (equipmentManager == null) return;

        switch (key)
        {
            case KEY_HAIR:   equipmentManager.EquipHair(configId);   break;
            case KEY_OUTFIT: equipmentManager.EquipOutfit(configId); break;
            case KEY_HAT:    equipmentManager.EquipHat(configId);    break;
            case KEY_TOOL:   equipmentManager.EquipTool(configId);   break;
            case KEY_WEAPON:
                // weapon is handled by WeaponEquipPresenter; no EquipmentManager slot
                break;
        }
    }

    private void ApplyFromProperties(Hashtable props)
    {
        if (equipmentManager == null || props == null) return;

        if (props.TryGetValue(KEY_HAIR,   out object hair))
            equipmentManager.EquipHair(hair as string ?? string.Empty);

        if (props.TryGetValue(KEY_OUTFIT, out object outfit))
            equipmentManager.EquipOutfit(outfit as string ?? string.Empty);

        if (props.TryGetValue(KEY_HAT,    out object hat))
            equipmentManager.EquipHat(hat as string ?? string.Empty);

        if (props.TryGetValue(KEY_TOOL,   out object tool))
            equipmentManager.EquipTool(tool as string ?? string.Empty);
    }
}
