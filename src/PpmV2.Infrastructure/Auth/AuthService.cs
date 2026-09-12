using Microsoft.AspNetCore.Identity;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Application.Auth.Interfaces;
using PpmV2.Application.Common.Results;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Auth;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;
using System.Security.Cryptography;

namespace PpmV2.Infrastructure.Auth;

/// <summary>
/// Infrastructure implementation of authentication use cases (register/login/refresh/logout).
/// </summary>
/// <remarks>
/// This service integrates ASP.NET Core Identity (UserManager) with the application domain:
/// - Uses Identity for credentials and secure password validation.
/// - Uses UserProfile repository for application-specific profile data.
/// - Issues JWT access tokens and persisted single-use refresh tokens.
///
/// Note: We intentionally avoid leaking whether an email exists during login.
/// </remarks>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly TimeProvider _timeProvider;

    // Refresh tokens live for 7 days; access tokens are short-lived (configured in JwtTokenService).
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public AuthService(
        UserManager<AppUser> userManager,
        IUserProfileRepository userProfileRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _userProfileRepository = userProfileRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT access token + refresh token if credentials are valid and the account is approved.
    /// </summary>
    /// <remarks>
    /// Security note: The method returns a generic error for invalid credentials to prevent user enumeration.
    /// </remarks>
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var validationErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
            validationErrors["email"] = ["Email is required."];

        if (string.IsNullOrWhiteSpace(request.Password))
            validationErrors["password"] = ["Password is required."];

        if (validationErrors.Count > 0)
            return AuthResult.Fail(AuthErrorCode.ValidationFailed, "Validation failed.", validationErrors);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
            return AuthResult.Fail(AuthErrorCode.InvalidCredentials, "Invalid email or password.");

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!validPassword)
            return AuthResult.Fail(AuthErrorCode.InvalidCredentials, "Invalid email or password.");

        if (user.Status != UserStatus.Approved)
            return AuthResult.Fail(AuthErrorCode.NotApproved, "Your account has not been approved by an administrator yet.");

        var (accessToken, refreshToken) = await IssueTokenPairAsync(user, ct);

        return AuthResult.Ok(userId: user.Id, email: user.Email!, token: accessToken, refreshToken: refreshToken);
    }

    /// <summary>
    /// Validates the refresh token and issues a new access + refresh token pair (single-use rotation).
    /// </summary>
    /// <remarks>
    /// The old refresh token is revoked on use. Any attempt to reuse a revoked token is rejected.
    /// </remarks>
    public async Task<AuthResult> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return AuthResult.Fail(AuthErrorCode.InvalidCredentials, "Refresh token is required.");

        var stored = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);

        if (stored is null || !stored.IsActive(_timeProvider.GetUtcNow().UtcDateTime))
            return AuthResult.Fail(AuthErrorCode.InvalidCredentials, "Refresh token is invalid or has expired.");

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());

        if (user is null || user.Status != UserStatus.Approved)
            return AuthResult.Fail(AuthErrorCode.NotApproved, "User is not active.");

        // Revoke the consumed token before issuing a new one.
        stored.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;

        var (accessToken, newRefreshToken) = await IssueTokenPairAsync(user, ct);

        return AuthResult.Ok(userId: user.Id, email: user.Email!, token: accessToken, refreshToken: newRefreshToken);
    }

    /// <summary>
    /// Revokes the supplied refresh token, effectively logging out the session.
    /// </summary>
    public async Task<ServiceResult> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return ServiceResult.Fail("Refresh token is required.");

        var stored = await _refreshTokenRepository.GetByTokenAsync(refreshToken, ct);

        // Treat unknown or already-revoked tokens as a successful logout -- idempotent.
        if (stored is null || stored.IsRevoked)
            return ServiceResult.Ok();

        stored.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return ServiceResult.Ok();
    }

    /// <summary>
    /// Registers a new user with pending approval status and creates the associated UserProfile.
    /// </summary>
    /// <remarks>
    /// The newly created account is set to Pending and does not receive a JWT token until approved.
    /// </remarks>
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Firstname))
            errors["firstname"] = ["Firstname is required."];

        if (string.IsNullOrWhiteSpace(request.Lastname))
            errors["lastname"] = ["Lastname is required."];

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = ["Email is required."];

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = ["Password is required."];

        if (errors.Count > 0)
            return AuthResult.Fail(AuthErrorCode.ValidationFailed, "Validation failed.", errors);

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return AuthResult.Fail(
                AuthErrorCode.UserAlreadyExists,
                "A user with this email already exists.",
                new Dictionary<string, string[]> { ["email"] = ["A user with this email already exists."] }
            );

        var appUser = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            IsActive = true,
            Status = UserStatus.Pending,
            Role = UserRole.Honorarkraft,
            IsProfileCompleted = false
        };

        var identityResult = await _userManager.CreateAsync(appUser, request.Password);

        if (!identityResult.Succeeded)
        {
            var identityErrors = MapIdentityErrors(identityResult);
            return AuthResult.Fail(
                AuthErrorCode.UserCreationFailed,
                "User creation failed.",
                identityErrors.Count > 0
                    ? identityErrors
                    : new Dictionary<string, string[]> { ["register"] = ["User creation failed."] }
            );
        }

        var profile = new UserProfile
        {
            IdentityUserId = appUser.Id,
            Email = request.Email,
            Firstname = request.Firstname.Trim(),
            Lastname = request.Lastname.Trim(),
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _userProfileRepository.AddAsync(profile, ct);
        await _userProfileRepository.SaveChangesAsync(ct);

        // Token is intentionally null: user must be approved by admin before login is allowed.
        return AuthResult.Ok(userId: appUser.Id, email: appUser.Email!, token: null);
    }

    // ---------- Private helpers ----------

    /// <summary>
    /// Generates a new access + refresh token pair and persists the refresh token.
    /// </summary>
    private async Task<(string AccessToken, string RefreshToken)> IssueTokenPairAsync(AppUser user, CancellationToken ct)
    {
        var claims = new JwtUserClaims(
            UserId: user.Id,
            Email: user.Email!,
            Role: user.Role,
            Status: user.Status
        );

        var accessToken = _jwtTokenService.GenerateToken(claims);
        var refreshTokenValue = GenerateRefreshTokenValue();
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.Add(RefreshTokenLifetime)
        };

        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return (accessToken, refreshTokenValue);
    }

    /// <summary>
    /// Generates a cryptographically secure random token value (base64url, 256-bit entropy).
    /// </summary>
    private static string GenerateRefreshTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static Dictionary<string, string[]> MapIdentityErrors(IdentityResult identityResult)
    {
        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in identityResult.Errors)
        {
            var key = InferFieldKey(e);
            if (!grouped.TryGetValue(key, out var list))
            {
                list = [];
                grouped[key] = list;
            }
            list.Add(e.Description);
        }

        return grouped.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    private static string InferFieldKey(IdentityError e)
    {
        var code = e.Code ?? string.Empty;

        if (code.Contains("Password", StringComparison.OrdinalIgnoreCase)) return "password";
        if (code.Contains("Email", StringComparison.OrdinalIgnoreCase)) return "email";
        if (code.Contains("UserName", StringComparison.OrdinalIgnoreCase)) return "email";

        var desc = e.Description ?? string.Empty;

        if (desc.Contains("password", StringComparison.OrdinalIgnoreCase)) return "password";
        if (desc.Contains("email", StringComparison.OrdinalIgnoreCase)) return "email";
        if (desc.Contains("user name", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("username", StringComparison.OrdinalIgnoreCase)) return "email";

        return "register";
    }
}
