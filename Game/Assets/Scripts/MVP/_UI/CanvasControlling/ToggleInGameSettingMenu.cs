using UnityEngine;
using UnityEngine.InputSystem;

public class ToggleInGameSettingMenu : MonoBehaviour
{
    public static ToggleInGameSettingMenu Instance { get; private set; }
    public static bool IsGlobalToggleAllowed => Instance == null || Instance.allowToggle;

    [SerializeField] private CanvasGroup inGameSettingMenuCanvasGroup;
    [SerializeField] private CanvasGroup _UICanvasGroup;
    [SerializeField] private CanvasGroup _HUDCanvasGroup;

    private bool allowToggle = true;
    private InputAction _escapeToggleAction;
    private bool _playerInputBlocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _escapeToggleAction = new InputAction("ToggleSettingsMenu", InputActionType.Button, "<Keyboard>/escape");
        _escapeToggleAction.performed += OnToggleSettings;
        inGameSettingMenuCanvasGroup.Hide();
    }

    private void OnEnable()
    {
        _escapeToggleAction?.Enable();
    }

    private void Update()
    {
        if (inGameSettingMenuCanvasGroup != null
            && inGameSettingMenuCanvasGroup.alpha > 0f
            && !_playerInputBlocked
            && InputManager.Instance != null)
        {
            BlockPlayerInput();
        }
    }

    private void OnDisable()
    {
        _escapeToggleAction?.Disable();
        UnblockPlayerInput();
    }

    private void OnDestroy()
    {
        if (_escapeToggleAction != null)
        {
            _escapeToggleAction.performed -= OnToggleSettings;
            _escapeToggleAction.Dispose();
            _escapeToggleAction = null;
        }

        UnblockPlayerInput();

        if (Instance == this)
            Instance = null;
    }

    public void ToggleAllowToggleState()
    {
        allowToggle = !allowToggle;
    }

    public void SetAllowToggleState(bool isAllowed)
    {
        Debug.Log($"[ToggleInGameSettingMenu] SetAllowToggleState called with: {isAllowed}");
        allowToggle = isAllowed;
    }

    public static void SetGlobalAllowToggleState(bool isAllowed)
    {
        if (Instance != null)
            Instance.SetAllowToggleState(isAllowed);
    }

    private void OnToggleSettings(InputAction.CallbackContext _)
    {
        if (inGameSettingMenuCanvasGroup == null || _UICanvasGroup == null || _HUDCanvasGroup == null)
            return;
        Debug.Log("[ToggleInGameSettingMenu] OnToggleSettings called (input): " + allowToggle);
        if (!allowToggle)
            return;

        if (inGameSettingMenuCanvasGroup.alpha > 0f)
            HideInGameSettingMenu();
        else
            ShowInGameSettingMenu();
    }

    private void ShowInGameSettingMenu()
    {
        inGameSettingMenuCanvasGroup.Show();
        _UICanvasGroup.Hide();
        _HUDCanvasGroup.Hide();
        BlockPlayerInput();
    }

    private void HideInGameSettingMenu()
    {
        inGameSettingMenuCanvasGroup.Hide();
        _UICanvasGroup.Show();
        _HUDCanvasGroup.Show();
        UnblockPlayerInput();
    }

    private void BlockPlayerInput()
    {
        if (_playerInputBlocked || InputManager.Instance == null)
            return;

        InputManager.Instance.DisablePlayerActions();
        _playerInputBlocked = true;
    }

    private void UnblockPlayerInput()
    {
        if (!_playerInputBlocked || InputManager.Instance == null)
            return;

        InputManager.Instance.EnablePlayerActions();
        _playerInputBlocked = false;
    }

}
