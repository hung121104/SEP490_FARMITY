using System;
using UnityEngine;

public class ResetPasswordPresenter
{
    private readonly IResetPasswordService resetPasswordService;
    private readonly ResetPasswordView view;

    private string pendingEmail;

    public ResetPasswordPresenter(IResetPasswordService resetPasswordService, ResetPasswordView view)
    {
        this.resetPasswordService = resetPasswordService;
        this.view = view;
    }

    // Step 1: Send OTP to user email.
    public async void RequestOtp()
    {
        Debug.Log("[ResetPasswordPresenter] RequestOtp called.");

        string email = view.GetEmail();

        if (string.IsNullOrWhiteSpace(email))
        {
            view.ShowError("Please enter your email.");
            return;
        }

        view.SetInteractable(false);

        try
        {
            var request = new RequestResetPasswordRequest
            {
                email = email
            };

            RequestResetPasswordResponse response = await resetPasswordService.RequestResetOtp(request);

            view.SetInteractable(true);

            if (response != null && response.ok)
            {
                pendingEmail = email;
                view.ShowOtpPanel();
                return;
            }

            view.ShowError(response?.message ?? "Could not send reset code. Please try again.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ResetPasswordPresenter] RequestOtp exception: {ex.Message}\n{ex.StackTrace}");
            view.SetInteractable(true);
            view.ShowError("An unexpected error occurred. Please try again.");
        }
    }

    // Step 2: Verify OTP and set a new password.
    public async void ConfirmReset()
    {
        Debug.Log("[ResetPasswordPresenter] ConfirmReset called.");

        string otp = view.GetOtp();
        string newPassword = view.GetNewPassword();
        string confirmPassword = view.GetConfirmPassword();

        if (string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            view.ShowError("Please fill in OTP and both password fields.");
            return;
        }

        if (newPassword != confirmPassword)
        {
            view.ShowError("Passwords do not match.");
            return;
        }

        string emailToUse = !string.IsNullOrWhiteSpace(pendingEmail) ? pendingEmail : view.GetEmail();

        if (string.IsNullOrWhiteSpace(emailToUse))
        {
            view.ShowError("Please request a reset code first.");
            return;
        }

        view.SetInteractable(false);

        try
        {
            var request = new ConfirmResetPasswordRequest
            {
                email = emailToUse,
                otp = otp,
                newPassword = newPassword
            };

            ConfirmResetPasswordResponse response = await resetPasswordService.ConfirmReset(request);

            view.SetInteractable(true);

            if (response != null && response.ok)
            {
                Debug.Log("[ResetPasswordPresenter] Password reset successful.");
                view.OnResetSuccess();
                return;
            }

            view.ShowError(response?.message ?? "Reset failed. Please check OTP and try again.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ResetPasswordPresenter] ConfirmReset exception: {ex.Message}\n{ex.StackTrace}");
            view.SetInteractable(true);
            view.ShowError("An unexpected error occurred. Please try again.");
        }
    }
}
