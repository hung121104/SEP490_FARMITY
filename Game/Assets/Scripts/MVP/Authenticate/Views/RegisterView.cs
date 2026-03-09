using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    [SerializeField] private Text errorText;

    [Header("OTP Verification Panel")]
    [Tooltip("A panel (CanvasGroup) containing the OTP input and verify button. Hidden by default.")]
    [SerializeField] private CanvasGroup otpPanel;
    [SerializeField] private InputField otpField;
    [SerializeField] private Button verifyButton;

    [Header("Navigation")]
    [Tooltip("Scene to load after a successful registration (leave empty to just show a success message).")]
    [SerializeField] private string successScene = "";

    private RegisterPresenter presenter;

    private void Start()
    {
        presenter = new RegisterPresenter(new RegisterService(), new AuthenticateService(), this);
        registerButton.onClick.AddListener(() => presenter.Register());

        if (verifyButton != null)
            verifyButton.onClick.AddListener(() => presenter.VerifyOtp());

        if (errorText != null)
            errorText.text = string.Empty;

        HideOtpPanel();
    }

    // ── Data getters (called by presenter) ──────────────────────────────────

    public string GetUsername() => usernameField != null ? usernameField.text : string.Empty;
    public string GetPassword() => passwordField != null ? passwordField.text : string.Empty;
    public string GetEmail()    => emailField    != null ? emailField.text    : string.Empty;
    public string GetOtp()      => otpField      != null ? otpField.text      : string.Empty;

    // ── OTP Panel (called by presenter) ─────────────────────────────────────

    public void ShowOtpPanel()
    {
        if (otpPanel != null)
            otpPanel.Show();

        if (errorText != null)
            errorText.text = "A verification code has been sent to your email.";
    }

    public void HideOtpPanel()
    {
        if (otpPanel != null)
            otpPanel.Hide();

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

        if (!string.IsNullOrEmpty(successScene))
            SceneManager.LoadScene(successScene);
    }
}
