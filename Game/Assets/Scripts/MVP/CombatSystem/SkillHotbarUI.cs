using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the skill hotbar UI.
/// Initializes hotbar slots and handles combat mode visibility.
/// Does NOT handle individual slot logic - that's SkillHotbarSlot's job.
/// 
/// Attach to a manager object in CombatSystem (NOT the canvas).
/// </summary>
public class SkillHotbarUI : MonoBehaviour
{
    #region Serialized Fields

    [Header("Canvas Reference")]
    [SerializeField] private GameObject skillHotbarCanvas;

    [Header("Slot References")]
    [SerializeField] private SkillHotbarSlot[] slotReferences = new SkillHotbarSlot[4];

    [Header("Default Activation Keys")]
    [SerializeField] private KeyCode[] defaultKeys = new KeyCode[]
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4
    };

    #endregion

    #region Private Fields

    private List<SkillHotbarSlot> slots = new List<SkillHotbarSlot>();

    #endregion

    #region Initialization

    private void Start()
    {
        InitializeSlots();
        SubscribeToCombatMode();
        
        // Start hidden
        if (skillHotbarCanvas != null)
            skillHotbarCanvas.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromCombatMode();
    }

    #endregion

    #region Setup

    private void InitializeSlots()
    {
        slots.Clear();

        // Use provided references or find them
        if (slotReferences != null && slotReferences.Length > 0)
        {
            for (int i = 0; i < slotReferences.Length; i++)
            {
                if (slotReferences[i] != null)
                {
                    slots.Add(slotReferences[i]);
                    SetupSlot(i, slotReferences[i]);
                }
            }
        }
        else
        {
            // Auto-find slots if not assigned
            Debug.LogWarning("[SkillHotbarUI] Slot references not assigned! Auto-finding...");
            SkillHotbarSlot[] foundSlots = GetComponentsInChildren<SkillHotbarSlot>();
            foreach (var slot in foundSlots)
            {
                slots.Add(slot);
            }
        }

        Debug.Log($"[SkillHotbarUI] Initialized {slots.Count} skill hotbar slots");
    }

    private void SetupSlot(int index, SkillHotbarSlot slot)
    {
        if (index < defaultKeys.Length)
        {
            slot.SetActivationKey(defaultKeys[index]);
        }

        Debug.Log($"[SkillHotbarUI] Slot {index} ready");
    }

    #endregion

    #region Combat Mode Events

    private void SubscribeToCombatMode()
    {
        CombatModeManager.OnCombatModeChanged += OnCombatModeChanged;
    }

    private void UnsubscribeFromCombatMode()
    {
        CombatModeManager.OnCombatModeChanged -= OnCombatModeChanged;
    }

    private void OnCombatModeChanged(bool isActive)
    {
        if (skillHotbarCanvas != null)
        {
            skillHotbarCanvas.SetActive(isActive);
        }

        Debug.Log($"[SkillHotbarUI] Combat mode: {isActive}. Canvas: {skillHotbarCanvas?.activeSelf}");
    }

    #endregion

    #region Public API

    public SkillHotbarSlot GetSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
            return slots[index];
        return null;
    }

    public int GetSlotCount() => slots.Count;

    public List<SkillHotbarSlot> GetAllSlots() => new List<SkillHotbarSlot>(slots);

    #endregion
}