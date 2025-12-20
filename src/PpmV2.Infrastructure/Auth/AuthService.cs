using Microsoft.AspNetCore.Identity;
using PpmV2.Application.Admin.Interfaces;
using PpmV2.Application.Auth.DTOs;
using PpmV2.Application.Auth.Interfaces;
using PpmV2.Application.Users.Interfaces;
using PpmV2.Domain.Users;
using PpmV2.Infrastructure.Identity;

namespace PpmV2.Infrastructure.Auth;

/// <summary>
/// Infrastructure implementation of authentication use cases (register/login).
/// </summary>
/// <remarks>
/// This service integrates ASP.NET Core Identity (UserManager) with the application domain:
/// - Uses Identity for credentials and secure password validation.
/// - Uses UserProfile repository for application-specific profile data.
/// - Issues JWT tokens based on domain-relevant claims.
///
/// Note: We intentionally avoid leaking whether an email exists during login.
/// </remarks>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        UserManager<AppUser> userManager,
        IUserProfileRepository userProfileRepository,
        IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _userProfileRepository = userProfileRepository;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token if credentials are valid and the account is approved.
    /// </summary>
    /// <remarks>
    /// Security note: The method returns a generic error for invalid credentials to prevent user enumeration.
    /// </remarks>
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        // Service - level validation to keep the service robust even if DTO - level validation is bypassed.
        var validationErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
            validationErrors["email"] = new[] { "Email is required." };

        if (string.IsNullOrWhiteSpace(request.Password))
            validationErrors["password"] = new[] { "Password is required." };

        if (validationErrors.Count > 0)
        {
            return AuthResult.Fail(
                AuthErrorCode.ValidationFailed,
                "Validation failed.",
                validationErrors
            );
        }

        var user = await _userManager.FindByEmailAsync(request.Email);

        // Do not leak whether the email exists (prevents user enumeration).
        if (user == null)
        {
            return AuthResult.Fail(
                AuthErrorCode.InvalidCredentials,
                "Invalid email or password."
            );
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!validPassword)
        {
            return AuthResult.Fail(
                AuthErrorCode.InvalidCredentials,
                "Invalid email or password."
            );
        }

        // Enforce approval workflow before allowing access.
        if (user.Status != UserStatus.Approved)
        {
            return AuthResult.Fail(
                AuthErrorCode.NotApproved,
                "Your account has not been approved by an administrator yet."
            );
        }

        // Domain-relevant information is embedded into the JWT as claims for authorization decisions.
        var claims = new JwtUserClaims(
            UserId: user.Id,
            Email: user.Email!,
            Role: user.Role,
            Status: user.Status
        );

        var token = _jwtTokenService.GenerateToken(claims);

        return AuthResult.Ok(
            userId: user.Id,
            email: user.Email!,
            token: token
        );
    }

    /// <summary>
    /// Registers a new user with pending approval status and creates the associated UserProfile.
    /// </summary>
    /// <remarks>
    /// The newly created account is set to Pending and does not receive a JWT token until approved.
    /// </remarks>
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // Service-level validation to keep the service robust even if DTO-level validation is bypassed.
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Firstname))
            errors["firstname"] = new[] { "Firstname is required." };

        if (string.IsNullOrWhiteSpace(request.Lastname))
            errors["lastname"] = new[] { "Lastname is required." };

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = new[] { "Email is required." };

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = new[] { "Password is required." };

        if (errors.Count > 0)
        {
            return AuthResult.Fail(
                AuthErrorCode.ValidationFailed,
                "Validation failed.",
                errors
            );
        }

        // Duplicate check (email is used as username).
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return AuthResult.Fail(
                AuthErrorCode.UserAlreadyExists,
                "A user with this email already exists.",
                new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "A user with this email already exists." }
                }
            );
        }

        // Create Identity user (credentials + account state).
        var appUser = new AppUser
        {
            UserName = request.Email, // you use email as username
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
                    : new Dictionary<string, string[]>
                    {
                        ["register"] = new[] { "User creation failed." }
                    }
            );
        }

        // Create domain profile (application-specific user data).
        var profile = new UserProfile
        {
            IdentityUserId = appUser.Id,
            Email = request.Email,
            Firstname = request.Firstname.Trim(),
            Lastname = request.Lastname.Trim(),
            IsActive = true
        };

        await _userProfileRepository.AddAsync(profile);
        await _userProfileRepository.SaveChangesAsync();

        // Token is intentionally null: user must be approved by admin before login is allowed.
        return AuthResult.Ok(
            userId: appUser.Id,
            email: appUser.Email!,
            token: null
        );
    }

    /// <summary>
    /// Maps ASP.NET Identity errors into a field-based dictionary compatible with API validation responses.
    /// </summary>
    private static Dictionary<string, string[]> MapIdentityErrors(IdentityResult identityResult)
    {
        // Best-effort mapping into field-based errors.
        // IdentityError.Code may vary across providers; we map by Code/Description heuristics.
        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in identityResult.Errors)
        {
            var key = InferFieldKey(e);

            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<string>();
                grouped[key] = list;
            }

            list.Add(e.Description);
        }

        return grouped.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to infer a request field name (email/password/register) from an IdentityError.
    /// </summary>
    private static string InferFieldKey(IdentityError e)
    {
        var code = e.Code ?? string.Empty;

        if (code.Contains("Password", StringComparison.OrdinalIgnoreCase))
            return "password";

        if (code.Contains("Email", StringComparison.OrdinalIgnoreCase))
            return "email";

        if (code.Contains("UserName", StringComparison.OrdinalIgnoreCase))
            return "email"; // Email is used as UserName

        var desc = e.Description ?? string.Empty;

        if (desc.Contains("password", StringComparison.OrdinalIgnoreCase))
            return "password";

        if (desc.Contains("email", StringComparison.OrdinalIgnoreCase))
            return "email";

        if (desc.Contains("user name", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("username", StringComparison.OrdinalIgnoreCase))
            return "email";

        return "register";
    }
}
