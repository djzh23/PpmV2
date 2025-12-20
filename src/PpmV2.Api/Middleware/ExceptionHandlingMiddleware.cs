using System.Net;
using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Common.Exceptions;

namespace PpmV2.Api.Middleware;

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
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = ex.Code,
                Detail = ex.Message
            };

            if (ex.Errors.Count > 0)
                problem.Extensions["errors"] = ex.Errors;

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
