using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Common.Results;

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
