using System.Threading.Tasks;

public interface IAuthenticateService
{
    Task<LoginResponse> Login(LoginRequest request);
}
