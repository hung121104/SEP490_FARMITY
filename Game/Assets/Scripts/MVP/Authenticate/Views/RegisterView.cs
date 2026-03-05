using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// View for the registration screen.
/// Attach to the Register panel GameObject and wire up the UI references in the Inspector.
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

    [Header("Navigation")]
    [Tooltip("Scene to load after a successful registration (leave empty to just show a success message).")]
    [SerializeField] private string successScene = "";

    private RegisterPresenter presenter;

    private void Start()
    {
        presenter = new RegisterPresenter(new RegisterService(), this);
        registerButton.onClick.AddListener(() => presenter.Register());

        if (errorText != null)
            errorText.text = string.Empty;
    }

    // ── Data getters (called by presenter) ──────────────────────────────────

    public string GetUsername() => usernameField != null ? usernameField.text : string.Empty;
    public string GetPassword() => passwordField != null ? passwordField.text : string.Empty;
    public string GetEmail()    => emailField    != null ? emailField.text    : string.Empty;

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
    }

    public void OnRegisterSuccess(RegisterResponse response)
    {
        if (errorText != null)
            errorText.text = string.Empty;

        Debug.Log($"[RegisterView] Registration successful — welcome {response.username}!");

        if (!string.IsNullOrEmpty(successScene))
            SceneManager.LoadScene(successScene);
    }
}
