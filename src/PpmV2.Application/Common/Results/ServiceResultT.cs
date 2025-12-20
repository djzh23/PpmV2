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

    protected ServiceResult(bool success, T? data, string? errorMessage)
        : base(success, errorMessage)
    {
        Data = data;
    }

    public static ServiceResult<T> Ok(T data) =>
        new ServiceResult<T>(true, data, null);

    public static new ServiceResult<T> Fail(string errorMessage) =>
        new ServiceResult<T>(false, default, errorMessage);
}
