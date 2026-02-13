using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;  // Add this for scene loading
using Photon.Pun;  // Add this for Photon integration

public class AuthenticatePresenter
{
    private IAuthenticateService authenticateService;
    private AuthenticateLoginView authenticateLoginView;

    public AuthenticatePresenter(IAuthenticateService service, AuthenticateLoginView view)
    {
        authenticateService = service;
        authenticateLoginView = view;
    }

    public async void Login()
    {
        string username = authenticateLoginView.GetUsername();
        string password = authenticateLoginView.GetPassword();

        var request = new LoginRequest { username = username, password = password };
        var response = await authenticateService.Login(request);

        if (response != null)
        {
            // Set Photon UserId for identification (secure, no token)
            PhotonNetwork.AuthValues = new Photon.Realtime.AuthenticationValues
            {
                UserId = response.userId ?? username  // Use backend userId or username
            };
            PhotonNetwork.NickName = response.username ?? username;  // Use username from backend

            Debug.Log("Login successful, loading next scene");
            // Load the scene containing ConnectToServer (replace "ConnectScene" with your actual scene name)
            SceneManager.LoadScene("MainMenuScene");
        }
        else
        {
            // Show error
            Debug.Log("Login failed");
        }
    }
}
