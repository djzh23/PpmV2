namespace PpmV2.Application.Common.Results;

/// <summary>
/// Represents the result of an application service operation.
/// </summary>
/// <remarks>
/// ServiceResult is used for non-exceptional control flow where failures are expected
/// and should be handled explicitly (e.g. user not found, invalid credentials).
/// 
/// This avoids using exceptions for regular business outcomes.
/// </remarks>
public class ServiceResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public bool IsNotFound { get; }

    protected ServiceResult(bool success, string? errorMessage = null, bool isNotFound = false)
    {
        Success = success;
        ErrorMessage = errorMessage;
        IsNotFound = isNotFound;
    }

    public static ServiceResult Ok() => new(true);

    public static ServiceResult Fail(string errorMessage) =>
        new(false, errorMessage);

    public static ServiceResult NotFound(string errorMessage) =>
        new(false, errorMessage, isNotFound: true);
}

