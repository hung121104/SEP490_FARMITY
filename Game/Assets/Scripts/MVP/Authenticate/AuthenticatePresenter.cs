using UnityEngine;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;  // Add this for scene loading
using Photon.Pun;  // Add this for Photon integration
using AchievementManager.Presenter;

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

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            authenticateLoginView.ShowError("Please enter username and password.");
            return;
        }

        authenticateLoginView.SetInteractable(false);

        try
        {
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

                // After SessionManager.Instance.SetAuthenticationData(...):
                AchievementPresenter.Instance?.OnLoginSuccess();
                return;
            }

            authenticateLoginView.ShowError("Login failed. Please check your username or password.");
            Debug.Log("Login failed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthenticatePresenter] Login exception: {ex.Message}");
            authenticateLoginView.ShowError("Unable to login right now. Please try again.");
        }
        finally
        {
            authenticateLoginView.SetInteractable(true);
        }
    }
}
