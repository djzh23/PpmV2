using PpmV2.Application.Common.Errors;

namespace PpmV2.Application.Common.Results;

public static class ServiceResultExtensions
{
    public static AppError ToAppError(this ServiceResult result)
    {
        var message = result.ErrorMessage ?? "An error occurred.";

        var httpStatus = result.IsNotFound ? 404 : result.IsForbidden ? 403 : 400;
        var code = result.IsNotFound ? "NOT_FOUND" : result.IsForbidden ? "FORBIDDEN" : "BAD_REQUEST";

        return new AppError(code, message, httpStatus);
    }
}
