using System;
using System.Collections.Generic;
using System.Text;

namespace PpmV2.Application.Common.Results;

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

