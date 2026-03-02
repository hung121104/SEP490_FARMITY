using UnityEngine;

/// <summary>
/// Manages Combat Mode toggle.
/// When ON: Only combat inputs work (skills, normal attack)
/// When OFF: Other systems handle inputs (hotbar, items, etc.)
/// </summary>
public class CombatModeManager : MonoBehaviour
{
    public static CombatModeManager Instance { get; private set; }

    [Header("Combat Mode Settings")]
    [SerializeField] private KeyCode combatModeToggleKey = KeyCode.LeftAlt;

    private bool isCombatModeActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(combatModeToggleKey))
        {
            ToggleCombatMode();
        }
    }

    private void ToggleCombatMode()
    {
        isCombatModeActive = !isCombatModeActive;
        Debug.Log($"Combat Mode: {(isCombatModeActive ? "ON" : "OFF")}");
        
        // Your UI team can listen to this event
        OnCombatModeChanged?.Invoke(isCombatModeActive);
    }

    public bool IsCombatModeActive => isCombatModeActive;

    public delegate void CombatModeDelegate(bool isActive);
    public static event CombatModeDelegate OnCombatModeChanged;
}