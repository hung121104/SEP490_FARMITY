using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HideUiButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;

    private InputAction _toggleSettingsAction;
    private Button _button;
    private Coroutine _reenableToggleRoutine;

    void Awake()
    {
        uiCanvasGroup.Hide();
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(HideUI);
    }

    private void OnEnable()
    {
        TryBindToggleInput();
    }

    private void Update()
    {
        if (_toggleSettingsAction == null)
            TryBindToggleInput();
    }

    private void OnDisable()
    {
        if (_toggleSettingsAction != null)
        {
            _toggleSettingsAction.performed -= OnToggleSettings;
            _toggleSettingsAction = null;
        }

        if (_reenableToggleRoutine != null)
        {
            StopCoroutine(_reenableToggleRoutine);
            _reenableToggleRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HideUI);

        if (_reenableToggleRoutine != null)
        {
            StopCoroutine(_reenableToggleRoutine);
            _reenableToggleRoutine = null;
        }
    }

    private void TryBindToggleInput()
    {
        if (_toggleSettingsAction != null || InputManager.Instance == null)
            return;

        _toggleSettingsAction = InputManager.Instance.Actions.Player.ToggleSettings;
        if (_toggleSettingsAction != null)
            _toggleSettingsAction.performed += OnToggleSettings;
    }

    private void OnToggleSettings(InputAction.CallbackContext _)
    {
        if (uiCanvasGroup == null || uiCanvasGroup.alpha <= 0f)
            return;

        if (ToggleInGameSettingMenu.IsGlobalToggleAllowed)
            return;

        HideUI();
    }

    public void HideUI()
    {
        Debug.Log("HideUI called");
        uiCanvasGroup.Hide();

        if (_reenableToggleRoutine != null)
            StopCoroutine(_reenableToggleRoutine);

        _reenableToggleRoutine = StartCoroutine(ReenableToggleNextFrame());
    }

    private IEnumerator ReenableToggleNextFrame()
    {
        yield return null;
        ToggleInGameSettingMenu.SetGlobalAllowToggleState(true);
        _reenableToggleRoutine = null;
    }
}
