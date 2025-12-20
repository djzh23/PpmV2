namespace PpmV2.Application.Common.Exceptions;

/// <summary>
/// Exception type representing validation failures in application use cases.
/// </summary>
/// <remarks>
/// This exception is thrown when business or input validation rules are violated.
/// It is translated into a standardized ProblemDetails response by the API middleware.
/// </remarks>
public sealed class ValidationException : Exception
{
    public string Code { get; }
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message, string code = "VALIDATION_ERROR")
        : base(message)
    {
        Code = code;
        Errors = new();
    }

    public ValidationException(Dictionary<string, string[]> errors, string code = "VALIDATION_ERROR")
        : base("Validation failed.")
    {
        Code = code;
        Errors = errors;
    }
}
