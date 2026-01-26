using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AuthenticateLoginView : MonoBehaviour
{
    [SerializeField]
    private InputField username;
    [SerializeField]
    private InputField password;
    [SerializeField]
    private Button loginButton;

    private AuthenticatePresenter presenter;

    void Start()
    {
        var service = new AuthenticateService();
        presenter = new AuthenticatePresenter(service, this);
        loginButton.onClick.AddListener(() => presenter.Login());
    }

    public string GetUsername()
    {
        return username.text;
    }

    public string GetPassword()
    {
        return password.text;
    }
}
