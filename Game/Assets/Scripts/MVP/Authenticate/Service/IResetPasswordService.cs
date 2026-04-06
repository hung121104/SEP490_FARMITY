using System.Threading.Tasks;

public interface IResetPasswordService
{
    Task<RequestResetPasswordResponse> RequestResetOtp(RequestResetPasswordRequest request);
    Task<ConfirmResetPasswordResponse> ConfirmReset(ConfirmResetPasswordRequest request);
}
