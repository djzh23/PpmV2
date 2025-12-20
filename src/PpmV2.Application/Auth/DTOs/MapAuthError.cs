namespace PpmV2.Application.Auth.DTOs;

//private IActionResult MapAuthError(AuthResult result)
//{
//    var (status, title) = result.ErrorCode switch
//    {
//        AuthErrorCode.ValidationFailed => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR"),
//        AuthErrorCode.InvalidCredentials => (StatusCodes.Status401Unauthorized, "INVALID_CREDENTIALS"),
//        AuthErrorCode.NotApproved => (StatusCodes.Status403Forbidden, "NOT_APPROVED"),
//        AuthErrorCode.UserAlreadyExists => (StatusCodes.Status409Conflict, "USER_ALREADY_EXISTS"),
//        AuthErrorCode.UserCreationFailed => (StatusCodes.Status400BadRequest, "USER_CREATION_FAILED"),
//        _ => (StatusCodes.Status400BadRequest, "AUTH_ERROR")
//    };

//    var problem = new ProblemDetails
//    {
//        Status = status,
//        Title = title,
//        Detail = result.ErrorMessage
//    };

//    if (result.Errors is not null && result.Errors.Count > 0)
//        problem.Extensions["errors"] = result.Errors;

//    return StatusCode(status, problem);
//}
