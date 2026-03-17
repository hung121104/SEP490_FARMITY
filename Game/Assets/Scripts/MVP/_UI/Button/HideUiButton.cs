using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HideUiButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup uiCanvasGroup;

    private InputAction _hideUiEscapeAction;
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
        BindEscapeInput();
    }

    private void Update()
    {
        if (_hideUiEscapeAction == null)
            BindEscapeInput();
    }

    private void OnDisable()
    {
        if (_hideUiEscapeAction != null)
        {
            _hideUiEscapeAction.performed -= OnToggleSettings;
            _hideUiEscapeAction.Disable();
            _hideUiEscapeAction.Dispose();
            _hideUiEscapeAction = null;
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

    private void BindEscapeInput()
    {
        if (_hideUiEscapeAction != null)
            return;

        _hideUiEscapeAction = new InputAction("HideUiEscape", InputActionType.Button, "<Keyboard>/escape");
        _hideUiEscapeAction.performed += OnToggleSettings;
        _hideUiEscapeAction.Enable();
    }

    private void OnToggleSettings(InputAction.CallbackContext _)
    {
        if (uiCanvasGroup == null || uiCanvasGroup.alpha <= 0f)
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
