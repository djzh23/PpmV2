namespace PpmV2.Application.Common.Errors;

/// <summary>
/// Represents a structured application error independent of HTTP or transport concerns.
/// </summary>
/// <remarks>
/// AppError is used to transport error information from the application layer
/// to the API layer, where it is mapped to RFC 7807 ProblemDetails.
/// 
/// It supports both a general error message and optional field-level errors
/// (e.g. validation errors).
/// </remarks>
public sealed record AppError(
    string Code,
    string Message,
    int HttpStatus,
    Dictionary<string, string[]>? Errors = null
);
