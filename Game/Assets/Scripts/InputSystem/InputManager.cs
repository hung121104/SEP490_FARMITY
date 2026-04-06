using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that owns the FarmittyInputActions asset.
/// Loads/saves binding overrides via PlayerPrefs so custom keybinds persist.
/// Other scripts access actions through InputManager.Instance (e.g. InputManager.Instance.Interact).
/// </summary>
public class InputManager : MonoBehaviour
{
    // ───── Singleton ─────
    public static InputManager Instance { get; private set; }

    // ───── Generated class from the .inputactions asset ─────
    private FarmittyInputActions _actions;

    /// <summary>Raw reference to the whole asset – useful for RebindUIController.</summary>
    public FarmittyInputActions Actions => _actions;

    // ───── Convenience accessors for gameplay scripts ─────
    public InputAction Move          => _actions.Player.Move;
    public InputAction Interact      => _actions.Player.Interact;
    public InputAction OpenInventory => _actions.Player.OpenInventory;
    public InputAction Attack        => _actions.Player.Attack;
    public InputAction SkillConfirm  => _actions.Player.SkillConfirm;
    public InputAction SkillCancel   => _actions.Player.SkillCancel;
    public InputAction UseWeaponSkill => _actions.Player.UseWeaponSkill;
    public InputAction OpenCharacterProgression => _actions.Player.OpenCharacterProgression;
    public InputAction UseSkillSlot1 => _actions.Player.UseSkillSlot1;
    public InputAction UseSkillSlot2 => _actions.Player.UseSkillSlot2;
    public InputAction UseSkillSlot3 => _actions.Player.UseSkillSlot3;
    public InputAction UseSkillSlot4 => _actions.Player.UseSkillSlot4;
    public InputAction OpenChat      => _actions.Player.OpenChat;
    public InputAction Sprint        => _actions.Player.Sprint;

    // ───── Hotbar / Item actions ─────
    public InputAction UseItem       => _actions.Player.UseItem;
    public InputAction ScrollItem    => _actions.Player.ScrollItem;
    public InputAction HotbarSlot1   => _actions.Player.HotbarSlot1;
    public InputAction HotbarSlot2   => _actions.Player.HotbarSlot2;
    public InputAction HotbarSlot3   => _actions.Player.HotbarSlot3;
    public InputAction HotbarSlot4   => _actions.Player.HotbarSlot4;
    public InputAction HotbarSlot5   => _actions.Player.HotbarSlot5;
    public InputAction HotbarSlot6   => _actions.Player.HotbarSlot6;
    public InputAction HotbarSlot7   => _actions.Player.HotbarSlot7;
    public InputAction HotbarSlot8   => _actions.Player.HotbarSlot8;
    public InputAction HotbarSlot9   => _actions.Player.HotbarSlot9;

    /// <summary>
    /// Returns the HotbarSlotN action for a 0-based index (0 → HotbarSlot1, etc.).
    /// Returns null if index is out of range.
    /// </summary>
    public InputAction GetHotbarSlotAction(int index)
    {
        return index switch
        {
            0 => HotbarSlot1,
            1 => HotbarSlot2,
            2 => HotbarSlot3,
            3 => HotbarSlot4,
            4 => HotbarSlot5,
            5 => HotbarSlot6,
            6 => HotbarSlot7,
            7 => HotbarSlot8,
            8 => HotbarSlot9,
            _ => null
        };
    }

    /// <summary>
    /// Returns the UseSkillSlotN action for a 0-based index (0 -> UseSkillSlot1, etc.).
    /// Returns null if index is out of range.
    /// </summary>
    public InputAction GetSkillSlotAction(int index)
    {
        return index switch
        {
            0 => UseSkillSlot1,
            1 => UseSkillSlot2,
            2 => UseSkillSlot3,
            3 => UseSkillSlot4,
            _ => null
        };
    }

