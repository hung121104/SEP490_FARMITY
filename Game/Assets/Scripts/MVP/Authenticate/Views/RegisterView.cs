using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// View for the registration screen.
/// Attach to the Register panel GameObject and wire up the UI references in the Inspector.
/// After the initial registration form is submitted, the OTP panel is shown for email verification.
/// </summary>
public class RegisterView : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private InputField usernameField;
    [SerializeField] private InputField passwordField;
    [SerializeField] private InputField emailField;

    [Header("Buttons")]
    [SerializeField] private Button registerButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("OTP Verification")]
    [SerializeField] private InputField otpField;
    [SerializeField] private Button verifyButton;

    [Header("Panel Navigation")]
    [Tooltip("BookPanelController that owns the register/OTP panels.")]
    [SerializeField] private BookPanelController bookPanelController;
    [Tooltip("Index of the OTP panel inside BookPanelController's Panels list.")]
    [SerializeField] private int otpPanelIndex = 1;

    private RegisterPresenter presenter;

    private void Start()
    {
        presenter = new RegisterPresenter(new RegisterService(), new AuthenticateService(), this);
        registerButton.onClick.AddListener(() => presenter.Register());

        if (verifyButton != null)
            verifyButton.onClick.AddListener(() => presenter.VerifyOtp());

        if (errorText != null)
            errorText.text = string.Empty;
    }

    // ── Data getters (called by presenter) ──────────────────────────────────

    public string GetUsername() => usernameField != null ? usernameField.text : string.Empty;
    public string GetPassword() => passwordField != null ? passwordField.text : string.Empty;
    public string GetEmail()    => emailField    != null ? emailField.text    : string.Empty;
    public string GetOtp()      => otpField      != null ? otpField.text      : string.Empty;

    // ── OTP Panel (called by presenter) ─────────────────────────────────────

    public void ShowOtpPanel()
    {
        NavigateToPanel(otpPanelIndex, "OTP");

        if (errorText != null)
            errorText.text = "A verification code has been sent to your email.";
    }

    public void HideOtpPanel()
    {
        if (otpField != null)
            otpField.text = string.Empty;
    }

    // ── Feedback (called by presenter) ──────────────────────────────────────

    public void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;

        Debug.LogWarning($"[RegisterView] {message}");
    }

    public void SetInteractable(bool interactable)
    {
        if (registerButton != null) registerButton.interactable = interactable;
        if (usernameField  != null) usernameField.interactable  = interactable;
        if (passwordField  != null) passwordField.interactable  = interactable;
        if (emailField     != null) emailField.interactable     = interactable;
        if (verifyButton   != null) verifyButton.interactable   = interactable;
        if (otpField       != null) otpField.interactable       = interactable;
    }

    public void OnRegisterSuccess()
    {
        if (errorText != null)
            errorText.text = string.Empty;

        Debug.Log("[RegisterView] Registration and email verification successful!");
    }

    private void NavigateToPanel(int panelIndex, string panelName)
    {
        if (bookPanelController == null)
        {
            Debug.LogWarning($"[RegisterView] Cannot show {panelName} panel: BookPanelController is not assigned.");
            return;
        }

        if (panelIndex < 0)
        {
            Debug.LogWarning($"[RegisterView] Cannot show {panelName} panel: invalid panel index {panelIndex}.");
            return;
        }

        bookPanelController.ShowPanel(panelIndex);
    }
}
