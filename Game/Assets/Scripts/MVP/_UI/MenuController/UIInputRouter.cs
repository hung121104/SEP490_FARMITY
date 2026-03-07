using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Maps an InputAction to a panel toggle.
/// Configurable entirely from the Inspector — add new panels without touching code.
/// </summary>
[Serializable]
public class UIPanelBinding
{
    [Tooltip("Must match the PanelId of the target IUIPanel (e.g. 'Inventory', 'Crafting')")]
    public string panelId;

    [Tooltip("Input action that triggers the toggle (assign from InputSystem_Actions asset)")]
    public InputActionReference toggleAction;
}

/// <summary>
/// Routes input events to UIPanelManager.
/// Attach to a persistent GameObject in the game scene (same as UIPanelManager).
///
/// HOW TO ADD A NEW PANEL SHORTCUT:
///   1. Add an action to your InputSystem_Actions asset in the Unity Editor (e.g. UI/ToggleCrafting).
///   2. Add a new entry to the Panel Bindings list in the Inspector.
///   3. Set panelId = the panel's PanelId string and assign the action reference.
///   No code changes required.
/// </summary>
public class UIInputRouter : MonoBehaviour
{
    [Header("Panel Toggle Bindings")]
    [SerializeField] private List<UIPanelBinding> panelBindings = new();

    [Header("Close Actions")]
    [Tooltip("Closes the topmost UI (popup first, then panel). Typically Escape.")]
    [SerializeField] private InputActionReference closeTopmostAction;

    [Tooltip("Closes ALL open panels instantly.")]
    [SerializeField] private InputActionReference closeAllAction;

    private void OnEnable()
    {
        foreach (var binding in panelBindings)
        {
            if (binding.toggleAction == null) continue;
            binding.toggleAction.action.performed += OnTogglePanel;
            binding.toggleAction.action.Enable();
        }

        if (closeTopmostAction != null)
        {
            closeTopmostAction.action.performed += OnCloseTopmost;
            closeTopmostAction.action.Enable();
        }

        if (closeAllAction != null)
        {
            closeAllAction.action.performed += OnCloseAll;
            closeAllAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        foreach (var binding in panelBindings)
        {
            if (binding.toggleAction == null) continue;
            binding.toggleAction.action.performed -= OnTogglePanel;
        }

        if (closeTopmostAction != null)
            closeTopmostAction.action.performed -= OnCloseTopmost;

        if (closeAllAction != null)
            closeAllAction.action.performed -= OnCloseAll;
    }

    private void OnTogglePanel(InputAction.CallbackContext ctx)
    {
        if (UIPanelManager.Instance == null) return;

        // Find which binding triggered this action
        foreach (var binding in panelBindings)
        {
            if (binding.toggleAction != null &&
                binding.toggleAction.action == ctx.action &&
                !string.IsNullOrEmpty(binding.panelId))
            {
                UIPanelManager.Instance.Toggle(binding.panelId);
                return;
            }
        }
    }

    private void OnCloseTopmost(InputAction.CallbackContext ctx)
    {
        UIPanelManager.Instance?.CloseTopmost();
    }

    private void OnCloseAll(InputAction.CallbackContext ctx)
    {
        UIPanelManager.Instance?.CloseAll();
    }
}
