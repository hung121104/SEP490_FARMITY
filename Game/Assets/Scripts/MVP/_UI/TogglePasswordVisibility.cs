using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class TogglePasswordVisibility : MonoBehaviour
{
    [SerializeField] private InputField inputField;

    private Toggle _toggle;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(OnToggleChanged);
        OnToggleChanged(_toggle.isOn); // sync input field to initial toggle state
    }

    private void OnDestroy()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (inputField == null) return;

        inputField.contentType = isOn
            ? InputField.ContentType.Standard
            : InputField.ContentType.Password;

        inputField.ForceLabelUpdate();
    }
}
