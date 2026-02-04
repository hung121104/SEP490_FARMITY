using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ToolView : MonoBehaviourPun
{
    #region Serialized Fields

    [Header("Available Tools")]
    [Tooltip("Tools assigned by designer, switch with Tab key")]
    [SerializeField] private ToolDataSO[] availableTools;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer toolSpriteRenderer;
    [SerializeField] private Animator toolAnimator;

    [Header("UI")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private Text toolNameText;

    [Header("Dependencies")]
    [Tooltip("Reference to PlayerModel (assign in Inspector or find on same GameObject)")]
    [SerializeField] private PlayerModel playerModel;

    #endregion

    #region Private Fields

    // MVP components
    private ToolStateModel toolModel;
    private ToolPresenter toolPresenter;

    // Tool selection
    private int currentToolIndex = 0;

    // Input state
    private bool useToolPressed = false;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initialize MVP components and equip first tool
    /// </summary>
    private void Start()
    {
        if (!photonView.IsMine) return;

        InitializeMVP();
        EquipFirstTool();
    }

    /// <summary>
    /// Handle tool usage and cooldown UI updates
    /// </summary>
    private void Update()
    {
        if (!photonView.IsMine) return;

        ProcessToolUsage();
        UpdateCooldownUI();
    }

    /// <summary>
    /// Cleanup - unsubscribe from events
    /// </summary>
    private void OnDestroy()
    {
        if (toolModel != null)
        {
            toolModel.OnToolChanged -= OnToolChanged;
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize MVP components
    /// </summary>
    private void InitializeMVP()
    {
        // Get PlayerModel reference
        if (playerModel == null)
        {
            Debug.LogError("PlayerModel not assigned! Assign it in Inspector.");
            return;
        }

        // Initialize MVP
        toolModel = new ToolStateModel();
        ToolServiceRouter serviceRouter = new ToolServiceRouter();
        toolPresenter = new ToolPresenter(toolModel, serviceRouter, playerModel);

        // Subscribe to model events
        toolModel.OnToolChanged += OnToolChanged;
    }

    /// <summary>
    /// Equip first tool from available tools array
    /// </summary>
    private void EquipFirstTool()
    {
        if (availableTools == null || availableTools.Length == 0)
        {
            Debug.LogWarning("No tools available to equip");
            return;
        }

        currentToolIndex = 0;
        toolPresenter.EquipTool(availableTools[currentToolIndex]);
    }

    #endregion

    #region Tool Usage

    /// <summary>
    /// Process tool usage input
    /// </summary>
    private void ProcessToolUsage()
    {
        if (!useToolPressed) return;

        bool success = toolPresenter.TryUseTool(transform.position);

        if (success && toolAnimator != null)
        {
            toolAnimator.SetTrigger("Use");
        }

        useToolPressed = false;
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Update cooldown overlay fill amount for visual feedback
    /// </summary>
    private void UpdateCooldownUI()
    {
        if (cooldownOverlay == null) return;

        float cooldownProgress = toolPresenter.GetCooldownProgress();
        cooldownOverlay.fillAmount = cooldownProgress;
    }

    /// <summary>
    /// Event handler when equipped tool changes
    /// Updates sprite, animator, and UI text
    /// </summary>
    private void OnToolChanged(ToolDataSO tool)
    {
        if (tool != null)
        {
            UpdateToolVisuals(tool);
            UpdateToolUI(tool);
            LogToolEquip(tool);
        }
        else
        {
            ClearToolVisuals();
        }
    }

    /// <summary>
    /// Update tool sprite and animator
    /// </summary>
    private void UpdateToolVisuals(ToolDataSO tool)
    {
        // Update sprite
        if (toolSpriteRenderer != null)
        {
            toolSpriteRenderer.sprite = tool.toolSprite;
            toolSpriteRenderer.enabled = true;
        }

        // Update animator
        if (toolAnimator != null && tool.toolAnimator != null)
        {
            toolAnimator.runtimeAnimatorController = tool.toolAnimator;
        }
    }

    /// <summary>
    /// Update tool name UI text
    /// </summary>
    private void UpdateToolUI(ToolDataSO tool)
    {
        if (toolNameText != null)
        {
            toolNameText.text = tool.toolName;
        }
    }

    /// <summary>
    /// Clear tool visuals when unequipped
    /// </summary>
    private void ClearToolVisuals()
    {
        if (toolSpriteRenderer != null)
            toolSpriteRenderer.enabled = false;

        if (toolNameText != null)
            toolNameText.text = "No Tool";
    }

    /// <summary>
    /// Log tool equip info
    /// </summary>
    private void LogToolEquip(ToolDataSO tool)
    {
        Debug.Log($"Equipped: {tool.toolName} (Stamina: {tool.staminaCost}, Cooldown: {tool.useCooldown}s)");
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Input System callback for tool usage
    /// Wire to PlayerInput: E key or Mouse Left Click
    /// </summary>
    public void OnUseTool(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            useToolPressed = true;
        }
    }

    /// <summary>
    /// Input System callback for switching tools
    /// Wire to PlayerInput: Tab key
    /// </summary>
    public void OnSwitchTool(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (availableTools == null || availableTools.Length == 0) return;

        SwitchToNextTool();
    }

    /// <summary>
    /// Switch to next tool in available tools array
    /// </summary>
    private void SwitchToNextTool()
    {
        currentToolIndex = (currentToolIndex + 1) % availableTools.Length;
        toolPresenter.EquipTool(availableTools[currentToolIndex]);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Equip specific tool by index (for external systems)
    /// </summary>
    public void EquipToolByIndex(int index)
    {
        if (availableTools == null || index < 0 || index >= availableTools.Length)
        {
            Debug.LogWarning($"Invalid tool index: {index}");
            return;
        }

        currentToolIndex = index;
        toolPresenter.EquipTool(availableTools[currentToolIndex]);
    }

    /// <summary>
    /// Equip specific tool by data (for external systems)
    /// </summary>
    public void EquipTool(ToolDataSO toolData)
    {
        if (toolData == null)
        {
            Debug.LogWarning("Cannot equip null tool");
            return;
        }

        toolPresenter.EquipTool(toolData);
    }

    /// <summary>
    /// Unequip current tool
    /// </summary>
    public void UnequipTool()
    {
        toolPresenter.UnequipTool();
    }

    #endregion
}

