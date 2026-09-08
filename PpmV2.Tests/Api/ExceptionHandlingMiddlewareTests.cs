using Microsoft.AspNetCore.Http;
using PpmV2.Api.Middleware;
using PpmV2.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace PpmV2.Tests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private static DefaultHttpContext CreateContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task Invoke_CallsNext_WhenNoExceptionIsThrown()
    {
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };
        var middleware = new ExceptionHandlingMiddleware(next);

        await middleware.Invoke(CreateContext());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Invoke_Returns400_WhenValidationExceptionIsThrown()
    {
        RequestDelegate next = _ => throw new ValidationException("Title is required.");
        var middleware = new ExceptionHandlingMiddleware(next);
        var ctx = CreateContext();

        await middleware.Invoke(ctx);

        Assert.Equal((int)HttpStatusCode.BadRequest, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Invoke_SetsContentType_ToApplicationProblemJson()
    {
        RequestDelegate next = _ => throw new ValidationException("Invalid input.");
        var middleware = new ExceptionHandlingMiddleware(next);
        var ctx = CreateContext();

        await middleware.Invoke(ctx);

        Assert.Equal("application/problem+json", ctx.Response.ContentType);
    }

    [Fact]
    public async Task Invoke_WritesErrorCode_InTitle()
    {
        RequestDelegate next = _ => throw new ValidationException("Bad value.", "MY_CODE");
        var middleware = new ExceptionHandlingMiddleware(next);
        var ctx = CreateContext();

        await middleware.Invoke(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        Assert.Contains("MY_CODE", body);
    }

    [Fact]
    public async Task Invoke_IncludesFieldErrors_WhenValidationExceptionHasErrorsDictionary()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["title"] = ["Title is required."],
            ["startAtUtc"] = ["Start time must be in the future."]
        };
        RequestDelegate next = _ => throw new ValidationException(errors);
        var middleware = new ExceptionHandlingMiddleware(next);
        var ctx = CreateContext();

        await middleware.Invoke(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        Assert.Contains("title", body);
        Assert.Contains("startAtUtc", body);
    }

    [Fact]
    public async Task Invoke_DoesNotWriteErrors_WhenValidationExceptionHasNoErrors()
    {
        RequestDelegate next = _ => throw new ValidationException("General error.");
        var middleware = new ExceptionHandlingMiddleware(next);
        var ctx = CreateContext();

        await middleware.Invoke(ctx);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        var doc = JsonDocument.Parse(body);
        // extensions should not contain an "errors" key when Errors is empty
        Assert.False(doc.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Invoke_RethrowsUnhandledExceptions()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Unexpected failure.");
        var middleware = new ExceptionHandlingMiddleware(next);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(CreateContext()));
    }
}
