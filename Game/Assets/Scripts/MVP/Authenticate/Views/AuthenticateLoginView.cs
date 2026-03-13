using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthenticateLoginView : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private InputField username;
    [SerializeField] private InputField password;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;

    [Header("Feedback")]
    [SerializeField] private Text errorText;

    [Header("Navigation")]
    [SerializeField] private string successScene = "";

    private AuthenticatePresenter presenter;

    void Start()
    {
        var service = new AuthenticateService();
        presenter = new AuthenticatePresenter(service, this);
        loginButton.onClick.AddListener(() => presenter.Login());

        if (errorText != null)
            errorText.text = string.Empty;
    }

    public string GetUsername() => username != null ? username.text : string.Empty;
    public string GetPassword() => password != null ? password.text : string.Empty;

    public void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;

        Debug.LogWarning($"[LoginView] {message}");
    }

    public void SetInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (username    != null) username.interactable    = interactable;
        if (password    != null) password.interactable    = interactable;
    }

    public void OnLoginSuccess()
    {
        if (errorText != null)
            errorText.text = string.Empty;

        Debug.Log("[LoginView] Login successful!");

        if (!string.IsNullOrEmpty(successScene))
            SceneManager.LoadScene(successScene);
    }
}
