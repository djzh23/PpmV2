using PpmV2.Application.Common.Errors;

namespace PpmV2.Application.Auth.DTOs;

public static class AuthResultExtensions
{
    public static AppError ToAppError(this AuthResult result)
    {
        // Useful for fail cases
        var message = result.ErrorMessage ?? "Authentication error.";

        return result.ErrorCode switch
        {
            AuthErrorCode.ValidationFailed =>
                new AppError("AUTH_VALIDATION_FAILED", message, 400, result.Errors),

            AuthErrorCode.UserAlreadyExists =>
                new AppError("AUTH_USER_ALREADY_EXISTS", message, 409, result.Errors),

            AuthErrorCode.InvalidCredentials =>
                new AppError("AUTH_INVALID_CREDENTIALS", message, 401),

            AuthErrorCode.NotApproved =>
                new AppError("AUTH_NOT_APPROVED", message, 403),

            AuthErrorCode.UserCreationFailed =>
                new AppError("AUTH_USER_CREATION_FAILED", message, 400, result.Errors),

            _ =>
                new AppError("AUTH_ERROR", message, 400, result.Errors)
        };
    }
}
