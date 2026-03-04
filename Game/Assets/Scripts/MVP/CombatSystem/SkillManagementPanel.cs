using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager for Skill Management Menu.
/// Gets all skills from SkillDatabase and creates UI elements dynamically.
/// Displays available skills in a grid/list format.
/// Handles drag state tracking.
/// 
/// This script attaches to a manager object in CombatSystem (NOT the Canvas).
/// It finds and populates the SkillManagementCanvas (pure visual, no logic).
/// </summary>
public class SkillManagementPanel : MonoBehaviour
{
    public static SkillManagementPanel Instance { get; private set; }

    [Header("Canvas Reference")]
    [SerializeField] private GameObject skillManagementCanvas;

    [Header("Prefabs")]
    [SerializeField] private GameObject skillDisplayItemPrefab;

    [Header("Grid Settings")]
    [SerializeField] private Transform skillGridContainer;
    [SerializeField] private int gridColumnCount = 2;

    private List<SkillDisplayItem> displayItems = new List<SkillDisplayItem>();
    private SkillDisplayItem currentlyDraggingItem = null;

    #region Initialization

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SubscribeToCombatMode();
        InitializeSkillGrid();

        // Start hidden
        if (skillManagementCanvas != null)
            skillManagementCanvas.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromCombatMode();
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
        Debug.Log($"[SkillManagementPanel] Combat mode: {isActive}");
    }

    #endregion

    #region Grid Initialization

    private void InitializeSkillGrid()
    {
        if (SkillDatabase.Instance == null)
        {
            Debug.LogError("[SkillManagementPanel] SkillDatabase not found!");
            return;
        }

        if (skillGridContainer == null)
        {
            Debug.LogError("[SkillManagementPanel] Skill grid container not assigned!");
            return;
        }

        if (skillDisplayItemPrefab == null)
        {
            Debug.LogError("[SkillManagementPanel] Skill display item prefab not assigned!");
            return;
        }

        // Clear existing items
        displayItems.Clear();
        foreach (Transform child in skillGridContainer)
        {
            Destroy(child.gameObject);
        }

        // Create display item for each skill in database
        List<SkillData> allSkills = SkillDatabase.Instance.GetAllSkills();
        
        for (int i = 0; i < allSkills.Count; i++)
        {
            CreateSkillDisplayItem(allSkills[i], i);
        }

        Debug.Log($"[SkillManagementPanel] Created {displayItems.Count} skill display items");
    }

    private void CreateSkillDisplayItem(SkillData skillData, int index)
    {
        GameObject itemGO = Instantiate(skillDisplayItemPrefab, skillGridContainer);
        itemGO.name = $"Skill_{skillData.skillName}";

        SkillDisplayItem displayItem = itemGO.GetComponent<SkillDisplayItem>();
        if (displayItem == null)
        {
            Debug.LogError($"[SkillManagementPanel] Prefab missing SkillDisplayItem component!");
            Destroy(itemGO);
            return;
        }

        displayItem.Initialize(skillData);
        displayItems.Add(displayItem);
    }

    #endregion

    #region Drag Event Handlers

    /// <summary>
    /// Called by SkillDisplayItem when drag begins.
    /// </summary>
    public void OnSkillBeginDrag(SkillDisplayItem draggedItem)
    {
        currentlyDraggingItem = draggedItem;
        Debug.Log($"[SkillManagementPanel] Skill drag started: {draggedItem.GetSkillData().skillName}");
    }

    /// <summary>
    /// Called by SkillDisplayItem when drag ends.
    /// </summary>
    public void OnSkillEndDrag(SkillDisplayItem draggedItem)
    {
        if (currentlyDraggingItem == draggedItem)
        {
            currentlyDraggingItem = null;
        }
        Debug.Log($"[SkillManagementPanel] Skill drag ended: {draggedItem.GetSkillData().skillName}");
    }

    /// <summary>
    /// Cancel all drag operations (similar to inventory system).
    /// </summary>
    public void CancelAllDragActions()
    {
        if (currentlyDraggingItem != null)
        {
            currentlyDraggingItem.ForceResetState();
            currentlyDraggingItem = null;
        }

        // Reset all items' state
        foreach (var item in displayItems)
        {
            if (item != null && item.IsDragging)
            {
                item.ForceResetState();
            }
        }

        Debug.Log("[SkillManagementPanel] All drag actions cancelled");
    }

    #endregion

    #region Public API

    public void ShowSkillPanel()
    {
        if (skillManagementCanvas != null)
            skillManagementCanvas.SetActive(true);
        
        Debug.Log("[SkillManagementPanel] Skill panel shown");
    }

    public void HideSkillPanel()
    {
        // Cancel drag before hiding
        CancelAllDragActions();

        if (skillManagementCanvas != null)
            skillManagementCanvas.SetActive(false);

        Debug.Log("[SkillManagementPanel] Skill panel hidden");
    }

    public void ToggleSkillPanel()
    {
        if (skillManagementCanvas != null)
        {
            if (skillManagementCanvas.activeSelf)
            {
                HideSkillPanel();
            }
            else
            {
                ShowSkillPanel();
            }
        }
    }

    public bool IsAnySkillDragging => currentlyDraggingItem != null;

    public SkillDisplayItem GetCurrentlyDraggingItem => currentlyDraggingItem;

    #endregion

    #region Input Handling

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleSkillPanel();
        }

        // ESC to cancel drag
        if (Input.GetKeyDown(KeyCode.Escape) && IsAnySkillDragging)
        {
            CancelAllDragActions();
        }

        // Debug: Show panel state
        if (skillManagementCanvas != null && skillManagementCanvas.activeSelf)
        {
            Debug.Log("[SkillManagementPanel] Panel is ACTIVE - drag/swap enabled in hotbar");
        }
    }

    #endregion

    #region Panel State

    /// <summary>
    /// Check if the skill management panel is currently active/visible.
    /// </summary>
    public bool isSkillPanelActive
    {
        get
        {
            if (skillManagementCanvas != null)
                return skillManagementCanvas.activeSelf;
            return false;
        }
    }

    #endregion
}