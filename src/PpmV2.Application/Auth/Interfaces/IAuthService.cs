using PpmV2.Application.Auth.DTOs;
using PpmV2.Application.Common.Results;

namespace PpmV2.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResult> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task<ServiceResult> LogoutAsync(string refreshToken, CancellationToken ct = default);
}
