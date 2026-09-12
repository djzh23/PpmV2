namespace PpmV2.Application.Common.Results;

/// <summary>
/// Generic version of ServiceResult carrying a data payload on success.
/// </summary>
/// <remarks>
/// This type is commonly used by application services to return data together
/// with success/failure information without throwing exceptions.
/// </remarks>
public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; }

    protected ServiceResult(bool success, T? data, string? errorMessage, bool isNotFound = false)
        : base(success, errorMessage, isNotFound)
    {
        Data = data;
    }

    public static ServiceResult<T> Ok(T data) => new(true, data, null);

    public static new ServiceResult<T> Fail(string errorMessage) =>
        new(false, default, errorMessage);

    public static new ServiceResult<T> NotFound(string errorMessage) =>
        new(false, default, errorMessage, isNotFound: true);
}
