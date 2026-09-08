using PpmV2.Application.Common.Errors;

namespace PpmV2.Application.Common.Results;

public static class ServiceResultExtensions
{
    public static AppError ToAppError(this ServiceResult result)
    {
        var message = result.ErrorMessage ?? "An error occurred.";

        var httpStatus = message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? 404
            : 400;

        var code = httpStatus == 404 ? "NOT_FOUND" : "BAD_REQUEST";

        return new AppError(code, message, httpStatus);
    }
}
