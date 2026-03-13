using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Syncs paper-doll appearance (hair, outfit, hat, tool configIds) across the
/// Photon network using Player Custom Properties.
///
/// Attach to the Player prefab alongside EquipmentManager.
///
/// How it works
/// ------------
///   LOCAL player  → call SetHair/SetOutfit/SetHat/SetTool
///                 → writes Custom Properties on PhotonNetwork.LocalPlayer
///                 → Photon broadcasts to everyone automatically
///
///   REMOTE player → OnPlayerPropertiesUpdate fires
///                 → reads the 4 configId keys → applies via EquipmentManager
///
///   ON JOIN       → Custom Properties are delivered automatically to late joiners
///                 → Start() applies whatever is already in Owner.CustomProperties
///
/// Inspector
/// ---------
///   equipmentManager — drag the EquipmentManager from this same GameObject.
///                      Auto-found in Awake if left empty.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerAppearanceSync : MonoBehaviourPunCallbacks
{
    // Custom Property keys (short to save bandwidth)
    private const string KEY_HAIR   = "apHair";
    private const string KEY_OUTFIT = "apOutfit";
    private const string KEY_HAT    = "apHat";
    private const string KEY_TOOL   = "apTool";

    [SerializeField] private EquipmentManager equipmentManager;

    private void Awake()
    {
        if (equipmentManager == null)
            equipmentManager = GetComponent<EquipmentManager>();
    }

    private void Start()
    {
        // Apply whatever is already in the owner's custom properties.
        // Covers: remote players that spawned before us, and our own re-join.
        if (photonView.Owner != null)
            ApplyFromProperties(photonView.Owner.CustomProperties);
    }

    // ── Public API (call these on the LOCAL player) ──────────────────────────

    public void SetHair(string configId)   => SetProperty(KEY_HAIR, configId);
    public void SetOutfit(string configId) => SetProperty(KEY_OUTFIT, configId);
    public void SetHat(string configId)    => SetProperty(KEY_HAT, configId);
    public void SetTool(string configId)   => SetProperty(KEY_TOOL, configId);

    /// <summary>
    /// Bulk-set all 4 appearance slots at once. Sends a single Custom Properties
    /// update (1 network message) instead of 4 separate ones.
    /// </summary>
    public void SetAll(string hair, string outfit, string hat, string tool)
    {
        if (!photonView.IsMine) return;

        var props = new Hashtable
        {
            { KEY_HAIR,   hair   ?? string.Empty },
            { KEY_OUTFIT, outfit ?? string.Empty },
            { KEY_HAT,    hat    ?? string.Empty },
            { KEY_TOOL,   tool   ?? string.Empty },
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // Apply locally immediately (no round-trip lag for own player)
        ApplyFromProperties(props);
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

    // ── Photon Callback ─────────────────────────────────────────────────────

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Only react if the update is for THIS player entity's owner
        if (photonView.Owner == null || targetPlayer.ActorNumber != photonView.Owner.ActorNumber)
            return;

        ApplyFromProperties(changedProps);
    }

    // ── RPC (called by master to tell the owning client to restore saved appearance) ──

    [PunRPC]
    private void RPC_RestoreAppearance(string hair, string outfit, string hat, string tool)
    {
        if (!photonView.IsMine) return;
        SetAll(hair, outfit, hat, tool);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void SetProperty(string key, string configId)
    {
        if (!photonView.IsMine) return;

        var props = new Hashtable { { key, configId ?? string.Empty } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // Apply locally immediately
        ApplyFromProperties(props);
    }

    private void ApplyFromProperties(Hashtable props)
    {
        if (equipmentManager == null) return;

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
