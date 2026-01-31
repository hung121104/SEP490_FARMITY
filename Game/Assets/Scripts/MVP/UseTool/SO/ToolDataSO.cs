using UnityEngine;

[CreateAssetMenu(fileName = "ToolDataSO", menuName = "Scriptable Objects/ToolDataSO")]
public class ToolDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string toolID;              // Unique: "tool_hoe_basic"
    public string toolName;            // Display: "Basic Hoe"
    public ToolCategory category;      // Farming, Weapon, Gathering

    [Header("Visuals")]
    public Sprite toolIcon;            // Icon for UI
    public Sprite toolSprite;          // Sprite when equipped
    public RuntimeAnimatorController toolAnimator; // Animation override

    [Header("Usage Properties")]
    [Tooltip("Stamina cost per use (0 = no cost)")]
    public float staminaCost = 0f;

    [Tooltip("Cooldown between uses in seconds (0 = no cooldown). Only for weapons.")]
    public float useCooldown = 0f;

    [Header("Effects")]
    public AudioClip useSound;
    public GameObject useEffectPrefab;  // Particle/VFX when used

    [Header("Upgrade System")]
    [Tooltip("Next tier tool (null if max tier)")]
    public ToolDataSO upgradedVersion;
}

public enum ToolCategory
{
    Farming,
    Weapon,
    Gathering,
    Fishing
}
