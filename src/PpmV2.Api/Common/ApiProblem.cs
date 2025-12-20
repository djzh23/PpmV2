using Microsoft.AspNetCore.Mvc;
using PpmV2.Application.Common.Errors;

namespace PpmV2.Api.Common;

public static class ApiProblem
{
    public static ObjectResult From(AppError error, HttpContext http)
    {
        var pd = new ProblemDetails
        {
            Status = error.HttpStatus,
            Title = error.Code,
            Detail = error.Message,
            Instance = http.Request.Path
        };

        if (error.Errors is not null && error.Errors.Count > 0)
            pd.Extensions["errors"] = error.Errors;

        return new ObjectResult(pd) { StatusCode = error.HttpStatus };
    }
}
