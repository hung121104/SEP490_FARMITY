using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that owns the FarmittyInputActions asset.
/// Loads/saves binding overrides via PlayerPrefs so custom keybinds persist.
/// Other scripts access actions through InputManager.Instance (e.g. InputManager.Instance.Harvest).
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
    public InputAction Harvest       => _actions.Player.Harvest;
    public InputAction OpenInventory => _actions.Player.OpenInventory;
    public InputAction Attack        => _actions.Player.Attack;
    public InputAction UseSkill      => _actions.Player.UseSkill;
    public InputAction OpenChat      => _actions.Player.OpenChat;

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

    // ───── PlayerPrefs key ─────
    private const string BINDINGS_KEY = "InputBindings";

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
    /// Serializes all current binding overrides to JSON and writes to PlayerPrefs.
    /// Call this after every successful rebind.
    /// </summary>
    public void SaveBindings()
    {
        string json = _actions.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BINDINGS_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("[InputManager] Bindings saved.");
    }

    /// <summary>
    /// Restores binding overrides from PlayerPrefs (if any exist).
    /// Called automatically in Awake.
    /// </summary>
    public void LoadBindings()
    {
        string json = PlayerPrefs.GetString(BINDINGS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            _actions.asset.LoadBindingOverridesFromJson(json);
            Debug.Log("[InputManager] Bindings loaded from PlayerPrefs.");
        }
    }

    /// <summary>
    /// Removes all overrides and deletes the PlayerPrefs key.
    /// Useful for a "Reset to Defaults" button.
    /// </summary>
    public void ResetBindingsToDefault()
    {
        _actions.asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(BINDINGS_KEY);
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
