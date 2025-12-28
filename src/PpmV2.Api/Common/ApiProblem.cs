using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Common.Errors;

namespace PpmV2.Api.Common;

/// <summary>
/// Factory for standardized ProblemDetails responses.
/// </summary>
/// <remarks>
/// Converts application-level errors (AppError) into RFC 7807 ProblemDetails.
/// This keeps controllers thin and ensures consistent error responses across endpoints.
/// </remarks>
public static class ApiProblem
{
    /// <summary>
    /// Creates an ObjectResult containing ProblemDetails based on the given AppError.
    /// </summary>
    /// <param name="error">Application error containing HTTP status, error code, message and optional field errors.</param>
    /// <param name="http">Current HTTP context (used for ProblemDetails.Instance).</param>
    public static ObjectResult From(AppError error, HttpContext http)
    {
        var pd = new ProblemDetails
        {
            Status = error.HttpStatus,
            Title = error.Code,
            Detail = error.Message,
            Instance = http.Request.Path
        };

        // Optional field-level errors (e.g. validation errors) are exposed via extensions.
        if (error.Errors is not null && error.Errors.Count > 0)
            pd.Extensions["errors"] = error.Errors;

        return new ObjectResult(pd) { StatusCode = error.HttpStatus };
    }
}
