using System.Threading.Tasks;

public interface IRegisterService
{
    Task<RegisterResponse> Register(RegisterRequest request);
}
