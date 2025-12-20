namespace PpmV2.Application.Auth.DTOs;

public sealed class AuthResult
{
    public bool Success { get; set; }

    public AuthErrorCode ErrorCode { get; set; } = AuthErrorCode.None;
    public string? ErrorMessage { get; set; }

    // Optional: Feldfehler (sehr nützlich für Register)
    public Dictionary<string, string[]>? Errors { get; set; }

    public string? Token { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }

    public static AuthResult Ok(Guid userId, string email, string? token = null)
        => new()
        {
            Success = true,
            UserId = userId,
            Email = email,
            Token = token,
            ErrorCode = AuthErrorCode.None
        };

    public static AuthResult Fail(AuthErrorCode code, string message, Dictionary<string, string[]>? errors = null)
        => new()
        {
            Success = false,
            ErrorCode = code,
            ErrorMessage = message,
            Errors = errors
        };
}
