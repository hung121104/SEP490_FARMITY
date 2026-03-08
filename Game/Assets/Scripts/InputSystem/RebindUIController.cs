using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to each "keybind row" in the Settings UI.
/// Handles interactive rebinding, duplicate-key detection, and label updates.
///
/// Inspector setup:
///   • actionName   – exact action name in the Player map (e.g. "Harvest")
///   • bindingIndex – which binding to rebind (0 for single-key actions)
///   • bindingLabel – TMP_Text that shows the current key
///   • rebindButton – Button the player clicks to start rebinding
///   • waitingText  – text shown while waiting for a key press (default "Press a key…")
/// </summary>
public class RebindUIController : MonoBehaviour
{
    // ───── Inspector ─────
    [Header("Action Reference")]
    [Tooltip("Exact name of the action in the Player map (e.g. \"Harvest\").")]
    [SerializeField] private string actionName;

    [Tooltip("Binding index within the action (0 for simple single-key actions).")]
    [SerializeField] private int bindingIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text bindingLabel;
    [SerializeField] private Button   rebindButton;

    [Header("Options")]
    [SerializeField] private string waitingText = "Press a key…";

    [Tooltip("Optional panel/overlay shown while waiting for input.")]
    [SerializeField] private GameObject waitingOverlay;

    [Tooltip("Optional popup shown when a duplicate binding is detected.")]
    [SerializeField] private GameObject duplicateWarningPopup;

    // ───── Runtime ─────
    private InputAction        _action;
    private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Lifecycle
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void Start()
    {
        _action = InputManager.Instance.Actions.Player.FindAction(actionName);
        if (_action == null)
        {
            Debug.LogError($"[RebindUI] Action \"{actionName}\" not found in Player map.");
            return;
        }

        rebindButton.onClick.AddListener(StartRebind);
        RefreshLabel();
    }

    private void OnDestroy()
    {
        CleanUpOperation();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Rebinding flow
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>Call this (or hook it to a Button) to begin the interactive rebind.</summary>
    public void StartRebind()
    {
        if (_action == null) return;

        // Show "Press a key…" feedback
        bindingLabel.text = waitingText;
        rebindButton.interactable = false;
        if (waitingOverlay != null) waitingOverlay.SetActive(true);

        // Disable player input while listening
        InputManager.Instance.DisablePlayerActions();

        _rebindOperation = _action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebind())
            .OnCancel(operation  => CancelRebind())
            .Start();
    }

    /// <summary>Called when the player successfully presses a new key.</summary>
    private void FinishRebind()
    {
        string newPath = _action.bindings[bindingIndex].effectivePath;

        // ── Duplicate check ──
        if (HasDuplicateBinding(newPath, out string conflictActionName))
        {
            // Revert the override
            _action.RemoveBindingOverride(bindingIndex);

            Debug.LogWarning($"[RebindUI] Duplicate! \"{newPath}\" is already bound to \"{conflictActionName}\".");

            if (duplicateWarningPopup != null)
            {
                duplicateWarningPopup.SetActive(true);
                // Optionally set a label inside the popup:
                var label = duplicateWarningPopup.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"Key already used by \"{conflictActionName}\".";
            }
        }
        else
        {
            InputManager.Instance.SaveBindings();
        }

        EndRebindUI();
    }

    /// <summary>Called when the player presses Escape to cancel.</summary>
    private void CancelRebind()
    {
        EndRebindUI();
    }

    /// <summary>Clean up the operation and restore the UI state.</summary>
    private void EndRebindUI()
    {
        CleanUpOperation();
        RefreshLabel();
        rebindButton.interactable = true;
        if (waitingOverlay != null) waitingOverlay.SetActive(false);
        InputManager.Instance.EnablePlayerActions();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Duplicate detection
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Iterates every binding in the Player action map.
    /// Returns true if any OTHER action already uses the same effective path.
    /// </summary>
    private bool HasDuplicateBinding(string newPath, out string conflictActionName)
    {
        conflictActionName = string.Empty;
        var playerMap = InputManager.Instance.Actions.Player.Get();

        foreach (var action in playerMap.actions)
        {
            // Skip ourselves
            if (action == _action) continue;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                // Skip composite parent entries (they have no path)
                if (action.bindings[i].isComposite) continue;

                if (action.bindings[i].effectivePath == newPath)
                {
                    conflictActionName = action.name;
                    return true;
                }
            }
        }
        return false;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Label helpers
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>Updates the label to show the human-readable key name.</summary>
    private void RefreshLabel()
    {
        if (_action == null || bindingLabel == null) return;

        bindingLabel.text = InputControlPath.ToHumanReadableString(
            _action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    /// <summary>Disposes the rebinding operation if it exists.</summary>
    private void CleanUpOperation()
    {
        _rebindOperation?.Dispose();
        _rebindOperation = null;
    }
}
