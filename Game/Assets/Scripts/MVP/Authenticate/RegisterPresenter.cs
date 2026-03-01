using UnityEngine;
using System.Threading.Tasks;

public class RegisterPresenter
{
    private readonly IRegisterService registerService;
    private readonly RegisterView view;

    public RegisterPresenter(IRegisterService service, RegisterView view)
    {
        registerService = service;
        this.view       = view;
    }

    public async void Register()
    {
        string username = view.GetUsername();
        string password = view.GetPassword();
        string email    = view.GetEmail();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
        {
            view.ShowError("Please fill in all fields.");
            return;
        }

        view.SetInteractable(false);

        var request = new RegisterRequest
        {
            username = username,
            password = password,
            email    = email
        };

        RegisterResponse response = await registerService.Register(request);

        view.SetInteractable(true);

        if (response != null)
        {
            Debug.Log($"[RegisterPresenter] Registered as {response.username}.");
            view.OnRegisterSuccess(response);
        }
        else
        {
            view.ShowError("Registration failed. Username or email may already be in use.");
        }
    }
}
