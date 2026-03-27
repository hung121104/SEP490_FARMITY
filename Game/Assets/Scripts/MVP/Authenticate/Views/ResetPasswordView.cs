using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ResetPasswordView : MonoBehaviour
{
    [Header("Request OTP")]
    [SerializeField] private InputField emailField;
    [SerializeField] private Button requestOtpButton;

    [Header("Confirm Reset")]
    [SerializeField] private InputField otpField;
    [SerializeField] private InputField newPasswordField;
    [SerializeField] private InputField confirmPasswordField;
    [SerializeField] private Button confirmResetButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Panel Navigation")]
    [Tooltip("BookPanelController that owns reset and OTP panels.")]
    [SerializeField] private BookPanelController bookPanelController;
    [Tooltip("Index of the reset OTP panel inside BookPanelController's Panels list.")]
    [SerializeField] private int resetOtpPanelIndex = 1;
    [Tooltip("Index of the reset request panel inside BookPanelController's Panels list.")]
    [SerializeField] private int resetRequestPanelIndex = 0;
    [Tooltip("Index of the login panel inside BookPanelController's Panels list.")]
    [SerializeField] private int loginPanelIndex = 0;

    private ResetPasswordPresenter presenter;

    private void Start()
    {
        presenter = new ResetPasswordPresenter(new ResetPasswordService(), this);

        if (requestOtpButton != null)
            requestOtpButton.onClick.AddListener(() => presenter.RequestOtp());

        if (confirmResetButton != null)
            confirmResetButton.onClick.AddListener(() => presenter.ConfirmReset());

        if (errorText != null)
            errorText.text = string.Empty;
    }

    public string GetEmail() => emailField != null ? emailField.text : string.Empty;
    public string GetOtp() => otpField != null ? otpField.text : string.Empty;
    public string GetNewPassword() => newPasswordField != null ? newPasswordField.text : string.Empty;
    public string GetConfirmPassword() => confirmPasswordField != null ? confirmPasswordField.text : string.Empty;

    public void ShowOtpPanel()
    {
        bookPanelController?.ShowPanel(resetOtpPanelIndex);

        if (errorText != null)
            errorText.text = "A reset code has been sent to your email.";
    }

    public void HideOtpPanel()
    {
        bookPanelController?.ShowPanel(resetRequestPanelIndex);

        if (otpField != null)
            otpField.text = string.Empty;

        if (newPasswordField != null)
            newPasswordField.text = string.Empty;

        if (confirmPasswordField != null)
            confirmPasswordField.text = string.Empty;
    }

    public void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;

        Debug.LogWarning($"[ResetPasswordView] {message}");
    }

    public void SetInteractable(bool interactable)
    {
        if (emailField != null) emailField.interactable = interactable;
        if (requestOtpButton != null) requestOtpButton.interactable = interactable;

        if (otpField != null) otpField.interactable = interactable;
        if (newPasswordField != null) newPasswordField.interactable = interactable;
        if (confirmPasswordField != null) confirmPasswordField.interactable = interactable;
        if (confirmResetButton != null) confirmResetButton.interactable = interactable;
    }

    public void OnResetSuccess()
    {
        if (errorText != null)
            errorText.text = "Password reset successful. You can log in now.";

        HideOtpPanel();
        bookPanelController?.ShowPanel(loginPanelIndex);

        Debug.Log("[ResetPasswordView] Password reset completed.");
    }
}
