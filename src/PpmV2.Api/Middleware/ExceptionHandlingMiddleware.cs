using System.Net;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Common.Exceptions;

namespace PpmV2.Api.Middleware;

/// <summary>
/// Central exception-to-ProblemDetails middleware.
/// </summary>
/// <remarks>
/// This middleware translates known application exceptions into standardized RFC 7807 responses.
/// Currently it handles ValidationException and returns HTTP 400 with field-level error details.
/// </remarks>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            // Validation errors are returned as ProblemDetails (application/problem+json).
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = ex.Code,
                Detail = ex.Message
            };

            // Attach field-level validation details in extensions to support client-side error rendering.
            if (ex.Errors.Count > 0)
                problem.Extensions["errors"] = ex.Errors;

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
