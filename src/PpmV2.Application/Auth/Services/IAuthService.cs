using PpmV2.Application.Auth.DTOs;

namespace PpmV2.Application.Auth.Services;

public interface IAuthService
{
    //Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    //Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
}
