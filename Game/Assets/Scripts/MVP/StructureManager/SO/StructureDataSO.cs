using UnityEngine;

/// <summary>
/// Unity-side static definition for a single structure type.
/// Holds all data that CANNOT live in the JSON item catalog (Prefabs, Sprites, size).
/// Create one asset per structure variant via:
///   Create > Scriptable Objects > StructureDataSO
/// </summary>
[CreateAssetMenu(fileName = "StructureDataSO", menuName = "Scriptable Objects/StructureDataSO")]
public class StructureDataSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Must match the itemID field of the corresponding item in the item catalog JSON")]
    public string StructureId;

    [Header("Prefab")]
    [Tooltip("The prefab instantiated for this structure (managed by StructurePool)")]
    public GameObject Prefab;

    [Header("Size (in grid tiles)")]
    [Tooltip("Width of this structure in tiles")]
    [Min(1)] public int Width = 1;

    [Tooltip("Height of this structure in tiles")]
    [Min(1)] public int Height = 1;

    [Header("Interaction")]
    [Tooltip("What happens when the player interacts with this structure")]
    public StructureInteractionType InteractionType = StructureInteractionType.None;

    [Header("Durability")]
    [Tooltip("Hit points before the structure is destroyed")]
    [Min(1)] public int MaxHealth = 3;

    [Header("Storage (only if InteractionType == Storage)")]
    [Tooltip("Number of inventory slots for a single chest")]
    public int StorageSlots = 0;

    [Header("Double Chest Sprites")]
    [Tooltip("Sprite used for the left-half when two chests of the same type merge")]
    public Sprite DoubleChestLeftSprite;

    [Tooltip("Sprite used for the right-half when two chests of the same type merge")]
    public Sprite DoubleChestRightSprite;
}
