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

    protected ServiceResult(bool success, string? errorMessage = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    public static ServiceResult Ok() => new ServiceResult(true);

    public static ServiceResult Fail(string errorMessage) =>
        new ServiceResult(false, errorMessage);
}

