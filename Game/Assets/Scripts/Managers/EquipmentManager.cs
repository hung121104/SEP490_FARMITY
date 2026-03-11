using UnityEngine;

/// <summary>
/// Attached to the Player root GameObject.
/// Owns references to all DynamicSpriteSwapper slots and provides a clean
/// API for equipping / unequipping items at runtime.
///
/// Inspector Setup
/// ---------------
/// Drag the DynamicSpriteSwapper component from each child layer GameObject
/// into the matching slot below.  The configIds should be the starting
/// equipment for a new character (e.g. "copper_hoe", "farmer_base_hair").
///
/// Usage Example (called from an inventory script or Photon RPC)
/// -------------
///   equip.EquipTool("gold_hoe");
///   equip.EquipHair("redhead_hair");
///   equip.EquipOutfit("winter_coat");
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    // ── Inspector Slots ───────────────────────────────────────────────────────

    [Header("Paper Doll Swapper References")]
    [Tooltip("The swapper on the Tool layer child GameObject.")]
    [SerializeField] private DynamicSpriteSwapper toolSwapper;

    [Tooltip("The swapper on the Hair layer child GameObject.")]
    [SerializeField] private DynamicSpriteSwapper hairSwapper;

    [Tooltip("The swapper on the Hat layer child GameObject.")]
    [SerializeField] private DynamicSpriteSwapper hatSwapper;

    [Tooltip("The swapper on the Outfit layer child GameObject.")]
    [SerializeField] private DynamicSpriteSwapper outfitSwapper;

    // ── Public Equipment API ──────────────────────────────────────────────────

    /// <summary>
    /// Equips the tool spritesheet identified by <paramref name="configId"/>
    /// (e.g. "copper_hoe", "gold_hoe", "watering_can_iron").
    /// Pass null or an empty string to hide the tool layer.
    /// </summary>
    public void EquipTool(string configId)
    {
        SetSwapper(toolSwapper, configId, nameof(toolSwapper));
    }

    /// <summary>
    /// Equips the hair spritesheet (e.g. "blonde_hair", "redhead_hair").
    /// </summary>
    public void EquipHair(string configId)
    {
        SetSwapper(hairSwapper, configId, nameof(hairSwapper));
    }

    /// <summary>
    /// Equips the hat spritesheet (e.g. "straw_hat", "winter_hat").
    /// </summary>
    public void EquipHat(string configId)
    {
        SetSwapper(hatSwapper, configId, nameof(hatSwapper));
    }

    /// <summary>
    /// Equips the outfit spritesheet (e.g. "farmer_default", "fisher_outfit").
    /// </summary>
    public void EquipOutfit(string configId)
    {
        SetSwapper(outfitSwapper, configId, nameof(outfitSwapper));
    }

    // ── Generic Helper ────────────────────────────────────────────────────────

    /// <summary>
    /// Generic slot assignment.  Logs a warning if the swapper reference is
    /// missing in the Inspector, preventing silent failures during development.
    /// </summary>
    private void SetSwapper(
        DynamicSpriteSwapper swapper,
        string configId,
        string slotName)
    {
        if (swapper == null)
        {
            Debug.LogWarning(
                $"[EquipmentManager] '{slotName}' is not assigned on " +
                $"{gameObject.name}. Drag the DynamicSpriteSwapper component " +
                "into the Inspector field.");
            return;
        }

        swapper.ConfigId = configId ?? string.Empty;
    }

#if UNITY_EDITOR
    // Quick-test utilities visible in the Inspector during Play mode
    [Header("Editor Quick-Test (Play Mode Only)")]
    [SerializeField] private string debugToolConfigId   = "copper_hoe";
    [SerializeField] private string debugHairConfigId   = "blonde_hair";
    [SerializeField] private string debugHatConfigId    = "";
    [SerializeField] private string debugOutfitConfigId = "farmer_default";

    [ContextMenu("Apply Debug Equipment")]
    private void ApplyDebugEquipment()
    {
        EquipTool(debugToolConfigId);
        EquipHair(debugHairConfigId);
        EquipHat(debugHatConfigId);
        EquipOutfit(debugOutfitConfigId);
        Debug.Log("[EquipmentManager] Debug equipment applied.");
    }
#endif
}