    // ───── PlayerPrefs key prefix ─────
    private const string BINDINGS_KEY = "Keybind";

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Lifecycle
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void Awake()
    {
        // Singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create action instance & restore saved overrides
        _actions = new FarmittyInputActions();
        LoadBindings();
        _actions.Player.Enable();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            _actions.Player.Disable();
            _actions.Dispose();
            Instance = null;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Save / Load
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Saves every binding override individually to PlayerPrefs.
    /// Uses per-binding string keys instead of InputSystem's JSON API
    /// to avoid IL2CPP stripping issues in builds.
    /// Call this after every successful rebind.
    /// </summary>
    public void SaveBindings()
    {
        var playerMap = _actions.asset.FindActionMap("Player");
        if (playerMap == null) return;

        foreach (var action in playerMap.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite) continue;

                string key = BINDINGS_KEY + "_" + action.name + "_" + i;
                if (binding.hasOverrides)
                    PlayerPrefs.SetString(key, binding.overridePath ?? string.Empty);
                else
                    PlayerPrefs.DeleteKey(key);
            }
        }

        PlayerPrefs.Save();
        Debug.Log("[InputManager] Bindings saved.");
    }

    /// <summary>
    /// Restores binding overrides from PlayerPrefs (if any exist).
    /// Called automatically in Awake.
    /// </summary>
    public void LoadBindings()
    {
        var playerMap = _actions.asset.FindActionMap("Player");
        if (playerMap == null) return;

        int loaded = 0;
        foreach (var action in playerMap.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].isComposite) continue;

                string key = BINDINGS_KEY + "_" + action.name + "_" + i;
                if (PlayerPrefs.HasKey(key))
                {
                    action.ApplyBindingOverride(i, PlayerPrefs.GetString(key));
                    loaded++;
                    continue;
                }

                // Backward compatibility for renamed action: SkillManagementToggle -> OpenCharacterProgression
                if (action.name == "OpenCharacterProgression")
                {
                    string legacyKey = BINDINGS_KEY + "_SkillManagementToggle_" + i;
                    if (PlayerPrefs.HasKey(legacyKey))
                    {
                        action.ApplyBindingOverride(i, PlayerPrefs.GetString(legacyKey));
                        loaded++;
                    }
                }

                // Backward compatibility for renamed action: WeaponSkillTrigger -> UseWeaponSkill
                if (action.name == "UseWeaponSkill")
                {
                    string legacyKey = BINDINGS_KEY + "_WeaponSkillTrigger_" + i;
                    if (PlayerPrefs.HasKey(legacyKey))
                    {
                        action.ApplyBindingOverride(i, PlayerPrefs.GetString(legacyKey));
                        loaded++;
                    }
                }

                // Backward compatibility for renamed actions: SkillSlotN -> UseSkillSlotN
                if (action.name == "UseSkillSlot1" || action.name == "UseSkillSlot2" ||
                    action.name == "UseSkillSlot3" || action.name == "UseSkillSlot4")
                {
                    string legacyActionName = action.name.Replace("UseSkillSlot", "SkillSlot");
                    string legacyKey = BINDINGS_KEY + "_" + legacyActionName + "_" + i;
                    if (PlayerPrefs.HasKey(legacyKey))
                    {
                        action.ApplyBindingOverride(i, PlayerPrefs.GetString(legacyKey));
                        loaded++;
                    }
                }
            }
        }

        if (loaded > 0)
            Debug.Log($"[InputManager] Loaded {loaded} binding override(s) from PlayerPrefs.");
    }

    /// <summary>
    /// Removes all overrides and deletes all PlayerPrefs keys for bindings.
    /// Useful for a "Reset to Defaults" button.
    /// </summary>
    public void ResetBindingsToDefault()
    {
        var playerMap = _actions.asset.FindActionMap("Player");
        if (playerMap != null)
        {
            foreach (var action in playerMap.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action.bindings[i].isComposite) continue;
                    PlayerPrefs.DeleteKey(BINDINGS_KEY + "_" + action.name + "_" + i);
                }
            }
        }

        _actions.asset.RemoveAllBindingOverrides();
        PlayerPrefs.Save();
        Debug.Log("[InputManager] Bindings reset to defaults.");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Helpers
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Temporarily disable all player actions (e.g. while a UI menu is open
    /// or during an interactive rebind).
    /// </summary>
    public void DisablePlayerActions()  => _actions.Player.Disable();

    /// <summary>Re-enable all player actions.</summary>
    public void EnablePlayerActions()   => _actions.Player.Enable();
}
