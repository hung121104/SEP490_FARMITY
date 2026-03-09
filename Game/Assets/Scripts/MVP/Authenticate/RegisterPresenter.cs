using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class RegisterPresenter
{
    private readonly IRegisterService registerService;
    private readonly IAuthenticateService authenticateService;
    private readonly RegisterView view;

    // Stored from step 1 so we can reuse them in auto-login
    private string pendingUsername;
    private string pendingPassword;
    private string pendingEmail;

    public RegisterPresenter(IRegisterService registerService, IAuthenticateService authenticateService, RegisterView view)
    {
        this.registerService     = registerService;
        this.authenticateService = authenticateService;
        this.view                = view;
    }

    // ── Step 1: Submit registration form → sends OTP email ──────────────────
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

        if (response != null && response.status == "pending_verification")
        {
            pendingUsername = username;
            pendingPassword = password;
            pendingEmail    = email;
            Debug.Log($"[RegisterPresenter] OTP sent to {email}.");
            view.ShowOtpPanel();
        }
        else
        {
            view.ShowError(response?.message ?? "Registration failed. Username or email may already be in use.");
        }
    }

    // ── Step 2: Verify OTP → creates the real account ───────────────────────
    public async void VerifyOtp()
    {
        string otp = view.GetOtp();

        if (string.IsNullOrWhiteSpace(otp))
        {
            view.ShowError("Please enter the verification code.");
            return;
        }

        view.SetInteractable(false);

        var request = new VerifyRegistrationRequest
        {
            email = pendingEmail,
            otp   = otp
        };

        VerifyRegistrationResponse response = await registerService.VerifyRegistration(request);

        view.SetInteractable(true);

        if (response != null && response.ok)
        {
            Debug.Log("[RegisterPresenter] Email verified successfully! Logging in automatically...");
            view.HideOtpPanel();
            await AutoLogin();
        }
        else
        {
            view.ShowError(response?.message ?? "Verification failed. Please check your code and try again.");
        }
    }

    // ── Step 3: Auto-login after successful verification ────────────────────
    private async Task AutoLogin()
    {
        view.SetInteractable(false);
        view.ShowError("Verified! Logging you in...");

        var loginRequest = new LoginRequest
        {
            username = pendingUsername,
            password = pendingPassword
        };

        LoginResponse loginResponse = await authenticateService.Login(loginRequest);

        view.SetInteractable(true);

        if (loginResponse != null)
        {
            PhotonNetwork.AuthValues = new Photon.Realtime.AuthenticationValues
            {
                UserId = loginResponse.userId ?? pendingUsername
            };
            PhotonNetwork.NickName = loginResponse.username ?? pendingUsername;

            Debug.Log("[RegisterPresenter] Auto-login successful, loading main scene.");
            SceneManager.LoadScene("MainMenuScene");
        }
        else
        {
            Debug.LogWarning("[RegisterPresenter] Auto-login failed after registration.");
            view.OnRegisterSuccess(); // fall back: let view handle navigation
        }
    }
}
